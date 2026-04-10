using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Threading;

namespace AppSwitcher.WindowDiscovery;

internal class ProcessInspector(ILogger<ProcessInspector> logger)
{
    private const int ERROR_ACCESS_DENIED = 5;
    private const int SCS_32BIT_BINARY = 0;
    private const int SCS_64BIT_BINARY = 6;

    public bool NeedsElevation(uint processId)
    {
        using var process = PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION, false, processId);
        if (process.IsInvalid)
        {
            var lastError = Marshal.GetLastWin32Error();
            if (lastError == ERROR_ACCESS_DENIED)
            {
                return true;
            }

            logger.LogWarning("Failed to open process (PROCESS_QUERY_INFORMATION) - error code: {ErrorCode}", lastError);
        }

        return false;
    }

    public bool IsWindowsExecutable(string path)
    {
        if (PInvoke.GetBinaryType(path, out var type))
        {
            return type is SCS_32BIT_BINARY or SCS_64BIT_BINARY;
        }

        return false;
    }
}