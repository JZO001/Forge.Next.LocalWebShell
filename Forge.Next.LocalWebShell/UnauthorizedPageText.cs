namespace Forge.Next.LocalWebShell;

/// <summary>The texts the 401 page shows to one request, and their language.</summary>
/// <param name="Language">The language tag of the texts, as configured; it goes into <c>lang</c> and <c>Content-Language</c>.</param>
/// <param name="Title">The page's title.</param>
/// <param name="Message">The page's message.</param>
internal sealed record UnauthorizedPageText(string Language, string Title, string Message);
