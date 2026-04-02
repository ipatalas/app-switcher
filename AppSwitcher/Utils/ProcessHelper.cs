using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Threading;

namespace AppSwitcher.Utils;

internal class ProcessHelper(ILogger<ProcessHelper> logger)
{
    private const int ERROR_ACCESS_DENIED = 5;

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
}