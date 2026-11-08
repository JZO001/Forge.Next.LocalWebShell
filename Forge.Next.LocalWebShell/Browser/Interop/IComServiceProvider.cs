using System.Runtime.InteropServices;

namespace Forge.Next.LocalWebShell.Browser.Interop;

/// <summary>The COM <c>IServiceProvider</c> (servprov.h); named apart from <see cref="System.IServiceProvider"/>.</summary>
[ComImport]
[Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IComServiceProvider
{
    /// <summary>Asks for a service by its identifier and interface.</summary>
    /// <param name="service">The service identifier.</param>
    /// <param name="interfaceId">The interface wanted.</param>
    /// <param name="result">The service object.</param>
    void QueryService(ref Guid service, ref Guid interfaceId, [MarshalAs(UnmanagedType.IUnknown)] out object result);
}
