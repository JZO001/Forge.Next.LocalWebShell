namespace Forge.Next.LocalWebShell.Tests;

/// <summary>The 401 page: the configured texts, HTML-encoded, and nothing executable.</summary>
public class UnauthorizedPageTests
{
    /// <summary>The texts are encoded, so a text cannot inject markup or script.</summary>
    [Fact]
    public void Render_WhenTheTextsContainMarkup_ShouldEncodeThem()
    {
        Options options = new() { UnauthorizedTitle = "<b>Title</b>", UnauthorizedMessage = "<script>alert(1)</script>" };

        string html = UnauthorizedPage.Render(options);

        html.ShouldNotContain("<script>");
        html.ShouldNotContain("<b>Title</b>");
        html.ShouldContain("&lt;b&gt;Title&lt;/b&gt;");
    }

    /// <summary>The page shows the configured title and message.</summary>
    [Fact]
    public void Render_WhenTheTextsArePlain_ShouldShowThem()
    {
        Options options = new() { UnauthorizedTitle = "Use the console link", UnauthorizedMessage = "Look at the window." };

        string html = UnauthorizedPage.Render(options);

        html.ShouldContain("<title>Use the console link</title>");
        html.ShouldContain("<p>Look at the window.</p>");
    }
}
