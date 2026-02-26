using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace AppSwitcher.Utils;

public class ProcessPathExtractor(ILogger<ProcessPathExtractor> logger)
{
#pragma warning disable IDE1006 // Naming Styles
    // ReSharper disable InconsistentNaming
    private const uint ERROR_INSUFFICIENT_BUFFER = 122;
    // ReSharper restore InconsistentNaming
#pragma warning restore IDE1006 // Naming Styles

    public string ? GetProcessImageName(uint processId)
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

    public string? GetPathFromRegistry(string processNameWithExtension)
    {
        var keyName = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{processNameWithExtension}";

        var searchLocations = new[] { Registry.CurrentUser, Registry.LocalMachine };

        foreach (var searchLocation in searchLocations)
        {
            using var key = searchLocation.OpenSubKey(keyName);
            if (key?.GetValue("") is string path && File.Exists(path))
            {
                return path;
            }
        }

        return null;
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