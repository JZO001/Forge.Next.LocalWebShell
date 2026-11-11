namespace Forge.Next.LocalWebShell;

/// <summary>
/// The 401 page in one language: an entry of <see cref="Options.UnauthorizedTexts"/>. Both texts are HTML-encoded on the
/// page, and neither may be empty (the options' validation names an empty one).
/// </summary>
public sealed class UnauthorizedText
{
    /// <summary>The page's title, shown as the browser tab's title and as the page's heading.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>The page's message: where the right link is.</summary>
    public string Message { get; set; } = string.Empty;
}
