using System.Globalization;
using ErrorOr;

namespace Forge.Next.LocalWebShell;

/// <summary>
/// A started local web shell: where it listens, the link that admits the browser, and whether the browser was opened.
/// </summary>
/// <remarks>
/// <see cref="LaunchUrl"/> carries the launch token, a secret: show it to the user (the console window), never log it.
/// <see cref="ToString"/> leaves it out on purpose.
/// </remarks>
public sealed class Session
{
    /// <summary>Creates the session; the shell does this when it starts.</summary>
    /// <param name="address">The loopback address the server listens on.</param>
    /// <param name="launchUrl">The address with the launch token.</param>
    /// <param name="browser">What happened to the browser.</param>
    /// <param name="browserError">Why the browser was not opened, or <see langword="null"/>.</param>
    internal Session(Uri address, Uri launchUrl, BrowserState browser, Error? browserError)
    {
        Address = address;
        LaunchUrl = launchUrl;
        Browser = browser;
        BrowserError = browserError;
    }

    /// <summary>
    /// The address the server listens on, for example <c>http://127.0.0.1:54321/</c>. Opening it without the session
    /// cookie gives the "open the link from the console" page: use <see cref="LaunchUrl"/> to admit a browser.
    /// </summary>
    public Uri Address { get; }

    /// <summary>
    /// The address with the launch token, for example <c>http://127.0.0.1:54321/?token=…</c>. The first request with it
    /// gets the session cookie and is redirected to the same page without the token. It works for the lifetime of the
    /// process, so the user can reopen a closed tab with it; a new start makes a new token. A secret: print it for the
    /// user, never log it or write it to a file.
    /// </summary>
    public Uri LaunchUrl { get; }

    /// <summary>What happened to the browser.</summary>
    public BrowserState Browser { get; }

    /// <summary>
    /// Why the browser was not opened (code <see cref="ErrorCodes.BrowserNotOpened"/>), or
    /// <see langword="null"/> when it was opened or not requested.
    /// </summary>
    public Error? BrowserError { get; }

    /// <summary>The address and the browser state, without the launch token, so the session can be logged safely.</summary>
    /// <returns>A description without secrets.</returns>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"Session {{ Address = {Address}, Browser = {Browser} }}");
}
