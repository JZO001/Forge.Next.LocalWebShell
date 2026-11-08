# Verification

## The checks

| Check | How | What it proves |
| --- | --- | --- |
| Build | `dotnet build Forge.Next.LocalWebShell.slnx -c Debug --no-incremental` | 0 warnings for net8.0, net9.0 and net10.0. The CA1416 platform analyzer is on, so an unguarded Windows-only call would show. |
| Tests | `dotnet test tests/Forge.Next.LocalWebShell.Tests` | 98 tests. Most run a real Kestrel on a free loopback port with real HTTP; the browser decision is tested for every platform. |
| Elevated Windows | The tests from an elevated terminal | `Browser/DesktopShellTests` starts a hidden `cmd.exe /c whoami /groups` through the desktop shell, and checks the Medium integrity SID (`S-1-16-8192`) and the absence of High (`S-1-16-12288`). Not elevated, it returns without asserting and says "Not verified" in its output (`--logger "console;verbosity=detailed"`). |
| Linux | `pwsh tools/run-linux-check.ps1` (add `-InstallXdgUtils` for the `xdg-open` start) | In `mcr.microsoft.com/dotnet/sdk:10.0`: the tests; the sample as root, as a normal user without a session, and with `DISPLAY` without and with `xdg-open`; the HTTP flow (401, admitted, 200, 400 for a foreign Host). Needs Docker with a Linux engine. |
| Real browser | `tools/BrowserCheck` (usage in its `Program.cs`) | In headless Chromium: the launch link lands on the page (the `Strict` cookie survives the redirect), the token leaves the address bar, a reload stays in, a fresh context gets 401 |

## What was run

| Date | Check | Where | Result |
| --- | --- | --- | --- |
| 2026-11-08 | Build, tests (87) | Windows 11, elevated terminal | 0 warnings, 87/87; the elevated check verified (Medium) |
| 2026-11-08 | Real browser | Windows 11, headless Chromium (Playwright .NET 1.64.1) | PASS |
| 2026-11-08 | After the rename: build, tests (87), real browser | Windows 11 | 0 warnings, 87/87, PASS |
| 2026-11-08 | After the Linux changes: build, tests (98) | Windows 11, elevated | 0 warnings, 98/98; the elevated check verified |
| 2026-11-08 | Linux check, with `-InstallXdgUtils` | Docker Desktop, `sdk:10.0` | 98/98; root → "runs as root"; headless user → "no graphical session"; `DISPLAY` without `xdg-open` → "could not be started"; with `xdg-open` → started (it then tried `lynx`, `w3m`, `links`: no browser in the container); HTTP flow 401 / admitted / 200 / 400; PASS |
| 2026-11-08 | Real browser (`tools/BrowserCheck`) | Windows 11 | PASS |

## Not verified yet

See `OPEN.md`. In short:
- a real default browser opened from an elevated process on Windows Server, and with Firefox as the default browser;
- a real Linux desktop;
- macOS.
