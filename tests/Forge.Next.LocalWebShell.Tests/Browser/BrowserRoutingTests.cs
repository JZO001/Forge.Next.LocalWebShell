using Forge.Next.LocalWebShell.Browser;

namespace Forge.Next.LocalWebShell.Tests.Browser;

/// <summary>
/// The browser decision for every operating system, privilege and session: never a privileged browser, never a
/// browser in the terminal. The decision is a pure function, so these run on any operating system.
/// </summary>
public class BrowserRoutingTests
{
    /// <summary>Windows without elevation: the shell opens the link.</summary>
    [Fact]
    public void Choose_WhenWindowsAndNotElevated_ShouldUseTheShell()
    {
        BrowserDecision decision = BrowserRouting.Choose(BrowserPlatform.Windows, privileged: false, NoVariables);

        decision.Route.ShouldBe(BrowserRoute.ShellOpen);
        decision.Reason.ShouldBeNull();
    }

    /// <summary>Windows elevated: only the desktop shell, which runs with the user's normal rights.</summary>
    [Fact]
    public void Choose_WhenWindowsAndElevated_ShouldUseTheDesktopShell()
    {
        BrowserDecision decision = BrowserRouting.Choose(BrowserPlatform.Windows, privileged: true, NoVariables);

        decision.Route.ShouldBe(BrowserRoute.DesktopShell);
    }

    /// <summary>Unix as root: no browser, even in a graphical session.</summary>
    [Fact]
    public void Choose_WhenUnixAndRoot_ShouldNotOpenAndSayWhy()
    {
        BrowserDecision decision = BrowserRouting.Choose(BrowserPlatform.Unix, privileged: true, Variables(("DISPLAY", ":0")));

        decision.Route.ShouldBe(BrowserRoute.None);
        decision.Reason!.ShouldContain("root");
    }

    /// <summary>Unix without a graphical session: no browser, with the SSH tunnel as the way out.</summary>
    [Fact]
    public void Choose_WhenUnixWithoutAGraphicalSession_ShouldNotOpenAndSayWhy()
    {
        BrowserDecision decision = BrowserRouting.Choose(BrowserPlatform.Unix, privileged: false, NoVariables);

        decision.Route.ShouldBe(BrowserRoute.None);
        decision.Reason!.ShouldContain("DISPLAY");
        decision.Reason!.ShouldContain("ssh -L");
    }

    /// <summary>Empty display variables count as missing.</summary>
    [Fact]
    public void Choose_WhenUnixDisplayVariablesAreEmpty_ShouldNotOpen()
    {
        BrowserDecision decision = BrowserRouting.Choose(BrowserPlatform.Unix, privileged: false, Variables(("DISPLAY", string.Empty), ("WAYLAND_DISPLAY", string.Empty)));

        decision.Route.ShouldBe(BrowserRoute.None);
    }

    /// <summary>Unix in an X11 or a Wayland session, not root: <c>xdg-open</c>.</summary>
    [Theory]
    [InlineData("DISPLAY", ":0")]
    [InlineData("WAYLAND_DISPLAY", "wayland-0")]
    public void Choose_WhenUnixInAGraphicalSession_ShouldUseXdgOpen(string variable, string value)
    {
        BrowserDecision decision = BrowserRouting.Choose(BrowserPlatform.Unix, privileged: false, Variables((variable, value)));

        decision.Route.ShouldBe(BrowserRoute.XdgOpen);
        decision.Reason.ShouldBeNull();
    }

    /// <summary>macOS, not root: <c>open</c>.</summary>
    [Fact]
    public void Choose_WhenMacOSAndNotRoot_ShouldUseOpen()
    {
        BrowserDecision decision = BrowserRouting.Choose(BrowserPlatform.MacOS, privileged: false, NoVariables);

        decision.Route.ShouldBe(BrowserRoute.MacOpen);
    }

    /// <summary>macOS as root: no browser.</summary>
    [Fact]
    public void Choose_WhenMacOSAndRoot_ShouldNotOpen()
    {
        BrowserDecision decision = BrowserRouting.Choose(BrowserPlatform.MacOS, privileged: true, NoVariables);

        decision.Route.ShouldBe(BrowserRoute.None);
        decision.Reason!.ShouldContain("root");
    }

    /// <summary>Another operating system: no browser.</summary>
    [Fact]
    public void Choose_WhenTheOperatingSystemIsUnknown_ShouldNotOpen()
    {
        BrowserDecision decision = BrowserRouting.Choose(BrowserPlatform.Other, privileged: false, NoVariables);

        decision.Route.ShouldBe(BrowserRoute.None);
        decision.Reason!.ShouldContain("not supported");
    }

    /// <summary>The current platform is recognised on the machine the tests run on.</summary>
    [Fact]
    public void CurrentPlatform_WhenCalled_ShouldMatchTheOperatingSystem()
    {
        BrowserPlatform platform = BrowserRouting.CurrentPlatform();

        if (OperatingSystem.IsWindows())
        {
            platform.ShouldBe(BrowserPlatform.Windows);
        }
        else if (OperatingSystem.IsLinux())
        {
            platform.ShouldBe(BrowserPlatform.Unix);
        }
        else if (OperatingSystem.IsMacOS())
        {
            platform.ShouldBe(BrowserPlatform.MacOS);
        }
    }

    /// <summary>An environment without any variable.</summary>
    private static string? NoVariables(string name) => null;

    /// <summary>An environment with the given variables.</summary>
    private static Func<string, string?> Variables(params (string Name, string Value)[] variables) =>
        name => variables.FirstOrDefault(variable => variable.Name == name).Value;
}
