namespace Forge.Next.LocalWebShell;

/// <summary>What happened to the browser when the shell started.</summary>
public enum BrowserState
{
    /// <summary><see cref="Options.OpenBrowser"/> was off; nothing was opened.</summary>
    NotRequested,

    /// <summary>The browser was asked to open the launch link.</summary>
    Opened,

    /// <summary>
    /// The browser could not be opened (see <see cref="Session.BrowserError"/>). The server runs; print the
    /// launch link for the user.
    /// </summary>
    NotOpened,
}
