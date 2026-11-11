# History and decisions

Newest last. Each entry: what was asked or decided, by whom, and why.

## 2026-11-08 – The package is created

**The need.** A local admin tool needs a UI. A WinForms or WPF window with a WebView2 control needs the WebView2 runtime,
or a recent Edge, on the machine, and Zoltán knows from experience that customers do not accept installing
prerequisites. The alternative needs only .NET: a console process with Blazor Server on localhost, opened in the user's
own browser.

**Why a package.** A local shell of this kind is only safe with protections a plain setup does not have:
- authenticating the browser;
- opening the browser only after the bind;
- an unelevated browser;
- a Host header check.

Zoltán decided to put them into a reusable package of their own, so any local admin tool can use them.

**Decided with Zoltán the same day** (the reasons in `DESIGN.md`):

| Topic | Decision |
| --- | --- |
| Binding | Loopback only (`127.0.0.1`), not configurable; HTTP, no HTTPS |
| Port | The operating system picks one (port 0); a fixed port is a setting |
| Launch token lifetime | Reusable for the process lifetime. Zoltán chose this over a one-time token plus a console key. |
| Several instances | Allowed: each has its own port and token. This led to the port in the cookie name, because browsers do not separate cookies by port. |
| Elevation | Admin tools built on the package often run elevated, so the browser must be opened unelevated (Windows desktop shell) |
| Console output | The application's job; the package returns the session. Spectre.Console is fine in an application. |
| Verification | Tests, the elevated Windows check, and a real-browser check (headless Chromium) |

**Deliverables:**
- the project (net8.0, net9.0, net10.0; ErrorOr 2.1.1), 87 tests, a sample;
- the README (user documentation), `CLAUDE.md`;
- the package `artifacts/Forge.Next.LocalWebShell.1.0.0.nupkg`, version 1.0.0.0.

Zoltán creates the GitHub repository (`JZO001/Forge.Next.LocalWebShell`) and publishes the package.

## 2026-11-08 – Type names without the namespace

Zoltán: the `LocalWebShell` prefix on the type names repeats the namespace and is confusing. All types lost it:
- **public:** `Options`, `Session`, `ErrorCodes`, `BrowserState`, `WebApplicationBuilderExtensions`,
  `WebApplicationExtensions`;
- **internal:** `Gate`, `Secrets`, `StartupFilter`, `KestrelSetup`, `UnauthorizedPage`, `OptionsValidation`,
  `Errors`.

**What stayed:** the methods (`AddLocalWebShell`, `StartLocalWebShellAsync`), the configuration section, the error
codes and the cookie name, where the namespace is not beside them. `ILocalBrowserLauncher` did not start with
`LocalWebShell` and was left; renaming it to `IBrowserLauncher` was offered (open, see `OPEN.md`).

**Side effect:** `Options` can clash with `Microsoft.Extensions.Options.Options` in a consumer file that imports both.
The README explains the alias.

## 2026-11-08 – Linux (and macOS) support

**Zoltán's question:** the package looks Windows-only; can it run on Linux?

**The answer:** it already did. The targets are plain `net8.0` to `net10.0`, the Windows-only code is behind
`OperatingSystem.IsWindows()`, and the CA1416 analyzer reported nothing.

**Two gaps fixed at his request:**
- **A Linux machine without a graphical session:** `xdg-open` fails or starts a text browser in the terminal. Now
  nothing is opened without `DISPLAY` or `WAYLAND_DISPLAY`, and the reason names the SSH tunnel.
- **Root on Linux or macOS** (the analogue of an elevated Windows process): now no browser is opened as root. The check
  became `Environment.IsPrivilegedProcess` on every platform, replacing the Windows-only `WindowsPrincipal` check.

**The refactoring:** the decision moved into a pure function (`BrowserRouting.Choose`), so every platform combination is
tested on any machine.

**Verification:**
- Linux in Docker (`tools/run-linux-check.ps1`): the test suite and the sample, as root, as a normal user without a
  session, with `DISPLAY` and without and with `xdg-open`, and the HTTP flow. All passed.
