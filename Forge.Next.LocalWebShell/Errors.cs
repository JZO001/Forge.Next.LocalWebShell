using ErrorOr;

namespace Forge.Next.LocalWebShell;

/// <summary>The factories of the shell's errors, so every place builds the same code, type and wording.</summary>
internal static class Errors
{
    /// <summary>The browser was not opened, with the reason.</summary>
    /// <param name="reason">Why, in a few words; it must not contain the launch token.</param>
    public static Error BrowserNotOpened(string reason) =>
        Error.Failure(ErrorCodes.BrowserNotOpened, $"The browser was not opened: {reason}");

    /// <summary>The fixed port is already used.</summary>
    /// <param name="port">The port that was asked for.</param>
    public static Error PortInUse(int port) =>
        Error.Conflict(ErrorCodes.PortInUse, $"Port {port} on the loopback address is already in use. Choose another port, or set LocalWebShell:Port to 0 to let the operating system pick one.");

    /// <summary>The server listens on a non-loopback address.</summary>
    /// <param name="address">The offending address.</param>
    public static Error NotLoopback(string address) =>
        Error.Failure(ErrorCodes.NotLoopback, $"The server also listens on {address}, which is not a loopback address, so it was stopped. Remove the extra Kestrel endpoint (for example Kestrel:Endpoints in the configuration).");

    /// <summary>The server reported no address.</summary>
    public static Error NoAddress() =>
        Error.Unexpected(ErrorCodes.NoAddress, "The server started but reported no address to open.");
}
