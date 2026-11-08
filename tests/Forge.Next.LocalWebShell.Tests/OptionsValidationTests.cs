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
}
