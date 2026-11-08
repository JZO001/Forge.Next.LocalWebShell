namespace Forge.Next.LocalWebShell.Tests;

/// <summary>The loopback rules for the Host header and for the server's addresses.</summary>
public class LoopbackHostsTests
{
    /// <summary>Loopback names pass, in any letter case.</summary>
    [Theory]
    [InlineData("localhost")]
    [InlineData("LocalHost")]
    [InlineData("127.0.0.1")]
    [InlineData("[::1]")]
    [InlineData("::1")]
    public void IsAllowedHost_WhenTheHostIsALoopbackName_ShouldReturnTrue(string host)
    {
        LoopbackHosts.IsAllowedHost(host).ShouldBeTrue();
    }

    /// <summary>Anything else is refused, including names that merely contain "localhost".</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("evil.example")]
    [InlineData("localhost.evil.example")]
    [InlineData("127.0.0.2")]
    [InlineData("0.0.0.0")]
    [InlineData("192.168.1.10")]
    public void IsAllowedHost_WhenTheHostIsNotALoopbackName_ShouldReturnFalse(string? host)
    {
        LoopbackHosts.IsAllowedHost(host).ShouldBeFalse();
    }

    /// <summary>Loopback server addresses are recognised, IPv4 (the whole 127/8), IPv6 and localhost.</summary>
    [Theory]
    [InlineData("http://127.0.0.1:5000")]
    [InlineData("http://127.0.0.2:5000")]
    [InlineData("http://[::1]:5000")]
    [InlineData("http://localhost:5000")]
    public void IsLoopbackAddress_WhenTheAddressIsLoopback_ShouldReturnTrue(string address)
    {
        LoopbackHosts.IsLoopbackAddress(address).ShouldBeTrue();
    }

    /// <summary>Wildcards, network addresses and unreadable values are not loopback.</summary>
    [Theory]
    [InlineData("http://0.0.0.0:5000")]
    [InlineData("http://[::]:5000")]
    [InlineData("http://+:5000")]
    [InlineData("http://*:5000")]
    [InlineData("http://192.168.1.10:5000")]
    [InlineData("http://example.com:5000")]
    [InlineData("not an address")]
    [InlineData(null)]
    public void IsLoopbackAddress_WhenTheAddressIsNotLoopback_ShouldReturnFalse(string? address)
    {
        LoopbackHosts.IsLoopbackAddress(address).ShouldBeFalse();
    }
}
