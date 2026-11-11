namespace Forge.Next.LocalWebShell.Tests;

/// <summary>The language tags the 401 page's settings accept, and a tag's language alone.</summary>
public class LanguageTagsTests
{
    /// <summary>Subtags of 1–8 ASCII letters or digits joined by a dash, the first of letters only.</summary>
    [Theory]
    [InlineData("en", true)]
    [InlineData("hu-HU", true)]
    [InlineData("pt-BR", true)]
    [InlineData("zh-Hant-TW", true)]
    [InlineData("es-419", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("en_US", false)]
    [InlineData("en-", false)]
    [InlineData("-en", false)]
    [InlineData("1en", false)]
    [InlineData("toolongsubtag", false)]
    [InlineData("hu HU", false)]
    [InlineData("hu\"><script>", false)]
    public void IsWellFormed_WhenGivenATag_ShouldTellWhetherItIsWellFormed(string? tag, bool expected) =>
        LanguageTags.IsWellFormed(tag).ShouldBe(expected);

    /// <summary>The language is the part before the first dash.</summary>
    [Theory]
    [InlineData("hu-HU", "hu")]
    [InlineData("zh-Hant-TW", "zh")]
    [InlineData("en", "en")]
    public void PrimaryLanguage_WhenGivenATag_ShouldReturnItsLanguage(string tag, string expected) =>
        LanguageTags.PrimaryLanguage(tag).ShouldBe(expected);
}
