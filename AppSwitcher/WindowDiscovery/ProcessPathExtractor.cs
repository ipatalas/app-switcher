using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace AppSwitcher.WindowDiscovery;

public class ProcessPathExtractor(ILogger<ProcessPathExtractor> logger) : IProcessPathExtractor
{
    private const uint ERROR_INSUFFICIENT_BUFFER = 122;

    public string? GetProcessImageName(uint processId)
    {
        using var processHandle =
            PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false,
                processId);
        if (processHandle.IsInvalid)
        {
            logger.LogWarning("Failed to open process {ProcessId}", processId);
            return null;
        }

        var processImageName = GetProcessImageName((HANDLE)processHandle.DangerousGetHandle());
        if (processImageName is null)
        {
            logger.LogWarning("Failed to get process image name for process {ProcessId}", processId);
            return null;
        }

        return processImageName;
    }

    private unsafe string? GetProcessImageName(HANDLE handle)
    {
        const int startLength = (int)PInvoke.MAX_PATH;

        Span<char> buffer = stackalloc char[startLength + 1];
        char[]? rentedArray = null;

        try
        {
            while (true)
            {
                uint length = (uint)buffer.Length;
                fixed (char* pinnedBuffer = &MemoryMarshal.GetReference(buffer))
                {
                    if (PInvoke.QueryFullProcessImageName(handle, 0, pinnedBuffer, &length))
                    {
                        return buffer[..(int)length].ToString();
                    }
                    else if (Marshal.GetLastPInvokeError() != ERROR_INSUFFICIENT_BUFFER)
                    {
                        return null;
                    }
                }

                char[]? toReturn = rentedArray;
                buffer = rentedArray = ArrayPool<char>.Shared.Rent(buffer.Length * 2);
                if (toReturn is not null)
                {
                    ArrayPool<char>.Shared.Return(toReturn);
                }
            }
        }
        finally
        {
            if (rentedArray is not null)
            {
                ArrayPool<char>.Shared.Return(rentedArray);
            }
        }
    }
}