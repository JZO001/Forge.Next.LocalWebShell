namespace Forge.Next.LocalWebShell.Tests;

/// <summary>The secrets: their form, their uniqueness, the comparison, and that they never show in <c>ToString</c>.</summary>
public class SecretsTests
{
    /// <summary>Both secrets are 43 URL-safe characters (32 bytes) and differ from each other.</summary>
    [Fact]
    public void Constructor_WhenCreated_ShouldMakeTwoDifferentUrlSafeSecrets()
    {
        Secrets secrets = new();

        secrets.LaunchToken.Length.ShouldBe(43);
        secrets.CookieValue.Length.ShouldBe(43);
        secrets.LaunchToken.ShouldNotBe(secrets.CookieValue);
        secrets.LaunchToken.ShouldAllBe(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');
        secrets.CookieValue.ShouldAllBe(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');
    }

    /// <summary>Two instances never share a secret.</summary>
    [Fact]
    public void Constructor_WhenCreatedTwice_ShouldMakeNewSecrets()
    {
        Secrets first = new();
        Secrets second = new();

        first.LaunchToken.ShouldNotBe(second.LaunchToken);
        first.CookieValue.ShouldNotBe(second.CookieValue);
    }

    /// <summary>Only the exact token matches; the cookie value, null and near misses do not.</summary>
    [Fact]
    public void IsLaunchToken_WhenComparing_ShouldMatchOnlyTheToken()
    {
        Secrets secrets = new();

        secrets.IsLaunchToken(secrets.LaunchToken).ShouldBeTrue();
        secrets.IsLaunchToken(secrets.CookieValue).ShouldBeFalse();
        secrets.IsLaunchToken(null).ShouldBeFalse();
        secrets.IsLaunchToken(string.Empty).ShouldBeFalse();
        secrets.IsLaunchToken(secrets.LaunchToken[..^1]).ShouldBeFalse();
        secrets.IsLaunchToken(secrets.LaunchToken.ToUpperInvariant()).ShouldBe(secrets.LaunchToken == secrets.LaunchToken.ToUpperInvariant());
    }

    /// <summary>Only the exact cookie value matches.</summary>
    [Fact]
    public void IsCookieValue_WhenComparing_ShouldMatchOnlyTheCookieValue()
    {
        Secrets secrets = new();

        secrets.IsCookieValue(secrets.CookieValue).ShouldBeTrue();
        secrets.IsCookieValue(secrets.LaunchToken).ShouldBeFalse();
        secrets.IsCookieValue(null).ShouldBeFalse();
    }

    /// <summary><c>ToString</c> shows neither secret.</summary>
    [Fact]
    public void ToString_WhenCalled_ShouldNotShowTheSecrets()
    {
        Secrets secrets = new();

        string text = secrets.ToString();

        text.ShouldNotContain(secrets.LaunchToken);
        text.ShouldNotContain(secrets.CookieValue);
    }
}
