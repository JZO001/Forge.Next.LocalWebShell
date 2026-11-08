# Design

## The parts

| File | Visibility | Role |
| --- | --- | --- |
| `WebApplicationBuilderExtensions.cs` | public | `AddLocalWebShell`: binds and validates `Options`, registers the secrets, the launcher (if none), Kestrel's loopback endpoint and the startup filter; filters the hosting diagnostics log to Warning |
| `WebApplicationExtensions.cs` | public | `StartLocalWebShellAsync`: start, read and check the addresses, build the launch link, open the browser; returns `ErrorOr<Session>` |
| `Options.cs`, `OptionsValidation.cs` | public / internal | The settings (`LocalWebShell` section) and their check (on build and on start) |
| `Session.cs`, `BrowserState.cs` | public | The result of the start: address, launch link, browser state and error; `ToString` without the token |
| `ErrorCodes.cs`, `Errors.cs` | public / internal | The stable error codes and the factories that build the errors |
| `ILocalBrowserLauncher.cs` | public | The replaceable launcher interface |
| `KestrelSetup.cs` | internal | `IConfigureOptions<KestrelServerOptions>`: `Listen(IPAddress.Loopback, Port)` |
| `StartupFilter.cs` | internal | Inserts `Gate` before the application's pipeline |
| `Gate.cs` | internal | The middleware: Host check, token → cookie + redirect, cookie check, 401 page |
| `Secrets.cs` | internal | The launch token and the cookie value: 32 random bytes each, URL-safe base64, constant-time comparison |
| `LoopbackHosts.cs` | internal | The allowed Host names; whether a server address is loopback |
| `UnauthorizedPage.cs` | internal | The static 401 page (HTML-encoded texts, no script) |
| `Browser/BrowserRouting.cs` | internal | The pure decision: platform × privilege × environment → route or reason |
| `Browser/BrowserPlatform.cs`, `BrowserRoute.cs`, `BrowserDecision.cs` | internal | The decision's types |
| `Browser/LocalBrowserLauncher.cs` | internal | The default launcher: carries the decision out (shell, desktop shell, `xdg-open`, `open`) |
| `Browser/DesktopShell.cs`, `Browser/Interop/*` | internal | Windows: `ShellExecute` through Explorer, so the browser starts unelevated (COM on an STA thread, 10 s limit) |

## A launch, step by step

1. **`AddLocalWebShell`:**
   - Kestrel will listen on `127.0.0.1:<Port>`;
   - the gate will be first;
   - two secrets are created: the launch token and the cookie value.
2. **`StartLocalWebShellAsync`:**
   1. `app.StartAsync()`; a taken fixed port gives `PortInUse` (`IOException` with `AddressInUseException` inside);
   2. the addresses from `IServerAddressesFeature`: any non-loopback one stops the server (`NotLoopback`);
   3. the launch link `http://127.0.0.1:<port>/?token=<token>`;
   4. the browser, if `OpenBrowser` is on (an exception from a replaced launcher becomes `BrowserNotOpened`).
3. **The first request:** the gate checks the Host header (400 if foreign). It sees the token, sets the cookie
   `Forge.LocalWebShell-<port>`, and answers 302 to the same path without the token (`Cache-Control: no-store`,
   `Referrer-Policy: no-referrer`).
4. **Later requests:** they pass with the cookie. Without the token and the cookie: the 401 page.

## The browser decision (`BrowserRouting.Choose`)

| Platform | Privileged | Graphical session | Route |
| --- | --- | --- | --- |
| Windows | no | – | `ShellOpen`: `Process.Start` with `UseShellExecute` |
| Windows | yes (elevated) | – | `DesktopShell`: Explorer's `ShellExecute`; if it fails, nothing |
| Linux, FreeBSD | yes (root) | any | nothing: "runs as root" |
| Linux, FreeBSD | no | no `DISPLAY` / `WAYLAND_DISPLAY` | nothing: "no graphical session … ssh -L" |
| Linux, FreeBSD | no | yes | `XdgOpen`: `xdg-open <link>`, stdin closed |
| macOS | yes | – | nothing: "runs as root" |
| macOS | no | – | `MacOpen`: `open <link>` |
| other | – | – | nothing: "not supported" |

"Privileged" is `Environment.IsPrivilegedProcess`: an elevated token on Windows, root on Unix.

## Why each choice was made

- **Loopback, and no address setting:** a local admin tool reachable from the network is what the package prevents.
- **A token at all:** a loopback port is machine-wide. Other users (RDP or SSH), other processes, and web pages through
  the user's browser can reach it, and the consuming apps run elevated and hold keys.
- **Port 0 by default:** no collisions, no interactive port prompt. With the token, a fixed port buys little, because a
  bookmark has no token.
- **IPv4 only:** a dynamic port cannot be shared by `127.0.0.1` and `[::1]`; the launch link uses `127.0.0.1`.
- **The token reusable for the process lifetime** (Zoltán's choice): the console link reopens a closed tab. A one-time
  token plus a console key was less convenient for no real gain, because the link is visible only in the user's console
  and history.
- **A separate cookie value:** the token never sits in the cookie jar.
- **`SameSite=Strict`:** a foreign page cannot send the cookie, not even on a WebSocket (CORS does not apply to
  WebSockets). `Lax` remains a setting for a browser that drops a strict cookie on the first navigation; headless
  Chromium kept it.
- **The port in the cookie name:** several instances were allowed (Zoltán), and browsers do not separate cookies by
  port.
- **A startup filter:** the gate's position cannot be wrong.
- **Settings validation on build:** Kestrel reads the port when the application is built, so invalid settings throw
  from `builder.Build()` already; `ValidateOnStart` covers the rest.
- **Filtering `Microsoft.AspNetCore.Hosting.Diagnostics`:** it logs full URLs, and the first one carries the token. The
  filter works for providers added through `builder.Logging`. A replaced logger factory (`UseSerilog()`) needs the same
  rule in its own configuration; the README says so.
- **Opening the browser after the bind:** before it, a taken port would open whatever holds the port.
- **The desktop shell for an elevated Windows process:** the documented technique (Raymond Chen, "How can I launch an
  unelevated process from my elevated process and vice versa?"). Fallbacks like `explorer.exe <url>` or `cmd /c start`
  were rejected: whether they run unelevated depends on Explorer settings and on whether a browser is already running.
- **No browser as root on Unix:** no `sudo -u $SUDO_USER` gymnastics. The session variables (`DISPLAY`, `XAUTHORITY`,
  the D-Bus address) differ between setups, and a wrong guess opens a root browser or nothing useful. Printing the link
  is predictable.
- **No browser without a graphical session:** `xdg-open` falls back to text browsers in the terminal (seen in the Docker
  check), which would take over the application's console. The SSH tunnel (`ssh -L`) is the documented way to use a
  headless server.
- **`xdg-open`'s standard input closed:** nothing it starts can read the console. Its output is not redirected, because
  a closed output pipe could kill the browser it starts (SIGPIPE).
- **Console output left to the application:** applications print in their own style (plain `Console`, Spectre.Console).
