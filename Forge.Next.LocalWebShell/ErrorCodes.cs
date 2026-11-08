namespace Forge.Next.LocalWebShell;

/// <summary>
/// The codes of the expected failures the shell returns as <c>ErrorOr</c> errors. They are stable: an application may
/// branch on them.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// The fixed port set in <see cref="Options.Port"/> is already used on the loopback address. An
    /// <c>ErrorType.Conflict</c> error. Choose another port, or leave <see cref="Options.Port"/> at <c>0</c>.
    /// </summary>
    public const string PortInUse = "LocalWebShell.PortInUse";

    /// <summary>
    /// The server listens on an address that is not a loopback address, typically because the application's
    /// configuration adds a Kestrel endpoint (<c>Kestrel:Endpoints</c>). The shell stops the server instead of exposing it.
    /// An <c>ErrorType.Failure</c> error.
    /// </summary>
    public const string NotLoopback = "LocalWebShell.NotLoopback";

    /// <summary>The server started but reported no address to open. An <c>ErrorType.Unexpected</c> error.</summary>
    public const string NoAddress = "LocalWebShell.NoAddress";

    /// <summary>
    /// The browser could not be opened. Not a failure of the start: the server runs, and the application should print
    /// <see cref="Session.LaunchUrl"/> for the user to open. An <c>ErrorType.Failure</c> error, found in
    /// <see cref="Session.BrowserError"/>.
    /// </summary>
    public const string BrowserNotOpened = "LocalWebShell.BrowserNotOpened";
}
