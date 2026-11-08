using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Forge.Next.LocalWebShell;

/// <summary>
/// Puts <see cref="Gate"/> in front of the application's whole pipeline. A startup filter runs before the
/// application's own middleware, so static files, the Blazor hub and every endpoint are behind the gate without the
/// application having to remember a <c>Use…</c> call or its position.
/// </summary>
internal sealed class StartupFilter : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.UseMiddleware<Gate>();
        next(app);
    };
}
