using ErrorOr;

namespace Forge.Next.LocalWebShell;

/// <summary>
/// Opens the launch link in the user's browser. The default implementation uses the default browser and never opens a
/// privileged browser or one inside the terminal:
/// <list type="bullet">
/// <item>on Windows, when the application runs elevated, it asks the desktop shell (Explorer) to open the link, so the
/// browser starts with the user's normal rights;</item>
/// <item>on Linux it uses <c>xdg-open</c>, only in a graphical session (<c>DISPLAY</c> or <c>WAYLAND_DISPLAY</c>);</item>
/// <item>on macOS it uses <c>open</c>;</item>
/// <item>as root on Linux or macOS it opens nothing.</item>
/// </list>
/// Whenever the safe way is not possible it opens nothing and returns the reason, rather than open a privileged browser.
/// </summary>
/// <remarks>
/// Register your own implementation before calling
/// <see cref="WebApplicationBuilderExtensions.AddLocalWebShell"/> to replace it (for example to open a
/// specific browser, or in tests); the shell registers the default only when none is registered.
/// </remarks>
public interface IBrowserLauncher
{
    /// <summary>Opens <paramref name="url"/> in the browser.</summary>
    /// <param name="url">The launch link; it carries the launch token, so do not log it.</param>
    /// <returns>
    /// Success when the browser was asked to open the link, or an error with
    /// <see cref="ErrorCodes.BrowserNotOpened"/> and the reason. Do not throw for an expected failure.
    /// </returns>
    ErrorOr<Success> Open(Uri url);
}
