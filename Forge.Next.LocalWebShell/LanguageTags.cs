namespace Forge.Next.LocalWebShell;

/// <summary>
/// The little the shell needs of language tags (<c>hu</c>, <c>hu-HU</c>, <c>zh-Hant-TW</c>): whether a configured tag is
/// well formed, and a tag's language alone. No culture lookup: a tag from a request is only compared with the configured
/// ones, never handed to <c>CultureInfo</c>.
/// </summary>
internal static class LanguageTags
{
    /// <summary>The longest subtag a language tag may have.</summary>
    private const int MaxSubtagLength = 8;

    /// <summary>
    /// Whether <paramref name="tag"/> is a well-formed language tag: subtags of 1–8 ASCII letters or digits joined by
    /// <c>-</c>, the first of letters only. It keeps a configured key fit for an HTML attribute and an HTTP header.
    /// </summary>
    /// <param name="tag">The configured tag; null or empty is not well formed.</param>
    /// <returns><see langword="true"/> when the tag is well formed.</returns>
    public static bool IsWellFormed(string? tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return false;
        }

        string[] subtags = tag.Split('-');
        for (int index = 0; index < subtags.Length; index++)
        {
            string subtag = subtags[index];
            if (subtag.Length is 0 or > MaxSubtagLength)
            {
                return false;
            }

            foreach (char character in subtag)
            {
                bool fits = index == 0 ? char.IsAsciiLetter(character) : char.IsAsciiLetterOrDigit(character);
                if (!fits)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>The tag's language alone: <c>hu-HU</c> → <c>hu</c>; a tag without a region stays as it is.</summary>
    /// <param name="tag">A language tag.</param>
    /// <returns>The part before the first <c>-</c>.</returns>
    public static string PrimaryLanguage(string tag)
    {
        int dash = tag.IndexOf('-', StringComparison.Ordinal);
        return dash < 0 ? tag : tag[..dash];
    }
}
