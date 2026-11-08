namespace Forge.Next.LocalWebShell.Browser;

/// <summary>The operating system families the browser launcher distinguishes.</summary>
internal enum BrowserPlatform
{
    /// <summary>Windows: the shell, or the desktop shell when elevated.</summary>
    Windows,

    /// <summary>Linux and FreeBSD: <c>xdg-open</c>, in a graphical session only.</summary>
    Unix,

    /// <summary>macOS: <c>open</c>.</summary>
    MacOS,

    /// <summary>Anything else: no browser is opened.</summary>
    Other,
}
