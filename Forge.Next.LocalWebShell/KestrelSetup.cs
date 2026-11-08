using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace Forge.Next.LocalWebShell;

/// <summary>
/// Makes Kestrel listen on the IPv4 loopback address only, on the configured port (0: the operating system picks one).
/// </summary>
/// <remarks>
/// An explicit Kestrel endpoint overrides the URLs from <c>ASPNETCORE_URLS</c>, <c>--urls</c> or launch settings, so
/// those cannot widen the binding. Endpoints added in the <c>Kestrel:Endpoints</c> configuration would be added on top;
/// the start catches those (<see cref="ErrorCodes.NotLoopback"/>). Only IPv4 is bound, because a dynamic
/// port cannot be shared by two addresses; the browser is sent to <c>127.0.0.1</c>.
/// </remarks>
internal sealed class KestrelSetup(IOptions<Options> options) : IConfigureOptions<KestrelServerOptions>
{
    /// <inheritdoc />
    public void Configure(KestrelServerOptions kestrel)
    {
        ArgumentNullException.ThrowIfNull(kestrel);
        kestrel.Listen(IPAddress.Loopback, options.Value.Port);
    }
}
