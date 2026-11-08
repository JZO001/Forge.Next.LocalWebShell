// The smallest local web shell: a console application that serves one page on 127.0.0.1 and opens it in the user's
// browser. Run it with `dotnet run`; add `--LocalWebShell:OpenBrowser=false` to only print the link.
using ErrorOr;
using Forge.Next.LocalWebShell;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddLocalWebShell();

await using WebApplication app = builder.Build();
app.MapGet("/", () => Results.Content(
    "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Forge.Next.LocalWebShell sample</title></head>"
    + "<body><h1>Forge.Next.LocalWebShell sample</h1><p>This browser opened the launch link, so it is in.</p></body></html>",
    "text/html"));

ErrorOr<Session> started = await app.StartLocalWebShellAsync();
if (started.IsError)
{
    Console.Error.WriteLine($"The application could not start: {started.FirstError.Description}");
    return 1;
}

Session session = started.Value;
Console.WriteLine("Keep this window open; closing it stops the application.");
Console.WriteLine("Anyone with the link below can use the application on this machine: do not share it.");
Console.WriteLine($"Open: {session.LaunchUrl}");
if (session.Browser == BrowserState.NotOpened)
{
    Console.WriteLine($"The browser was not opened ({session.BrowserError?.Description}); open the link yourself.");
}

await app.WaitForShutdownAsync();
return 0;
