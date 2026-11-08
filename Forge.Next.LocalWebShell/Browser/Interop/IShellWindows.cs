using System.Runtime.InteropServices;

namespace Forge.Next.LocalWebShell.Browser.Interop;

/// <summary>
/// <c>IShellWindows</c> (exdisp.idl), the collection of the shell's open windows. Only <see cref="FindWindowSW"/> is
/// called; the methods before it are declared to keep the vtable order.
/// </summary>
[ComImport]
[Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85")]
[InterfaceType(ComInterfaceType.InterfaceIsDual)]
internal interface IShellWindows
{
    /// <summary><c>Count</c>; unused, keeps the vtable order.</summary>
    int Count { get; }

    /// <summary><c>Item</c>; unused, keeps the vtable order.</summary>
    [return: MarshalAs(UnmanagedType.IDispatch)]
    object Item(object index);

    /// <summary><c>_NewEnum</c>; unused, keeps the vtable order.</summary>
    [return: MarshalAs(UnmanagedType.IUnknown)]
    object NewEnum();

    /// <summary><c>Register</c>; unused, keeps the vtable order.</summary>
    int Register([MarshalAs(UnmanagedType.IDispatch)] object window, int hwnd, int windowClass);

    /// <summary><c>RegisterPending</c>; unused, keeps the vtable order.</summary>
    int RegisterPending(int threadId, ref object location, ref object locationRoot, int windowClass);

    /// <summary><c>Revoke</c>; unused, keeps the vtable order.</summary>
    void Revoke(int cookie);

    /// <summary><c>OnNavigate</c>; unused, keeps the vtable order.</summary>
    void OnNavigate(int cookie, ref object location);

    /// <summary><c>OnActivated</c>; unused, keeps the vtable order.</summary>
    void OnActivated(int cookie, [MarshalAs(UnmanagedType.VariantBool)] bool active);

    /// <summary>Finds a shell window by location and class, and returns its automation object (or <see langword="null"/>).</summary>
    /// <param name="location">The location (<c>CSIDL_DESKTOP</c> for the desktop).</param>
    /// <param name="locationRoot">The root; empty.</param>
    /// <param name="windowClass"><c>SWC_DESKTOP</c>.</param>
    /// <param name="hwnd">The window handle.</param>
    /// <param name="options"><c>SWFO_NEEDDISPATCH</c>.</param>
    /// <returns>The window's automation object, or <see langword="null"/> when there is no such window.</returns>
    [return: MarshalAs(UnmanagedType.IDispatch)]
    object? FindWindowSW(ref object location, ref object? locationRoot, int windowClass, out int hwnd, int options);
}
