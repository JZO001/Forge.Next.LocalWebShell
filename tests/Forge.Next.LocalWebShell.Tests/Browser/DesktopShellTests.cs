using ErrorOr;
using Forge.Next.LocalWebShell.Browser;
using Xunit.Abstractions;

namespace Forge.Next.LocalWebShell.Tests.Browser;

/// <summary>
/// The unelevated start through the desktop shell, checked for real: from an elevated test run, a hidden
/// <c>cmd.exe</c> started this way writes its own groups, and its integrity level must be Medium (unelevated), not High.
/// </summary>
/// <remarks>
/// It proves something only when the test process itself is elevated, on Windows, with Explorer as the shell (a
/// developer machine with an elevated terminal). Elsewhere it returns without asserting, and says so in its output.
/// </remarks>
public class DesktopShellTests(ITestOutputHelper output)
{
    /// <summary>The SID of the Medium mandatory level: an unelevated process.</summary>
    private const string MediumIntegrity = "S-1-16-8192";

    /// <summary>The SID of the High mandatory level: an elevated process.</summary>
    private const string HighIntegrity = "S-1-16-12288";

    /// <summary>A program started through the desktop shell from an elevated process runs unelevated.</summary>
    [Fact]
    public async Task ShellExecuteUnelevated_WhenThisProcessIsElevated_ShouldStartTheProgramUnelevated()
    {
        if (!OperatingSystem.IsWindows() || !Environment.IsPrivilegedProcess)
        {
            output.WriteLine("Not verified: the test process is not elevated on Windows.");
            return;
        }

        string groupsFile = Path.Combine(Path.GetTempPath(), $"forge-lws-{Guid.NewGuid():N}.txt");
        try
        {
            ErrorOr<Success> started = DesktopShell.ShellExecuteUnelevated("cmd.exe", $"/c whoami /groups > \"{groupsFile}\"", 0);
            started.IsError.ShouldBeFalse(started.IsError ? started.FirstError.Description : string.Empty);

            string groups = await ReadWhenWrittenAsync(groupsFile, TimeSpan.FromSeconds(20));

            groups.ShouldContain(MediumIntegrity);
            groups.ShouldNotContain(HighIntegrity);
            output.WriteLine("Verified: the program ran at the Medium integrity level.");
        }
        finally
        {
            File.Delete(groupsFile);
        }
    }

    /// <summary>Waits until the file exists and holds a mandatory level line; fails after the limit.</summary>
    private static async Task<string> ReadWhenWrittenAsync(string path, TimeSpan limit)
    {
        DateTime deadline = DateTime.UtcNow + limit;
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(path))
            {
                try
                {
                    string text = await File.ReadAllTextAsync(path);
                    if (text.Contains("S-1-16-", StringComparison.Ordinal))
                    {
                        return text;
                    }
                }
                catch (IOException)
                {
                    // cmd.exe still writes it.
                }
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"The program started through the desktop shell did not write {path} within {limit.TotalSeconds} s.");
    }
}
