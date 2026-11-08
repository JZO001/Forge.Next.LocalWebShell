using System.Runtime.InteropServices;

namespace Forge.Next.LocalWebShell.Browser.Interop;

/// <summary>
/// <c>IShellView</c> (shobjidl.idl). Only <see cref="GetItemObject"/> is called; the methods before it (two of
/// <c>IOleWindow</c>, then the view's own) keep the vtable order.
/// </summary>
[ComImport]
[Guid("000214E3-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellView
{
    /// <summary><c>IOleWindow.GetWindow</c>; unused.</summary>
    void GetWindow(out IntPtr hwnd);

    /// <summary><c>IOleWindow.ContextSensitiveHelp</c>; unused.</summary>
    void ContextSensitiveHelp([MarshalAs(UnmanagedType.Bool)] bool enterMode);

    /// <summary><c>TranslateAccelerator</c>; unused.</summary>
    [PreserveSig]
    int TranslateAccelerator(IntPtr message);

    /// <summary><c>EnableModeless</c>; unused.</summary>
    void EnableModeless([MarshalAs(UnmanagedType.Bool)] bool enable);

    /// <summary><c>UIActivate</c>; unused.</summary>
    void UIActivate(uint state);

    /// <summary><c>Refresh</c>; unused.</summary>
    void Refresh();

    /// <summary><c>CreateViewWindow</c>; unused.</summary>
    void CreateViewWindow(IntPtr previous, IntPtr folderSettings, IntPtr browser, IntPtr viewRectangle, out IntPtr hwnd);

    /// <summary><c>DestroyViewWindow</c>; unused.</summary>
    void DestroyViewWindow();

    /// <summary><c>GetCurrentInfo</c>; unused.</summary>
    void GetCurrentInfo(IntPtr folderSettings);

    /// <summary><c>AddPropertySheetPages</c>; unused.</summary>
    void AddPropertySheetPages(uint reserved, IntPtr addPage, IntPtr parameter);

    /// <summary><c>SaveViewState</c>; unused.</summary>
    void SaveViewState();

    /// <summary><c>SelectItem</c>; unused.</summary>
    void SelectItem(IntPtr pidl, uint flags);

    /// <summary>An object of the view; with <c>SVGIO_BACKGROUND</c> and <c>IID_IDispatch</c>, its <c>IShellFolderViewDual</c>.</summary>
    /// <param name="item">The object wanted (<c>SVGIO_*</c>).</param>
    /// <param name="interfaceId">The interface wanted.</param>
    /// <param name="result">The object.</param>
    void GetItemObject(uint item, ref Guid interfaceId, [MarshalAs(UnmanagedType.IUnknown)] out object result);
}
