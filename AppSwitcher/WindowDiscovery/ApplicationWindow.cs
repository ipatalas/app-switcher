using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AppSwitcher.WindowDiscovery;

[DebuggerDisplay("{Title} ({ProcessId})")]
internal record ApplicationWindow(HWND Handle,
                                  string Title,
                                  int ProcessId,
                                  string ProcessImageName,
                                  SHOW_WINDOW_CMD State,
                                  Point Position,
                                  Size Size,
                                  WINDOW_STYLE Style,
                                  WindowStyleEx StyleEx)
{
    public bool IsValidWindow =>
        Size != Size.Empty && !string.IsNullOrEmpty(Title) && !StyleEx.HasFlag(WindowStyleEx.WS_EX_NOACTIVATE);

    public unsafe string? GetProductName()
    {
        var verInfoSize = PInvoke.GetFileVersionInfoSize(ProcessImageName, null);
        if (verInfoSize > 0)
        {
            IntPtr verInfo = Marshal.AllocHGlobal((int)verInfoSize);

            try
            {
                if (PInvoke.GetFileVersionInfo(ProcessImageName, verInfoSize, (void*)verInfo))
                {
                    if (PInvoke.VerQueryValue((void*)verInfo, @"\VarFileInfo\Translation", out var pTranslations, out var len))
                    {
                        var ptr = new IntPtr(pTranslations);
                        int transCount = (int)len / 4; // Each translation is 4 bytes (2 for language, 2 for codepage)
                        for (int i = 0; i < transCount; i++)
                        {
                            int offset = i * 4;
                            ushort langId = (ushort)Marshal.ReadInt16(ptr, offset);
                            ushort codePage = (ushort)Marshal.ReadInt16(ptr, offset + 2);

                            string subBlock = @$"\StringFileInfo\{langId:X4}{codePage:X4}\ProductName";
                            if (PInvoke.VerQueryValue((void*)verInfo, subBlock, out void* pValue, out _))
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
            finally
            {
                Marshal.FreeHGlobal(verInfo);
            }
        }

        return null;
    }
}
