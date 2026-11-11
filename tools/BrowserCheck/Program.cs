// The real-browser check of Forge.Next.LocalWebShell (not part of the solution, so building the package does not pull
// Playwright). It starts the published or built sample without a browser, reads the launch link from its output, and
// opens it in headless Chromium:
// - the first navigation must end on the page (the Strict cookie survived the redirect);
// - the address bar must not hold the token;
// - a reload stays in;
// - a fresh context without the link gets the 401 page, in English;
// - a fresh context whose browser prefers Hungarian gets the sample's Hungarian 401 page.
// Usage (from the repository root):
//   dotnet build samples/Forge.Next.LocalWebShell.Sample -c Release
//   dotnet build tools/BrowserCheck
//   pwsh tools/BrowserCheck/bin/Debug/net10.0/playwright.ps1 install chromium   (once per machine)
//   dotnet tools/BrowserCheck/bin/Debug/net10.0/BrowserCheck.dll samples/Forge.Next.LocalWebShell.Sample/bin/Release/net10.0/Forge.Next.LocalWebShell.Sample.dll
using System.Diagnostics;
using Microsoft.Playwright;

string sample = args[0];
using Process app = Process.Start(new ProcessStartInfo("dotnet", $"\"{sample}\" --LocalWebShell:OpenBrowser=false")
{
    RedirectStandardOutput = true,
    UseShellExecute = false,
})!;
try
{
    string? launch = null;
    using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(30));
    while (launch is null && !deadline.IsCancellationRequested)
    {
        string? line = await app.StandardOutput.ReadLineAsync(deadline.Token);
        if (line is null) break;
        if (line.StartsWith("Open: ", StringComparison.Ordinal)) launch = line["Open: ".Length..];
    }
    if (launch is null) { Console.WriteLine("FAIL no launch link"); return 1; }

    using IPlaywright playwright = await Playwright.CreateAsync();
    await using IBrowser browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
    IBrowserContext context = await browser.NewContextAsync();
    IPage page = await context.NewPageAsync();
    IResponse? response = await page.GotoAsync(launch);
    string body = await page.InnerTextAsync("body");
    bool admitted = body.Contains("This browser opened the launch link", StringComparison.Ordinal);
    bool tokenGone = !page.Url.Contains("token=", StringComparison.Ordinal);
    Console.WriteLine($"admitted={admitted} status={response?.Status} url-has-token={!tokenGone}");

    await page.ReloadAsync();
    bool stillIn = (await page.InnerTextAsync("body")).Contains("This browser opened", StringComparison.Ordinal);
    Console.WriteLine($"after-reload-admitted={stillIn}");

    IBrowserContext stranger = await browser.NewContextAsync();
    IPage other = await stranger.NewPageAsync();
    Uri address = new(new Uri(launch).GetLeftPart(UriPartial.Path));
    IResponse? refused = await other.GotoAsync(address.ToString());
    string strangerLanguage = await other.GetAttributeAsync("html", "lang") ?? string.Empty;
    Console.WriteLine($"stranger-status={refused?.Status} lang={strangerLanguage}");

    // A stranger whose browser prefers Hungarian gets the sample's Hungarian 401 page.
    IBrowserContext hungarian = await browser.NewContextAsync(new() { Locale = "hu-HU" });
    IPage hungarianPage = await hungarian.NewPageAsync();
    IResponse? refusedHungarian = await hungarianPage.GotoAsync(address.ToString());
    string hungarianLanguage = await hungarianPage.GetAttributeAsync("html", "lang") ?? string.Empty;
    Console.WriteLine($"stranger-hu-status={refusedHungarian?.Status} lang={hungarianLanguage} title={await hungarianPage.TitleAsync()}");

    bool ok = admitted && tokenGone && stillIn && refused?.Status == 401 && strangerLanguage == "en"
        && refusedHungarian?.Status == 401 && hungarianLanguage == "hu";
    Console.WriteLine(ok ? "PASS" : "FAIL");
    return ok ? 0 : 1;
}
finally
{
    if (!app.HasExited) app.Kill(entireProcessTree: true);
}
