# Open items

As of 2026-11-11. Close an item by moving its outcome into `HISTORY.md` (decisions) or `VERIFICATION.md` (checks), and
deleting it here.

## Waiting for Zoltán

- **Publishing 1.1.0:** 1.0.0 is published. 1.1.0 (the 401 page in the browser's language, `IBrowserLauncher`, the
  `net11.0` target) is built into `artifacts/` and waits for Zoltán to publish it. Every change after a published
  version raises the version.

## Not verified yet

- **Windows Server:** an elevated application opening the real default browser through the desktop shell, with Edge,
  Chrome and Firefox as the default browser. The elevated check in the tests proves the mechanism (Medium integrity),
  not every browser. A Hyper-V VM may be used (Zoltán allowed Docker and Hyper-V for testing).
- **A real Linux desktop:** `xdg-open` with a desktop environment (GNOME, KDE) choosing the browser, and a Wayland
  session. The desktop check (`run-linux-check.ps1 -Desktop`) covers X11 through a virtual X server with Firefox, without
  a desktop environment, where `xdg-open` picks `x-www-browser`.
- **macOS:** not tried at all; the route (`open`, not as root) is covered only by the decision's tests.

## Ideas, not decided

- **The console of a consuming app on Linux:** closing the terminal sends SIGHUP. ASP.NET Core's console lifetime
  handles SIGINT, SIGQUIT and SIGTERM. This is the application's concern, not the package's; mention it to a consumer
  that targets Linux.
