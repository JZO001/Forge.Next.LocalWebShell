using Microsoft.AspNetCore.Http;

namespace Forge.Next.LocalWebShell;

/// <summary>
/// The settings of the local web shell. They are bound from the <c>LocalWebShell</c> configuration section, and the
/// delegate passed to <see cref="WebApplicationBuilderExtensions.AddLocalWebShell"/> runs after the binding,
/// so code wins over configuration. They are validated when the application is built (<c>builder.Build()</c> throws an
/// <c>OptionsValidationException</c> naming every problem) and again when it starts.
/// </summary>
/// <remarks>
/// The address is deliberately not a setting: the shell always listens on the IPv4 loopback address
/// (<c>127.0.0.1</c>) only. A local admin tool reachable from the network is exactly what this package exists to prevent.
/// </remarks>
public sealed class Options
{
    /// <summary>The configuration section the options are bound from: <c>LocalWebShell</c>.</summary>
    public const string SectionName = "LocalWebShell";

    /// <summary>
    /// The loopback port to listen on. <c>0</c>, the default, lets the operating system pick a free port at every start,
    /// so two applications (or two instances of one) never collide and no port has to be asked for. A fixed port
    /// (1–65535) is for those who need a known address; if it is taken, the start returns
    /// <see cref="ErrorCodes.PortInUse"/>.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Whether the start opens the user's default browser at the launch link. <see langword="true"/> by default. Set it
    /// to <see langword="false"/> for tests and headless runs; the launch link is still returned
    /// (<see cref="Session.LaunchUrl"/>) for the application to print. From the command line:
    /// <c>--LocalWebShell:OpenBrowser=false</c>. Even when it is on, the default launcher opens nothing where that would
    /// be unsafe or useless (as root on Linux or macOS, or without a graphical session on Linux), and
    /// <see cref="Session.BrowserError"/> says why.
    /// </summary>
    public bool OpenBrowser { get; set; } = true;

    /// <summary>
    /// The base name of the session cookie the launch token is exchanged for. The shell appends the port
    /// (<c>Forge.LocalWebShell-54321</c>), because browsers do not separate cookies by port: without it, two instances on
    /// one machine would overwrite each other's cookie and lock each other's tab out. <c>Forge.LocalWebShell</c> by
    /// default; it must be a valid cookie name (no spaces, <c>;</c>, <c>,</c> or <c>=</c>).
    /// </summary>
    public string CookieName { get; set; } = "Forge.LocalWebShell";

    /// <summary>
    /// The <c>SameSite</c> attribute of the session cookie. <see cref="SameSiteMode.Strict"/>, the default, keeps the
    /// cookie off every request a foreign web page starts, including a cross-site WebSocket to the Blazor hub (CORS does
    /// not apply to WebSockets). <see cref="SameSiteMode.Lax"/> is allowed for a browser that drops a strict cookie on the
    /// first navigation; it still keeps the cookie off cross-site WebSockets and sub-requests. <see cref="SameSiteMode.None"/>
    /// and <see cref="SameSiteMode.Unspecified"/> are rejected.
    /// </summary>
    public SameSiteMode CookieSameSite { get; set; } = SameSiteMode.Strict;

    /// <summary>
    /// The title of the page a browser gets when it has neither the launch token nor the session cookie (HTTP 401). It is
    /// HTML-encoded. Translate it with the application's own texts if needed.
    /// </summary>
    public string UnauthorizedTitle { get; set; } = "Open the link from the console window";

    /// <summary>The message of the page a browser gets without the launch token or the session cookie. It is HTML-encoded.</summary>
    public string UnauthorizedMessage { get; set; } =
        "This application opens only through the link it printed in its console window when it started. Open that link, or start the application again.";
}
