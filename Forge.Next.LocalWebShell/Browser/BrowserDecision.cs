namespace Forge.Next.LocalWebShell.Browser;

/// <summary>The launcher's decision: the route, and for <see cref="BrowserRoute.None"/> the reason.</summary>
/// <param name="Route">How the link is opened.</param>
/// <param name="Reason">Why it is not opened; <see langword="null"/> unless <paramref name="Route"/> is <see cref="BrowserRoute.None"/>.</param>
internal sealed record BrowserDecision(BrowserRoute Route, string? Reason)
{
    /// <summary>A decision not to open, with the reason.</summary>
    /// <param name="reason">Why, in a few words, for the user.</param>
    /// <returns>The decision.</returns>
    public static BrowserDecision NotOpened(string reason) => new(BrowserRoute.None, reason);
}
