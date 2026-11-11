using Microsoft.Extensions.Primitives;

namespace Forge.Next.LocalWebShell.Tests;

/// <summary>
/// The 401 page: the texts HTML-encoded and nothing executable, and the language chosen from the browser's
/// <c>Accept-Language</c> among the configured ones.
/// </summary>
public class UnauthorizedPageTests
{
    /// <summary>The texts are encoded, so a text cannot inject markup or script.</summary>
    [Fact]
    public void Render_WhenTheTextsContainMarkup_ShouldEncodeThem()
    {
        UnauthorizedPageText text = new("en", "<b>Title</b>", "<script>alert(1)</script>");

        string html = UnauthorizedPage.Render(text);

        html.ShouldNotContain("<script>");
        html.ShouldNotContain("<b>Title</b>");
        html.ShouldContain("&lt;b&gt;Title&lt;/b&gt;");
    }

    /// <summary>The page shows the title and the message, and names their language.</summary>
    [Fact]
    public void Render_WhenTheTextsArePlain_ShouldShowThemWithTheirLanguage()
    {
        UnauthorizedPageText text = new("hu", "Use the console link", "Look at the window.");

        string html = UnauthorizedPage.Render(text);

        html.ShouldContain("<html lang=\"hu\">");
        html.ShouldContain("<title>Use the console link</title>");
        html.ShouldContain("<p>Look at the window.</p>");
    }

    /// <summary>The page is UTF-8: accented letters stay as they are, quotes in a text are still encoded.</summary>
    [Fact]
    public void Render_WhenTheTextsHaveAccentsAndQuotes_ShouldKeepTheLettersAndEncodeTheQuotes()
    {
        UnauthorizedPageText text = new("hu", "Nyisd meg a konzol linkjét", "A \"link\" a konzolban.");

        string html = UnauthorizedPage.Render(text);

        html.ShouldContain("<title>Nyisd meg a konzol linkjét</title>");
        html.ShouldContain("A &quot;link&quot; a konzolban.");
    }

    /// <summary>Without further languages the default texts come, whatever the browser asks for.</summary>
    [Fact]
    public void Choose_WhenThereAreNoFurtherLanguages_ShouldReturnTheDefaults()
    {
        Options options = new();

        UnauthorizedPageText text = UnauthorizedPage.Choose(options, new StringValues("hu-HU,hu;q=0.9"));

        text.ShouldBe(new UnauthorizedPageText("en", options.UnauthorizedTitle, options.UnauthorizedMessage));
    }

    /// <summary>
    /// The browser's languages are tried in its order (by quality): the same tag, the tag's language alone, then a
    /// configured tag of the same language; nothing fitting means the default.
    /// </summary>
    [Theory]
    [InlineData("hu-HU,hu;q=0.9,en;q=0.8", "hu")]
    [InlineData("hu", "hu")]
    [InlineData("HU-hu", "hu")]
    [InlineData("en-US,hu;q=0.8", "en")]
    [InlineData("de-DE,de;q=0.9", "en")]
    [InlineData("de-DE,hu;q=0.5", "hu")]
    [InlineData("en;q=0.4,hu;q=0.9", "hu")]
    [InlineData("pt", "pt-BR")]
    [InlineData("pt-PT", "pt-BR")]
    [InlineData("hu;q=0,en", "en")]
    [InlineData("*", "en")]
    [InlineData("", "en")]
    [InlineData("@@@", "en")]
    public void Choose_WhenTheBrowserAsksForLanguages_ShouldTakeTheFirstThatFits(string acceptLanguage, string expected)
    {
        Options options = new();
        options.UnauthorizedTexts["hu"] = new UnauthorizedText { Title = "Cím", Message = "Üzenet" };
        options.UnauthorizedTexts["pt-BR"] = new UnauthorizedText { Title = "Título", Message = "Mensagem" };

        UnauthorizedPageText text = UnauthorizedPage.Choose(options, new StringValues(acceptLanguage));

        text.Language.ShouldBe(expected);
    }

    /// <summary>An entry for the default language replaces the default texts for a browser that asks for it.</summary>
    [Fact]
    public void Choose_WhenAnEntryIsForTheDefaultLanguage_ShouldUseTheEntry()
    {
        Options options = new();
        options.UnauthorizedTexts["en"] = new UnauthorizedText { Title = "Own title", Message = "Own message" };

        UnauthorizedPageText text = UnauthorizedPage.Choose(options, new StringValues("en-GB"));

        text.Title.ShouldBe("Own title");
    }

    /// <summary>Only the first preferences are tried, so a forged header cannot make the gate loop over thousands.</summary>
    [Fact]
    public void Choose_WhenTheFittingLanguageComesAfterTheLimit_ShouldReturnTheDefaults()
    {
        Options options = new();
        options.UnauthorizedTexts["hu"] = new UnauthorizedText { Title = "Cím", Message = "Üzenet" };
        string others = string.Join(',', Enumerable.Range(0, UnauthorizedPage.MaxPreferences).Select(index => $"x{(char)('a' + index)}"));

        UnauthorizedPageText text = UnauthorizedPage.Choose(options, new StringValues($"{others},hu"));

        text.Language.ShouldBe("en");
    }
}
