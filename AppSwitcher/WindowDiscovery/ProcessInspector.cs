using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
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

    // tl;dr - it's hard to guess/check whether a starting process will be elevated but easier to monitor the outcome
    // mainly an issue of taskmgr.exe which first starts a normal process which then starts another elevated one
    public async Task<bool> WaitForPotentialElevation(string processPath, int timeoutMs = 2000)
    {
        var processName = Path.GetFileNameWithoutExtension(processPath);

        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Any(process => NeedsElevation((uint)process.Id)))
            {
                logger.LogDebug("Detected elevated started process in {Duration}ms", sw.ElapsedMilliseconds);
                return true;
            }

            await Task.Delay(200);
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