using System.Runtime.InteropServices;

namespace Forge.Next.LocalWebShell.Browser.Interop;

/// <summary>
/// <c>IShellBrowser</c> (shobjidl.idl). Only <see cref="QueryActiveShellView"/> is called; the methods before it (two of
/// <c>IOleWindow</c>, then the browser's own) keep the vtable order, and the later ones are left out.
/// </summary>
[ComImport]
[Guid("000214E2-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellBrowser
{
    /// <summary><c>IOleWindow.GetWindow</c>; unused.</summary>
    void GetWindow(out IntPtr hwnd);

    /// <summary><c>IOleWindow.ContextSensitiveHelp</c>; unused.</summary>
    void ContextSensitiveHelp([MarshalAs(UnmanagedType.Bool)] bool enterMode);

    /// <summary><c>InsertMenusSB</c>; unused.</summary>
    void InsertMenusSB(IntPtr sharedMenu, IntPtr menuWidths);

    /// <summary><c>SetMenuSB</c>; unused.</summary>
    void SetMenuSB(IntPtr sharedMenu, IntPtr oleMenu, IntPtr activeObjectWindow);

    /// <summary><c>RemoveMenusSB</c>; unused.</summary>
    void RemoveMenusSB(IntPtr sharedMenu);

    /// <summary><c>SetStatusTextSB</c>; unused.</summary>
    void SetStatusTextSB(IntPtr statusText);

    /// <summary><c>EnableModelessSB</c>; unused.</summary>
    void EnableModelessSB([MarshalAs(UnmanagedType.Bool)] bool enable);

    /// <summary><c>TranslateAcceleratorSB</c>; unused.</summary>
    void TranslateAcceleratorSB(IntPtr message, ushort id);

    /// <summary><c>BrowseObject</c>; unused.</summary>
    void BrowseObject(IntPtr pidl, uint flags);

    /// <summary><c>GetViewStateStream</c>; unused.</summary>
    void GetViewStateStream(uint mode, out IntPtr stream);

    /// <summary><c>GetControlWindow</c>; unused.</summary>
    void GetControlWindow(uint id, out IntPtr hwnd);

    /// <summary><c>SendControlMsg</c>; unused.</summary>
    void SendControlMsg(uint id, uint message, IntPtr wParam, IntPtr lParam, out IntPtr result);

    /// <summary>The browser's active view.</summary>
    /// <param name="view">The view.</param>
    void QueryActiveShellView([MarshalAs(UnmanagedType.Interface)] out IShellView view);
}
