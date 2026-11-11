using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Forge.Next.LocalWebShell.Tests;

/// <summary>The settings check: the defaults pass, and each wrong value is named.</summary>
public class OptionsValidationTests
{
    /// <summary>The default settings are valid.</summary>
    [Fact]
    public void Validate_WhenTheSettingsAreTheDefaults_ShouldSucceed()
    {
        ValidateOptionsResult result = new OptionsValidation().Validate(null, new Options());

        result.Succeeded.ShouldBeTrue();
    }

    /// <summary>The port range is 0–65535.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(65536)]
    public void Validate_WhenThePortIsOutOfRange_ShouldFail(int port)
    {
        ValidateOptionsResult result = new OptionsValidation().Validate(null, new Options { Port = port });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("LocalWebShell:Port");
    }

    /// <summary>A cookie name must be a valid token.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("two words")]
    [InlineData("a;b")]
    [InlineData("a=b")]
    [InlineData("a,b")]
    public void Validate_WhenTheCookieNameIsInvalid_ShouldFail(string name)
    {
        ValidateOptionsResult result = new OptionsValidation().Validate(null, new Options { CookieName = name });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("LocalWebShell:CookieName");
    }

    /// <summary>Only Strict and Lax are allowed for SameSite.</summary>
    [Theory]
    [InlineData(SameSiteMode.None)]
    [InlineData(SameSiteMode.Unspecified)]
    public void Validate_WhenSameSiteIsNeitherStrictNorLax_ShouldFail(SameSiteMode mode)
    {
        ValidateOptionsResult result = new OptionsValidation().Validate(null, new Options { CookieSameSite = mode });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("LocalWebShell:CookieSameSite");
    }

    /// <summary>The page texts must not be empty.</summary>
    [Fact]
    public void Validate_WhenThePageTextsAreEmpty_ShouldFailForBoth()
    {
        ValidateOptionsResult result = new OptionsValidation().Validate(null, new Options { UnauthorizedTitle = " ", UnauthorizedMessage = string.Empty });

        result.Failures!.Count().ShouldBe(2);
    }

    /// <summary>The default texts' language must be a language tag, as the page writes it into an attribute and a header.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("en_US")]
    [InlineData("en\"x")]
    public void Validate_WhenTheUnauthorizedLanguageIsNotATag_ShouldFail(string language)
    {
        ValidateOptionsResult result = new OptionsValidation().Validate(null, new Options { UnauthorizedLanguage = language });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("LocalWebShell:UnauthorizedLanguage");
    }

    /// <summary>Well-formed further languages with both texts pass.</summary>
    [Fact]
    public void Validate_WhenTheFurtherLanguagesAreRight_ShouldSucceed()
    {
        Options options = new();
        options.UnauthorizedTexts["hu"] = new UnauthorizedText { Title = "Cím", Message = "Üzenet" };
        options.UnauthorizedTexts["zh-Hant-TW"] = new UnauthorizedText { Title = "標題", Message = "訊息" };

        new OptionsValidation().Validate(null, options).Succeeded.ShouldBeTrue();
    }

    /// <summary>A further language is named by its key when the key is no tag or a text is missing.</summary>
    [Fact]
    public void Validate_WhenFurtherLanguagesAreWrong_ShouldNameEachProblem()
    {
        Options options = new();
        options.UnauthorizedTexts["hu HU"] = new UnauthorizedText { Title = "Cím", Message = "Üzenet" };
        options.UnauthorizedTexts["de"] = new UnauthorizedText { Title = " ", Message = string.Empty };

        ValidateOptionsResult result = new OptionsValidation().Validate(null, options);

        result.Failures!.ShouldContain(failure => failure.StartsWith("LocalWebShell:UnauthorizedTexts:hu HU:", StringComparison.Ordinal));
        result.Failures!.ShouldContain("LocalWebShell:UnauthorizedTexts:de:Title must not be empty.");
        result.Failures!.ShouldContain("LocalWebShell:UnauthorizedTexts:de:Message must not be empty.");
    }
}
