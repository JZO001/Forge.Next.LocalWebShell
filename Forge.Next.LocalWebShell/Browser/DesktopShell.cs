using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ErrorOr;
using Forge.Next.LocalWebShell.Browser.Interop;

namespace Forge.Next.LocalWebShell.Browser;

/// <summary>
/// Starts a program or opens a link through the desktop shell (Explorer), so it runs with the shell's token, the user's
/// normal rights, even when this process is elevated.
/// </summary>
/// <remarks>
/// The documented technique (Raymond Chen, "How can I launch an unelevated process from my elevated process and vice
/// versa?"): find the desktop's shell window (<c>IShellWindows.FindWindowSW</c>), get its browser
/// (<c>IServiceProvider.QueryService(SID_STopLevelBrowser)</c>), its active view, the view's automation object
/// (<c>IShellFolderViewDual</c>), and call <c>Shell.Application.ShellExecute</c> on it. The call runs inside Explorer.
/// It needs Explorer as the shell; without it (Server Core, another shell) it returns an error.
/// The calls run on a short-lived STA thread, with a time limit, because the shell objects expect a single-threaded
/// apartment.
/// </remarks>
[SupportedOSPlatform("windows")]
internal static class DesktopShell
{
    /// <summary><c>SWC_DESKTOP</c>: look for the desktop window.</summary>
    private const int SwcDesktop = 8;

    /// <summary><c>SWFO_NEEDDISPATCH</c>: return the window's automation object.</summary>
    private const int SwfoNeedDispatch = 1;

    /// <summary><c>SVGIO_BACKGROUND</c>: the view's background object, which is the <c>IShellFolderViewDual</c>.</summary>
    private const uint SvgioBackground = 0;

    /// <summary><c>CSIDL_DESKTOP</c>, the location of the window looked for.</summary>
    private const int CsidlDesktop = 0;

    /// <summary>How long the shell may take to answer.</summary>
    private static readonly TimeSpan AnswerTimeout = TimeSpan.FromSeconds(10);

    /// <summary><c>CLSID_ShellWindows</c>.</summary>
    private static readonly Guid ShellWindowsClassId = new("9BA05972-F6A8-11CF-A442-00A0C90A8F39");

    /// <summary><c>SID_STopLevelBrowser</c>.</summary>
    private static readonly Guid TopLevelBrowserServiceId = new("4C96BE40-915C-11CF-99D3-00AA004AE837");

    /// <summary><c>IID_IShellBrowser</c>.</summary>
    private static readonly Guid ShellBrowserInterfaceId = new("000214E2-0000-0000-C000-000000000046");

    /// <summary><c>IID_IDispatch</c>.</summary>
    private static readonly Guid DispatchInterfaceId = new("00020400-0000-0000-C000-000000000046");

    /// <summary>
    /// Asks the desktop shell to open <paramref name="file"/> with <paramref name="arguments"/>; for a link, the user's
    /// default browser opens it.
    /// </summary>
    /// <param name="file">A program, document or URL.</param>
    /// <param name="arguments">The program's arguments; empty for a document or URL.</param>
    /// <param name="show">The <c>SW_*</c> show command (1: normal, 0: hidden).</param>
    /// <returns>Success when the shell accepted the request, or <see cref="ErrorCodes.BrowserNotOpened"/>.</returns>
    public static ErrorOr<Success> ShellExecuteUnelevated(string file, string arguments, int show)
    {
        ErrorOr<Success> result = Errors.BrowserNotOpened("the desktop shell did not answer in time.");

        Thread thread = new(() => result = Execute(file, arguments, show))
        {
            IsBackground = true,
            Name = "Forge.Next.LocalWebShell desktop shell",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!thread.Join(AnswerTimeout))
        {
            return Errors.BrowserNotOpened("the desktop shell did not answer in time.");
        }

        return result;
    }

    /// <summary>The COM calls themselves, on the STA thread.</summary>
    private static ErrorOr<Success> Execute(string file, string arguments, int show)
    {
        List<object> comObjects = [];
        try
        {
            Type? shellWindowsType = Type.GetTypeFromCLSID(ShellWindowsClassId, throwOnError: false);
            if (shellWindowsType is null)
            {
                return Errors.BrowserNotOpened("the shell windows object is not registered.");
            }

            object shellWindowsObject = Track(comObjects, Activator.CreateInstance(shellWindowsType))
                ?? throw new COMException("The shell windows object could not be created.");
            IShellWindows shellWindows = (IShellWindows)shellWindowsObject;

            object location = CsidlDesktop;
            object? root = null;
            object? desktop = Track(comObjects, shellWindows.FindWindowSW(ref location, ref root, SwcDesktop, out int _, SwfoNeedDispatch));
            if (desktop is null)
            {
                return Errors.BrowserNotOpened("there is no desktop shell window (is Explorer the shell?).");
            }

            Guid serviceId = TopLevelBrowserServiceId;
            Guid browserInterfaceId = ShellBrowserInterfaceId;
            ((IComServiceProvider)desktop).QueryService(ref serviceId, ref browserInterfaceId, out object browserObject);
            Track(comObjects, browserObject);

            ((IShellBrowser)browserObject).QueryActiveShellView(out IShellView view);
            Track(comObjects, view);

            Guid dispatchInterfaceId = DispatchInterfaceId;
            view.GetItemObject(SvgioBackground, ref dispatchInterfaceId, out object folderView);
            Track(comObjects, folderView);

            // IShellFolderViewDual.Application is the Shell.Application object of the Explorer process.
            dynamic folderViewDual = folderView;
            object application = Track(comObjects, (object)folderViewDual.Application)
                ?? throw new COMException("The shell view has no application object.");

            dynamic shellApplication = application;
            shellApplication.ShellExecute(file, arguments, string.Empty, "open", show);
            return Result.Success;
        }
        catch (Exception exception)
        {
            // A boundary: COM, casting and binder failures all mean the same thing to the caller.
            return Errors.BrowserNotOpened($"the desktop shell refused: {exception.Message}");
        }
        finally
        {
            foreach (object comObject in comObjects)
            {
                if (Marshal.IsComObject(comObject))
                {
                    Marshal.FinalReleaseComObject(comObject);
                }
            }
        }
    }

    /// <summary>Remembers a COM object for release, and returns it.</summary>
    private static T? Track<T>(List<object> comObjects, T? value) where T : class
    {
        if (value is not null)
        {
            comObjects.Add(value);
        }

        return value;
    }
}