- The run also showed `xdg-open` trying `lynx`, `w3m` and `links`, which confirms the terminal-browser risk.
- The real-browser check moved into the repository (`tools/BrowserCheck`).

**What Zoltán added:** use Docker and Hyper-V for testing as needed; the version stays 1.0.0 (not published yet); keep
this knowledge base current.

**State at the end of the day:** 98 tests on Windows and on Linux, a 0-warning build. The package was rebuilt as
1.0.0, still not published.

## 2026-11-08 – No references to other projects

Zoltán: this repository must not refer to any other project. The names of other products, repositories and his other
packages, and decision numbers from elsewhere, were removed from the documents and this knowledge base. The rule is in
`CLAUDE.md`. Third-party libraries and tools the package uses or documents stay.

## 2026-11-08 – `ILocalBrowserLauncher` keeps its name

The rename to `IBrowserLauncher`, offered after the type-name change, is not done: Zoltán approved keeping the name.
`ILocalBrowserLauncher` does not repeat the namespace's last segment the way the old `LocalWebShell…` names did, and
`IBrowserLauncher` would be generic enough to clash with a consumer's own browser abstraction. The item left `OPEN.md`.

## 2026-11-11 – 1.1.0: the 401 page in the browser's language; `IBrowserLauncher`

1.0.0 is published. A consuming admin tool shows its whole UI in the user's language, and the 401 page was the one place
it could not follow the browser: the settings took a single title and message per process.

**Zoltán's decisions:**
- the 401 page follows the browser's language;
- the reason in `BrowserNotOpened` stays English: the application can say the main sentence in its own language from
  the error code, and the reason is diagnostics;
- the launcher interface is renamed to `IBrowserLauncher` after all, as a breaking change in 1.1.0: the package has only
  his own applications as consumers, so a breaking minor version is acceptable. This reverses the 2026-11-08 decision to
  keep `ILocalBrowserLauncher`. The internal default launcher became `BrowserLauncher`.

**The design:**
- `UnauthorizedLanguage` (default `en`) names the language of the existing two texts;
- `UnauthorizedTexts` adds further languages, keyed by language tag, from code or configuration;
- the gate matches `Accept-Language` against the keys (see `DESIGN.md`).

The page also gained `<html lang>`, `Content-Language` and `Vary: Accept-Language`. It keeps accented letters as they
are: the first end-to-end test showed the default `HtmlEncoder` writing `é` as `&#xE9;`. The sample shows a Hungarian
401 page, and `tools/BrowserCheck` checks it in a Hungarian browser context.

**Verification:** see `VERIFICATION.md`, 2026-11-11.

## 2026-11-11 – The `net11.0` target

Zoltán added `net11.0` to the package's targets, in 1.1.0, before it was published. Its consequences for the repository:
- building the package needs the .NET 11 SDK: the .NET 10 SDK refuses even to restore a project that targets
  `net11.0` (`NETSDK1045`), and so does any project that references it;
- the tests run on `net10.0` and `net11.0`, so the new target is run, not only built; the sample and
  `tools/BrowserCheck` stay `net10.0`, the target the package's consumers use most;
- the Linux checks moved to `mcr.microsoft.com/dotnet/sdk:11.0` (Ubuntu 26.04). That image carries only the .NET 11
  runtime, so `tools/linux-net10-runtime.sh` adds the ASP.NET Core 10 runtime with Microsoft's `dotnet-install` script
  for the `net10.0` tests and the sample. The checks already needed network for the restore.

**Verification:** see `VERIFICATION.md`, 2026-11-11, the `net11.0` rows.

## 2026-11-11 – The no-reference rule covers the consumers

Zoltán clarified the 2026-11-08 rule in `CLAUDE.md`: it bans the names of the products, repositories and internal
decisions of the projects that use, or will use, this package. The other `Forge.*` projects may be named, and so may the
libraries and tools the package uses or documents, the `Microsoft.*` libraries among them.
