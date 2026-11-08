# Forge.Next.LocalWebShell

**Run an ASP.NET Core app (for example Blazor Server) as a local tool in the user's own browser, safely, without
WebView2.**

The application is a console process with Kestrel inside. It opens the user's default browser (Edge, Chrome, Firefox,
Opera), and the browser shows the UI. This package makes that setup safe:
- it listens on the loopback address only;
- it admits only the browser it opened itself (a launch token exchanged for a cookie);
- it refuses foreign Host headers;
- it opens the browser only once the server listens;
- it never opens a privileged browser: on Windows it opens the browser **without elevation** even when the
  application runs as administrator, and on Linux and macOS it opens none as root;
- it never starts a browser inside the terminal: on a Linux machine without a desktop it only prints the link.

It runs on Windows, Linux and macOS. The machine needs only the .NET runtime with ASP.NET Core: no WebView2 runtime,
no Edge requirement.

---

## Table of contents

- [Why this package exists](#why-this-package-exists)
- [What it does](#what-it-does)
- [Installation](#installation)
- [Requirements](#requirements)
- [Quick start](#quick-start)
- [Blazor Server](#blazor-server)
- [How a launch works](#how-a-launch-works)
- [The launch token, explained](#the-launch-token-explained)
- [Configuration](#configuration)
- [What to print in the console](#what-to-print-in-the-console)
- [Running as administrator or root](#running-as-administrator-or-root)
- [Linux and macOS](#linux-and-macos)
- [Logging](#logging)
- [Errors](#errors)
- [Several instances on one machine](#several-instances-on-one-machine)
- [Testing an application that uses it](#testing-an-application-that-uses-it)
- [Replacing the browser launcher](#replacing-the-browser-launcher)
- [Moving an existing local shell to this package](#moving-an-existing-local-shell-to-this-package)
- [What it does not protect against](#what-it-does-not-protect-against)
- [API reference](#api-reference)
- [License](#license)

---

## Why this package exists

A local admin tool needs a UI. The usual choices each have a cost:
- **WinForms or WPF with a WebView2 control** needs the WebView2 runtime (or a recent Edge) on the machine. Many customer
  machines do not have it, and customers do not like installing prerequisites.
- **A console process with Kestrel, plus the user's browser**, needs nothing but .NET. A Blazor Server UI then runs in
  any modern browser, and every secret and every network call stays in the process (the "backend"); the browser only
  renders.

The second choice has a catch: **a loopback port is machine-wide, not per user.** Without protection, any process and
any other user signed in on the same machine can open `http://localhost:<port>` and use the tool. Typical cases:
- a Windows Server with several administrators connected over RDP;
- a terminal server where ordinary users work;
- malware running as a standard user.

If the tool runs as administrator, that is a privilege escalation path. Web pages are a risk too: a malicious page can
try to reach `localhost`, for example with DNS rebinding, or by opening a WebSocket to the Blazor hub (CORS does not
apply to WebSockets). This package closes those holes with a few lines of setup.

## What it does

| Concern | What the package does |
| --- | --- |
| Reachable from the network | Kestrel listens on `127.0.0.1` only. A non-loopback address, e.g. from `Kestrel:Endpoints` in the configuration, stops the server with an error. |
| Other users and processes on the machine | A gate in front of the whole pipeline admits only requests that carry the **launch token** (once) or the **session cookie** it is exchanged for. Everything else gets a small 401 page. |
| Malicious web pages | The cookie is `HttpOnly` and `SameSite=Strict`, so a foreign page cannot send it, not even on a WebSocket. A Host header that is not `localhost`, `127.0.0.1` or `[::1]` gets 400 (DNS rebinding). |
| The token leaking | It is never logged by the package. ASP.NET Core's request diagnostics, which log full URLs, are filtered to Warning. After the exchange, a redirect removes it from the address bar. `ToString()` never shows it. |
| Browser opened too early | The browser is opened only after the server listens and the address is checked. |
| Privileged browser | On Windows, an elevated application opens the browser through the desktop shell (Explorer), so the browser runs with the user's normal rights. On Linux and macOS, an application running as root opens no browser. If the unprivileged way is impossible, nothing is opened, rather than a privileged browser. |
| A browser in the terminal | On Linux, without a graphical session (`DISPLAY`, `WAYLAND_DISPLAY`) nothing is opened, so `xdg-open` cannot start a text browser in the console. The link is printed instead. |
| Port conflicts | Port `0` (default): the operating system picks a free port, so nothing collides and nothing has to be asked. A fixed port is possible; if it is taken, a typed error comes back. |
| Several instances | Each instance has its own port, token and cookie (the cookie name contains the port), so they do not lock each other out. |

## Installation

```bash
dotnet add package Forge.Next.LocalWebShell
```

Or via a `PackageReference`:

```xml
<ItemGroup>
  <PackageReference Include="Forge.Next.LocalWebShell" Version="1.0.0" />
</ItemGroup>
```

## Requirements

| | |
| --- | --- |
| **Target frameworks** | `net8.0`, `net9.0`, `net10.0` |
| **Runtime** | The ASP.NET Core shared framework (`Microsoft.AspNetCore.App`). A framework-dependent app needs the **ASP.NET Core Runtime** plus the **.NET Runtime**, or the **ASP.NET Core Hosting Bundle**, which contains both; a self-contained app carries them. No WebView2, no Windows Desktop Runtime. |
| **Dependencies** | [`ErrorOr`](https://www.nuget.org/packages/ErrorOr) (`>= 2.1.1`) for the typed results. |
| **Operating systems** | Windows, Linux (and FreeBSD) and macOS. The gate, the binding and the start are the same everywhere; opening the browser uses the shell or the desktop shell on Windows, `xdg-open` in a Linux graphical session and `open` on macOS (see [Linux and macOS](#linux-and-macos)). |
| **Browser** | Any modern browser. Internet Explorer cannot run Blazor. |

## Quick start

A complete console application that serves one page and opens it in the browser:

```csharp
using ErrorOr;
using Forge.Next.LocalWebShell;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddLocalWebShell();                        // 1. register: loopback, gate, logging filter, launcher

await using WebApplication app = builder.Build();
app.MapGet("/", () => Results.Content("<h1>Hello from the local shell</h1>", "text/html"));

ErrorOr<Session> started = await app.StartLocalWebShellAsync();   // 2. start, then open the browser
if (started.IsError)
{
    Console.Error.WriteLine($"The application could not start: {started.FirstError.Description}");
    return 1;
}

Session session = started.Value;
Console.WriteLine("Keep this window open; closing it stops the application.");
Console.WriteLine("Anyone with the link below can use the application on this machine: do not share it.");
Console.WriteLine($"Open: {session.LaunchUrl}");   // 3. the user can reopen a closed tab with this link

await app.WaitForShutdownAsync();                  // 4. run until Ctrl+C or the window is closed
return 0;
```

Only two calls are new compared with a plain web app:
- `builder.AddLocalWebShell()`;
- `app.StartLocalWebShellAsync()`, used instead of `app.Run()`.

The repository's `samples/Forge.Next.LocalWebShell.Sample` is exactly this program.

## Blazor Server

The package does not care what the application serves. With the .NET 8+ Blazor Web App model:

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddLocalWebShell();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

await using WebApplication app = builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

ErrorOr<Session> started = await app.StartLocalWebShellAsync();
// ... print the link, then:
await app.WaitForShutdownAsync();
```

With the older `_Host.cshtml` model (`AddServerSideBlazor`, `MapBlazorHub`, `MapFallbackToPage("/_Host")`) it is the
same: add the two calls, keep the rest. The gate is inserted before all middleware by a startup filter, so its position
cannot be wrong. Static files, `/_blazor` (negotiation and the WebSocket upgrade) and every endpoint are behind it. Do
not call `UseHttpsRedirection`: the shell serves plain HTTP on loopback, on purpose.

## How a launch works

1. `builder.AddLocalWebShell()` makes Kestrel listen on `127.0.0.1:<Port>` (0: a free port), registers the gate, and
   creates two random secrets for this process: the **launch token** and the **cookie value**.
2. `app.StartLocalWebShellAsync()`:
   1. starts the server;
   2. reads the bound address and checks that every address is loopback;
   3. builds the launch link `http://127.0.0.1:<port>/?token=<launch token>`;
   4. opens the browser with it, if `OpenBrowser` is on.
3. The browser's first request carries the token. The gate:
   1. sets the session cookie `Forge.LocalWebShell-<port>` (`HttpOnly`, `SameSite=Strict`, `Path=/`, session-only);
   2. redirects to the same path without `token`.
4. Every later request, Blazor's WebSocket included, passes with the cookie. A request with neither token nor cookie gets
   the 401 page; a foreign Host header gets 400.
5. The process ends (Ctrl+C, the console closed): the secrets are gone. The next start makes new ones, and old links
   and cookies stop working.

## The launch token, explained

The launch token is the one secret this package adds, and it is easy to confuse with the application's own secrets.

**What it is:** a random value of 32 bytes (43 URL-safe characters), made by the application itself at every start. It
lives only in the process's memory and in the launch link.

**What it is for:** one thing only. It lets the backend tell the browser it opened apart from everyone else who can
reach the same loopback port: another user on the machine, another process, a web page.

**Who sees it:**
- the person at the console, in the printed link;
- the browser that opened the link.

Nobody types it: it is in the link the application opens itself.

**How long it works:** for the lifetime of the process. If the user closes the tab, the console link opens the
application again. The token is exchanged at once for a cookie whose value is a different random value, so the token
never sits in the browser's cookie store, and a redirect removes it from the address bar.

**What it is not:** it is not a password, a user account or an API key, and it says nothing about who the user is. It is
not sent to any other machine, and it is not stored anywhere.

Compared with the secrets a typical admin tool already has:

| | Who issues it | What it is for | Where it lives |
| --- | --- | --- | --- |
| The application's own credentials (API keys, connection strings, pairing codes of a remote service) | That service, or an installer | Calling that service | The application's protected store (e.g. DPAPI), never in the browser |
| **The launch token** | **The application itself, at every start** | **Admitting the browser it opened to its own loopback UI** | **The process's memory and the launch link; gone when the process ends** |
| The session cookie | The gate, in exchange for the token | The same, for every later request of that browser | The browser's cookie jar for this host and port, for the browser session |

## Configuration

The settings are bound from the `LocalWebShell` section. The delegate passed to `AddLocalWebShell` runs after the
binding, so code wins. They are validated when the application is built: `builder.Build()` throws an
`OptionsValidationException` that names every problem.

| Setting | Default | Meaning |
| --- | --- | --- |
| `Port` | `0` | The loopback port. `0` lets the operating system pick a free one at every start (recommended). 1–65535: a fixed port; if taken, the start returns `LocalWebShell.PortInUse`. |
| `OpenBrowser` | `true` | Open the default browser at the launch link. `false` for tests, headless runs and machines without a desktop. |
| `CookieName` | `Forge.LocalWebShell` | The base name of the session cookie; the port is appended (`Forge.LocalWebShell-54321`). It must be a valid cookie name. |
| `CookieSameSite` | `Strict` | `Strict` or `Lax`. `Strict` keeps the cookie off everything a foreign page starts. `Lax` is the fallback for a browser that drops a strict cookie on the first navigation; it still keeps the cookie off cross-site WebSockets and sub-requests. `None` is rejected. |
| `UnauthorizedTitle` | `Open the link from the console window` | The title of the 401 page (HTML-encoded). |
| `UnauthorizedMessage` | `This application opens only through the link…` | The text of the 401 page (HTML-encoded). Translate both with the application's own texts if needed. |

The address is not a setting: the shell always listens on `127.0.0.1` only.

```json
{
  "LocalWebShell": {
    "Port": 0,
    "OpenBrowser": true,
    "CookieSameSite": "Strict"
  }
}
```

From the command line: `MyTool.exe --LocalWebShell:OpenBrowser=false --LocalWebShell:Port=5123`.

## What to print in the console

The console window is the user's only handle on the tool. Recommended lines:

- **The launch link:** `session.LaunchUrl`. The user needs it to reopen a closed tab, and when the browser could not be
  opened (`session.Browser == BrowserState.NotOpened`, reason in `session.BrowserError`).
- **"Keep this window open; closing it stops the application."**
- **"Anyone with the link below can use the application on this machine: do not share it."**
- **Optionally a note for old systems:** "Use a modern browser (Edge, Chrome, Firefox); Internet Explorer is not
  supported."

Print the link to the console only. Never log it and never write it to a file.

## Running as administrator or root

An admin tool often needs administrator rights (services, IIS, machine-wide keys), so its manifest says
`requireAdministrator`. Opening a URL from an elevated process normally starts the browser **elevated**: every page the
user visits until closing that browser would run with administrator rights.

The default launcher avoids this on Windows:
- **Not elevated:** the link opens through the shell (`UseShellExecute`), like a double-click.
- **Elevated:** the link goes to the desktop shell, Explorer, which runs with the user's normal rights, and Explorer
  starts the default browser. This is the documented technique: Raymond Chen, *How can I launch an unelevated process
  from my elevated process and vice versa?* It uses `IShellWindows.FindWindowSW` and then
  `IShellDispatch2.ShellExecute` on the desktop's view.
- **Explorer is not the shell** (Server Core, a replacement shell): nothing is opened, and
  `session.Browser == NotOpened`. Print the link; the user opens it. An elevated browser is never opened as a fallback.

If UAC is off, or the user is the built-in Administrator account, Explorer itself runs elevated, and so does the browser.
That is the machine's choice; the package cannot change it.

The test suite proves the technique for real when it is run from an elevated terminal:
`DesktopShellTests` starts a hidden `cmd.exe` through the desktop shell and checks that it runs at the Medium (unelevated)
integrity level.

On Linux and macOS there is no "unelevated" route that is reliable for every desktop and every `sudo` setup, so an
application running as root opens no browser at all; see [Linux and macOS](#linux-and-macos).

## Linux and macOS

The package runs on Linux and macOS as it does on Windows: the loopback binding, the token and the cookie, the Host
check and the start order are the same, and so is the reason to have them (on a Linux server, every user signed in over
SSH can reach the loopback port). Only opening the browser differs, and it follows the same two rules: never a
privileged browser, never a browser inside the terminal.

| Situation | What the default launcher does |
| --- | --- |
| Linux or FreeBSD, a graphical session (`DISPLAY` or `WAYLAND_DISPLAY` is set), not root | Starts `xdg-open` with the link: the desktop's default browser opens it. |
| Linux, no graphical session (a server over SSH, a container) | Opens nothing; `session.BrowserError` says there is no graphical session. Without this check `xdg-open` either fails or starts a text browser (`lynx`, `w3m`, `links`) in the same terminal and takes over the console. |
| Linux or macOS as root (`sudo`, a root service) | Opens nothing: a browser running as root would give every page root rights, and Chromium-based browsers refuse to run as root anyway. Open the link as your normal user. |
| macOS, not root | Starts `open` with the link. |
| `xdg-open` is not installed | Opens nothing; the error says the browser could not be started. |

In every "opens nothing" case the server runs, and the application prints `session.LaunchUrl`.

**A server without a desktop:** forward the port over SSH and open the link on your own machine. The Host header then
is `127.0.0.1` (allowed), and the token works as usual. With a fixed port:

```bash
# on the server
MyTool --LocalWebShell:Port=5123
# on your machine
ssh -L 5123:127.0.0.1:5123 user@server
# then open the printed link (http://127.0.0.1:5123/?token=...) in your browser
```

**Details:**
- `xdg-open` gets the link as a single argument, without a shell, and its standard input is closed at once, so nothing it
  starts can read from the application's console. Its own messages (and those of the browser it starts) may appear in
  the console; `xdg-open` repeats the link, token included, when it fails. The console is the user's own and already
  shows the link, but do not capture the console into a log.
- The privilege check is `Environment.IsPrivilegedProcess`: root on Unix, an elevated token on Windows.
- Verified on Linux in Docker (`mcr.microsoft.com/dotnet/sdk:10.0`), with `pwsh tools/run-linux-check.ps1`:
  - the whole test suite;
  - the sample as root, as a normal user without a session, and with `DISPLAY` but without and with `xdg-open`;
  - the HTTP flow: 401 without the cookie, admitted with the link, 200 with the cookie, 400 for a foreign Host.

  A real Linux desktop (a browser actually opening) has not been tried yet.

## Logging

- The package logs its own events under `Forge.Next.LocalWebShell` and the gate's type name:
  - the address it listens on (Information);
  - an admitted browser (Information);
  - a refused Host header (Warning);
  - a refused request (Debug, with the path only).

  It never logs the token or a query string.
- ASP.NET Core's `Microsoft.AspNetCore.Hosting.Diagnostics` category logs every request's full URL, query string
  included, at Information level, and the first request carries the token. `AddLocalWebShell` therefore filters that
  category to Warning.
- That filter applies to every provider added through `builder.Logging`, including `builder.Logging.AddSerilog()`. If
  the application replaces the logger factory itself (`builder.Host.UseSerilog()` or `builder.Services.AddSerilog()`),
  set the category to Warning in Serilog's configuration:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": { "Microsoft.AspNetCore.Hosting.Diagnostics": "Warning" }
    }
  }
}
```

- If the application has its own request logging (e.g. `UseSerilogRequestLogging`), make sure it logs the path without
  the query string, or at least not the first request's.

## Errors

`StartLocalWebShellAsync` returns `ErrorOr<Session>`. The codes are in `ErrorCodes`:

| Code | Type | When | What to do |
| --- | --- | --- | --- |
| `LocalWebShell.PortInUse` | Conflict | The fixed `Port` is taken. The server is not running. | Dispose the application; choose another port, or use `0`. |
| `LocalWebShell.NotLoopback` | Failure | The server also listens on a non-loopback address (typically `Kestrel:Endpoints` in the configuration). The server was stopped. | Remove the extra endpoint. |
| `LocalWebShell.NoAddress` | Unexpected | The server reported no address. The server was stopped. | Report it; it should not happen with Kestrel. |
| `LocalWebShell.BrowserNotOpened` | Failure | Not a start failure: found in `session.BrowserError` when `session.Browser == NotOpened`. | Print `session.LaunchUrl` for the user. |

Programming errors throw:
- starting without `AddLocalWebShell`: `InvalidOperationException`;
- invalid settings: `OptionsValidationException`, already from `builder.Build()`.

## Several instances on one machine

Two instances can run at the same time, by the same user or by different users. Each one has:
- its own port (port `0`);
- its own token;
- its own cookie (`Forge.LocalWebShell-<port>`).

Browsers keep cookies per host, not per port, so without the port in the name the second instance's cookie would replace
the first one's and lock its tab out. What the instances manage together (files, services, a remote system) is the
application's concern: lock or serialize it there.

## Testing an application that uses it

- Turn the browser off: `options.OpenBrowser = false` in code, or `--LocalWebShell:OpenBrowser=false`.
- Read `session.LaunchUrl` (or the `Open: …` line from the process output, the way a user would).
- **HTTP-level tests:** an `HttpClient` with a `CookieContainer` that follows redirects behaves like a browser. Request the
  launch link once, then request anything:

```csharp
CookieContainer cookies = new();
using HttpClient browser = new(new HttpClientHandler { CookieContainer = cookies, AllowAutoRedirect = true })
{
    BaseAddress = session.Address,
};
string home = await browser.GetStringAsync(session.LaunchUrl);   // admitted, cookie stored
string page = await browser.GetStringAsync("/settings");          // passes with the cookie
```

- **Real browser tests** (Playwright): navigate to the launch link, then use the page normally. A fresh browser context
  without the link must get 401. The repository's `tools/BrowserCheck` does exactly this against the sample, in headless
  Chromium.
- **On Linux:** `tools/run-linux-check.ps1` runs the test suite and the sample in a .NET SDK container. Copy the idea
  for an application's own Linux check.
- `WebApplicationFactory` / `TestServer` replace Kestrel, so the loopback binding and the address check do not apply there.
  The gate still runs, and its Host check needs the client's base address to be `http://localhost`.

## Replacing the browser launcher

Register an `ILocalBrowserLauncher` before `AddLocalWebShell` to open a specific browser, or to do nothing in tests. The
package adds its default only when none is registered.

```csharp
builder.Services.AddSingleton<ILocalBrowserLauncher, MyLauncher>();
builder.AddLocalWebShell();

internal sealed class MyLauncher : ILocalBrowserLauncher
{
    public ErrorOr<Success> Open(Uri url)
    {
        // url carries the launch token: do not log it.
        ...
        return Result.Success;
    }
}
```

Return an error with `ErrorCodes.BrowserNotOpened` for an expected failure. If the launcher throws, the
start still succeeds and the exception becomes `session.BrowserError`.

## Moving an existing local shell to this package

A typical hand-written local shell does the following:
- builds a `WebApplication`;
- tries a range of ports with `app.RunAsync($"http://localhost:{port}")` and opens the browser with
  `Process.Start("cmd", $"/c start {url}")` before the start has finished;
- has no authentication.

To move it:

1. Remove the port loop and the manual browser start.
2. Call `builder.AddLocalWebShell()` before `builder.Build()`. To keep a fixed port, set `LocalWebShell:Port`.
3. Replace `await app.RunAsync(url)` with:

   ```csharp
   ErrorOr<Session> started = await app.StartLocalWebShellAsync();
   // handle started.IsError (e.g. PortInUse), print started.Value.LaunchUrl
   await app.WaitForShutdownAsync();
   ```

4. Set `"AllowedHosts"` in `appsettings.json` to `"localhost;127.0.0.1;[::1]"`, or leave it: the gate checks the Host
   header itself.
5. Remove any `UseHttpsRedirection`, and any Kestrel endpoint in the configuration (`Kestrel:Endpoints`).
6. Check the logging: see [Logging](#logging), especially with Serilog.
7. Never render a stored secret (a password, a key) into the page, not even in a password field. The browser's DOM is
   readable with the developer tools. Show "stored" and accept a new value instead. This rule is the application's, not
   the package's, but the architecture depends on it.

## What it does not protect against

- **Code running as the same user** can read what that user can: the console window, the browser's profile, files in
  the user's profile. The token protects against *other* users, other users' processes and web pages, not against
  malware that already runs in the user's session.
- **Secrets the application sends to the browser.** Keep them in the backend; see step 7 above.
- **Traffic encryption:** there is none, on purpose. Loopback traffic does not leave the machine, and browsers treat
  `localhost` as a secure context. HTTPS would need a browser-trusted certificate on every machine.
- **IPv6:** only `127.0.0.1` is bound (a dynamic port cannot be shared by two addresses). Browsers reach it by that
  address, which the launch link uses.

## API reference

All public types are in the `Forge.Next.LocalWebShell` namespace. Their names do not repeat the namespace (`Options`,
`Session`, `ErrorCodes`), so a few are short and general. Two of them can clash with another namespace in the same file:
- `Options`, if the file also imports `Microsoft.Extensions.Options`, which has a static class of that name;
- `Session`, if the file has a type of that name of its own.

In such a file, qualify the name (`Forge.Next.LocalWebShell.Options`) or alias it:

```csharp
using ShellOptions = Forge.Next.LocalWebShell.Options;
```

The configuration delegate of `AddLocalWebShell` needs no type name at all: `builder.AddLocalWebShell(options => options.Port = 5123)`.

### `WebApplicationBuilderExtensions`

| Member | Description |
| --- | --- |
| `WebApplicationBuilder AddLocalWebShell(this WebApplicationBuilder builder, Action<Options>? configure = null)` | Registers the shell: binds the options, validates them on build and start, makes Kestrel listen on `127.0.0.1:<Port>`, inserts the gate first in the pipeline, filters the hosting diagnostics log to Warning, and adds the default launcher if none is registered. Idempotent. |

### `WebApplicationExtensions`

| Member | Description |
| --- | --- |
| `Task<ErrorOr<Session>> StartLocalWebShellAsync(this WebApplication app, CancellationToken cancellationToken = default)` | Starts the server, checks the addresses, builds the launch link, and opens the browser if asked. See [Errors](#errors). Wait afterwards with `app.WaitForShutdownAsync()`. |

### `Session`

| Member | Description |
| --- | --- |
| `Uri Address` | Where the server listens, e.g. `http://127.0.0.1:54321/`. Without the cookie it shows the 401 page. |
| `Uri LaunchUrl` | `Address` with `?token=…`. A secret: print it, never log it. Valid for the process's lifetime. |
| `BrowserState Browser` | `NotRequested`, `Opened` or `NotOpened`. |
| `Error? BrowserError` | Why the browser was not opened, or `null`. |
| `string ToString()` | Address and browser state, without the token. |

### `Options`

`Port`, `OpenBrowser`, `CookieName`, `CookieSameSite`, `UnauthorizedTitle`, `UnauthorizedMessage`, and the constant
`SectionName` (`"LocalWebShell"`). See [Configuration](#configuration).

### `ILocalBrowserLauncher`

| Member | Description |
| --- | --- |
| `ErrorOr<Success> Open(Uri url)` | Opens the launch link. The default: the shell, or the desktop shell when elevated, on Windows; `xdg-open` in a Linux graphical session, not as root; `open` on macOS, not as root. Otherwise an error that says why. |

### `BrowserState`

`NotRequested`, `Opened`, `NotOpened`.

### `ErrorCodes`

`PortInUse`, `NotLoopback`, `NoAddress`, `BrowserNotOpened` (string constants, `LocalWebShell.*`).

---

## License

Licensed under the **Apache-2.0** license. See the
[repository](https://github.com/JZO001/Forge.Next.LocalWebShell) for details.
