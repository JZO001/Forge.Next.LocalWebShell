# Open items

As of 2026-11-08. Close an item by moving its outcome into `HISTORY.md` (decisions) or `VERIFICATION.md` (checks), and
deleting it here.

## Waiting for Zoltán

- **Publishing:** the package was built as 1.0.0 (`artifacts/`) and is not published yet. Zoltán creates the
  repository and publishes it. Until then, changes need no version bump; after it, every change does.
- **`ILocalBrowserLauncher` → `IBrowserLauncher`?** Left as it is after the 2026-11-08 rename, because it did not start
  with `LocalWebShell`. Offered as an option; no answer yet.

## Not verified yet

- **Windows Server:** an elevated application opening the real default browser through the desktop shell, with Edge,
  Chrome and Firefox as the default browser. The elevated check in the tests proves the mechanism (Medium integrity),
  not every browser. A Hyper-V VM may be used (Zoltán allowed Docker and Hyper-V for testing).
- **A real Linux desktop:** `xdg-open` opening a browser in an X11 and in a Wayland session. The Docker check covers
  the decision and the process start, not a desktop.
- **macOS:** not tried at all; the route (`open`, not as root) is covered only by the decision's tests.

## Ideas, not decided

- **A Linux desktop check in CI:** a container with a virtual X server (Xvfb) and a browser could run the `xdg-open`
  route for real. Not needed so far.
- **The console of a consuming app on Linux:** closing the terminal sends SIGHUP. ASP.NET Core's console lifetime
  handles SIGINT, SIGQUIT and SIGTERM. This is the application's concern, not the package's; mention it to a consumer
  that targets Linux.
