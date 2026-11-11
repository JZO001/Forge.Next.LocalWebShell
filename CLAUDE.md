# Forge.Next.LocalWebShell – notes for AI sessions working on this repository

This repository holds one NuGet package: a safe local browser shell for ASP.NET Core admin apps (Blazor Server in the
user's own browser, no WebView2). Read in this order:

1. `README.md`: the user documentation: what the package does, why, and how to use it.
2. This file: the rules that must hold in every change.
3. `AI/README.md`: the knowledge base: how the package came about and what was decided (`AI/HISTORY.md`), the design
   (`AI/DESIGN.md`), how it is verified (`AI/VERIFICATION.md`), and what is still open (`AI/OPEN.md`).

## Working with Zoltán

- **Language:** Zoltán Juhász (JZO Tech Ltd) owns the package. He writes in Hungarian; answer in Hungarian. Code,
  comments, commit messages and every document in this repository are in English.
- **Git and publishing are his:** he creates and manages the git repository and publishes the NuGet package. Do not
  run git commands or publish unless he asks.
- **Working style:**
  - one topic at a time;
  - options with a recommendation;
  - facts checked before they are stated;
  - after a work item, report and stop.
- **Keep the knowledge base current:** when a change, a decision or a verification happens, update `AI/` in the same
  step, so the next session can continue from it. Zoltán asked for this explicitly.
- **No reference to the package's consumers** (Zoltán, 2026-11-08, clarified 2026-11-11): no name of a product,
  repository or internal decision of a project that uses, or will use, this package, in the code, the documents or
  this knowledge base. Describe a consumer generically ("a local admin tool", "a hand-written local shell"). Allowed:
  - the other `Forge.*` projects;
  - the libraries and tools this package uses or documents: ASP.NET Core and the other `Microsoft.*` libraries,
    ErrorOr, Serilog, Playwright, Docker.

## Invariants (do not break)

1. **Loopback only.** Kestrel listens on `127.0.0.1`. The address is not a setting. After the start every reported
   address is checked, and a non-loopback one stops the server (`LocalWebShell.NotLoopback`).
2. **The gate is first.** It is inserted by `StartupFilter`, before all application middleware. Static files,
   `/_blazor` and every endpoint are behind it. Do not turn it into a `Use…` call the application has to place.
3. **Secrets never reach a log or a string.**
   - Do not log the token, the cookie value, `LaunchUrl` or any query string.
   - `Secrets.ToString()` and `Session.ToString()` omit them; keep it so, and keep the tests.
   - `AddLocalWebShell` filters `Microsoft.AspNetCore.Hosting.Diagnostics` to Warning, because it logs full URLs.
4. **The token is not the cookie.** The cookie carries a separate random value, so the token never sits in the cookie
   jar. Comparisons use `CryptographicOperations.FixedTimeEquals`.
5. **The cookie name contains the port.** Browsers do not separate cookies by port; two instances on one machine must
   not overwrite each other.
6. **The cookie:**
   - `HttpOnly`, `SameSite` `Strict` (or `Lax` by choice), never `None`, `Path=/`, session-only;
   - `Secure` off (loopback HTTP).
7. **The redirect after the token never leaves the shell.** A path like `//host` or `/\host` becomes `/`.
8. **Never open a privileged browser.**
   - **Windows, elevated:** only the desktop shell route (`DesktopShell`). If it fails, nothing is opened. Do not add an
     `explorer.exe` or `cmd /c start` fallback: whether those run unelevated depends on Explorer settings.
   - **Linux and macOS as root:** no browser at all.
   - The check is `Environment.IsPrivilegedProcess`.
9. **Never start a browser inside the terminal.** On Linux, without `DISPLAY` or `WAYLAND_DISPLAY` nothing is opened
   (`xdg-open` would fall back to `lynx`, `w3m` and the like). `xdg-open` gets its standard input closed.
10. **The browser opens after the server listens and the address is checked**, never before.
11. **The decision is a pure function.** `BrowserRouting.Choose` decides from the platform, the privilege and the
    environment; `BrowserLauncher` only carries it out. New platform rules go into the function and its tests.
12. **Expected failures are `ErrorOr` errors** with stable codes (`ErrorCodes`). Programming errors throw: a missing
    registration, invalid settings.

## Conventions

- **Type names do not repeat the namespace** (Zoltán, 2026-11-08): `Options`, `Session`, `ErrorCodes`, `BrowserState`,
  `WebApplicationBuilderExtensions`, and internally `Gate`, `Secrets`, `StartupFilter`. The `LocalWebShell` name stays
  where the namespace is not beside it:
  - the methods `AddLocalWebShell` and `StartLocalWebShellAsync`;
  - the configuration section `LocalWebShell`;
  - the error codes `LocalWebShell.*`;
  - the cookie `Forge.LocalWebShell-<port>`.

  The README tells consumers how to handle a clash of `Options` with `Microsoft.Extensions.Options`.
- **Code style:**
  - explicit types (no `var`), file-scoped namespaces, one type per file named after the type;
  - primary constructors, collection expressions, `LangVersion` 12 for the `net8.0` target;
  - XML documentation on every public and protected member, saying what null means, what errors come back, and why.
- **The COM interop interfaces** in `Browser/Interop` declare the methods before the one called, to keep the vtable
  order. Do not reorder them.
- **Targets:** the package targets `net8.0` to `net11.0`, so building it needs the .NET 11 SDK. The tests run on
  `net10.0` and `net11.0`; the sample and `tools/BrowserCheck` are `net10.0`. The Linux checks run in
  `mcr.microsoft.com/dotnet/sdk:11.0` and add the .NET 10 runtime (`tools/linux-net10-runtime.sh`).
- **Tests:**
  - xUnit 2, Shouldly, NSubstitute;
  - one test class per tested class (`<Class>Tests`), named `Member_WhenCondition_ShouldOutcome`;
  - real Kestrel through `TestHelpers/TestShell`.
- **Before handing over a change:**
  - a 0-warning build of the solution;
  - all tests, from an elevated terminal on Windows so `DesktopShellTests` really checks;
  - `pwsh tools/run-linux-check.ps1` when anything platform-related changed, and with `-Desktop` when the browser route
    on Linux changed;
  - `tools/BrowserCheck` when the gate or the cookie changed.

  See `AI/VERIFICATION.md`.
- **Release:**
  1. Raise `AssemblyVersion` in `Forge.Next.LocalWebShell.csproj`, unless the version was never published.
  2. Run `dotnet build -c Release`; it writes the package to `artifacts/` (`GeneratePackageOnBuild`).
  3. Zoltán publishes it.
