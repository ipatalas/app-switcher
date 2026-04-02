using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AppSwitcher.WindowDiscovery;

[DebuggerDisplay("{Title} ({ProcessId})")]
internal record ApplicationWindow(
    HWND Handle,
    string Title,
    uint ProcessId,
    string ProcessImageName,
    SHOW_WINDOW_CMD State,
    Point Position,
    Size Size,
    WINDOW_STYLE Style,
    WindowStyleEx StyleEx,
    bool IsCloaked,
    bool NeedsElevation)
{
    public bool IsValidWindow =>
        Size != Size.Empty && !string.IsNullOrEmpty(Title) &&
        !StyleEx.HasFlag(WindowStyleEx.WS_EX_NOACTIVATE) &&
        !StyleEx.HasFlag(WindowStyleEx.WS_EX_TOOLWINDOW) &&
        !IsCloaked;

    public unsafe string? GetProductName()
    {
        var verInfoSize = PInvoke.GetFileVersionInfoSize(ProcessImageName);
        if (verInfoSize <= 0)
        {
            return null;
        }

        var versionInfo = new byte[verInfoSize];

        if (PInvoke.GetFileVersionInfo(ProcessImageName, versionInfo))
        {
            fixed (byte* pVersion = versionInfo)
            {
                if (PInvoke.VerQueryValue(pVersion, @"\VarFileInfo\Translation", out var pTranslations,
                        out var len))
                {
                    var ptr = new IntPtr(pTranslations);
                    var transCount = (int)len / 4; // Each translation is 4 bytes (2 for language, 2 for codepage)
                    for (var i = 0; i < transCount; i++)
                    {
                        var offset = i * 4;
                        var langId = (ushort)Marshal.ReadInt16(ptr, offset);
                        var codePage = (ushort)Marshal.ReadInt16(ptr, offset + 2);

                        var subBlock = @$"\StringFileInfo\{langId:X4}{codePage:X4}\ProductName";
                        if (PInvoke.VerQueryValue(pVersion, subBlock, out var pValue, out _))
                        {
                            var name = Marshal.PtrToStringUni(new IntPtr(pValue));
                            if (!string.IsNullOrEmpty(name))
                            {
                                return name;
                            }
                        }
                    }
                }
            }
        }

        return null;
    }
}