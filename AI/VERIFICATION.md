# Verification

## The checks

| Check | How | What it proves |
| --- | --- | --- |
| Build | `dotnet build Forge.Next.LocalWebShell.slnx -c Debug --no-incremental` | 0 warnings for net8.0, net9.0, net10.0 and net11.0; needs the .NET 11 SDK. The CA1416 platform analyzer is on, so an unguarded Windows-only call would show. |
| Tests | `dotnet test tests/Forge.Next.LocalWebShell.Tests` | 141 tests, run on net10.0 and on net11.0. Most run a real Kestrel on a free loopback port with real HTTP; the browser decision is tested for every platform. |
| Elevated Windows | The tests from an elevated terminal | `Browser/DesktopShellTests` starts a hidden `cmd.exe /c whoami /groups` through the desktop shell, and checks the Medium integrity SID (`S-1-16-8192`) and the absence of High (`S-1-16-12288`). Not elevated, it returns without asserting and says "Not verified" in its output (`--logger "console;verbosity=detailed"`). |
| Linux | `pwsh tools/run-linux-check.ps1` (add `-InstallXdgUtils` for the `xdg-open` start) | In `mcr.microsoft.com/dotnet/sdk:11.0` (Ubuntu 26.04), with the .NET 10 runtime added by `tools/linux-net10-runtime.sh`, because the image has only .NET 11 and an SDK 10 image cannot restore the `net11.0` target: the tests on both runtimes; the sample as root, as a normal user without a session, and with `DISPLAY` without and with `xdg-open`; the HTTP flow (401, admitted, 200, 400 for a foreign Host). Needs Docker with a Linux engine. |
| Linux desktop | `pwsh tools/run-linux-check.ps1 -Desktop` (`tools/linux-desktop-check.sh`; needs network) | In the same image, with the same .NET 10 runtime for the `net10.0` sample: a virtual X server (Xvfb), Firefox from Mozilla's APT repository (Ubuntu's own package is a snap stub), the sample as the image's normal user with `DISPLAY` set. Passes when Firefox's window title is the sample page's title, which only the page behind the launch cookie has: `xdg-open` started the browser with the launch link, the `Strict` cookie was set and kept over the redirect, and the page was served. |
| Real browser | `tools/BrowserCheck` (usage in its `Program.cs`) | In headless Chromium: the launch link lands on the page (the `Strict` cookie survives the redirect), the token leaves the address bar, a reload stays in, a fresh context gets 401 in English, and a fresh context with a Hungarian locale gets the sample's Hungarian 401 page (`lang="hu"`) |

## What was run

| Date | Check | Where | Result |
| --- | --- | --- | --- |
| 2026-11-08 | Build, tests (87) | Windows 11, elevated terminal | 0 warnings, 87/87; the elevated check verified (Medium) |
| 2026-11-08 | Real browser | Windows 11, headless Chromium (Playwright .NET 1.64.1) | PASS |
| 2026-11-08 | After the rename: build, tests (87), real browser | Windows 11 | 0 warnings, 87/87, PASS |
| 2026-11-08 | After the Linux changes: build, tests (98) | Windows 11, elevated | 0 warnings, 98/98; the elevated check verified |
| 2026-11-08 | Linux check, with `-InstallXdgUtils` | Docker Desktop, `sdk:10.0` | 98/98; root → "runs as root"; headless user → "no graphical session"; `DISPLAY` without `xdg-open` → "could not be started"; with `xdg-open` → started (it then tried `lynx`, `w3m`, `links`: no browser in the container); HTTP flow 401 / admitted / 200 / 400; PASS |
| 2026-11-08 | Real browser (`tools/BrowserCheck`) | Windows 11 | PASS |
| 2026-11-08 | Linux desktop check (`-Desktop`) | Docker Desktop, `sdk:10.0` (Ubuntu 24.04), Xvfb, Firefox 159.0.1 | `xdg-open` started Firefox with the launch link (through `x-www-browser`); Firefox's window: "Forge.Next.LocalWebShell sample — Mozilla Firefox"; PASS. The first run failed only in the check itself: `xwininfo` in the C locale could not print the title's non-ASCII dash; it now runs with `C.UTF-8`. Firefox warned that it cannot create a user namespace in the container (its sandbox), and ran. |
| 2026-11-08 | Linux check, after the launcher script gained `-Desktop` | Docker Desktop, `sdk:10.0` | 98/98; the sample's decisions as before; HTTP flow 401 / admitted / 200 / 400; PASS |
| 2026-11-11 | 1.1.0 (the 401 page's languages, `IBrowserLauncher`): build, tests (141) | Windows 11, elevated | 0 warnings for net8.0, net9.0 and net10.0; 141/141; the elevated check verified (Medium) |
| 2026-11-11 | Real browser (`tools/BrowserCheck`, with the Hungarian context) | Windows 11, headless Chromium | English stranger: 401, `lang=en`; Hungarian stranger: 401, `lang=hu`, the Hungarian title; PASS |
| 2026-11-11 | Linux check, with `-InstallXdgUtils` | Docker Desktop, `sdk:10.0` | 141/141; the sample's decisions as before (root, no graphical session, no `xdg-open`); HTTP flow 401 / 200 / 400; PASS |
| 2026-11-11 | Release build | Windows 11 | `artifacts/Forge.Next.LocalWebShell.1.1.0.nupkg`: version 1.1.0, the three targets, README and icon; the XML documentation names `IBrowserLauncher` only |
| 2026-11-11 | `net11.0` target: the .NET 10 SDK | Windows 11, a copy pinned to SDK 10.0.404 | the restore fails with `NETSDK1045`, so the Linux checks left `sdk:10.0` |
| 2026-11-11 | `net11.0` target: build, tests (141 on each runtime) | Windows 11, elevated, SDK 11.0.100 | 0 warnings for net8.0, net9.0, net10.0 and net11.0; 141/141 on net10.0 and 141/141 on net11.0; the elevated check verified (Medium) on both |
| 2026-11-11 | `net11.0` target: Linux check, with `-InstallXdgUtils` | Docker Desktop, `sdk:11.0` (Ubuntu 26.04) + ASP.NET Core 10.0.15 | 141/141 on net10.0 and on net11.0; the sample's decisions as before (root, no graphical session, no `xdg-open`, `xdg-open` started); HTTP flow 401 / admitted / 200 / 400; PASS |
| 2026-11-11 | `net11.0` target: Linux desktop check (`-Desktop`) | Docker Desktop, `sdk:11.0` (Ubuntu 26.04) + ASP.NET Core 10.0.15, Xvfb, Firefox 160.0 | `xdg-open` started Firefox with the launch link (through `x-www-browser`); Firefox's window: "Forge.Next.LocalWebShell sample — Mozilla Firefox"; PASS. Firefox again warned about its sandbox's user namespace, and ran. |
| 2026-11-11 | `net11.0` target: Release build | Windows 11 | `artifacts/Forge.Next.LocalWebShell.1.1.0.nupkg`: version 1.1.0, the four targets (`lib/net8.0` to `lib/net11.0`), README with the `net11.0` row and note, and icon. `tools/BrowserCheck` was not run again: the sample and the gate did not change. |

## Not verified yet

See `OPEN.md`. In short:
- a real default browser opened from an elevated process on Windows Server, and with Firefox as the default browser;
- a Linux desktop environment (GNOME, KDE) and Wayland; X11 without a desktop environment is verified by the desktop check;
- macOS.
