using Forge.Next.LocalWebShell.Tests.TestHelpers;

namespace Forge.Next.LocalWebShell.Tests;

/// <summary>The session: it can be logged without the launch token.</summary>
public class SessionTests
{
    /// <summary><c>ToString</c> has the address and the browser state, never the token.</summary>
    [Fact]
    public async Task ToString_WhenCalled_ShouldLeaveTheTokenOut()
    {
        await using TestShell shell = await TestShell.StartAsync();

        string text = shell.Session.ToString();

        text.ShouldContain(shell.Session.Address.ToString());
        text.ShouldContain(nameof(BrowserState.NotRequested));
        text.ShouldNotContain(shell.Token);
    }
}
