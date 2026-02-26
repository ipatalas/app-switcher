using System.Diagnostics;
using System.IO;

namespace AppSwitcher.Utils;

public class AppLocator(ProcessPathExtractor processPathExtractor)
{
    private readonly string[] _envPaths =
        Environment.GetEnvironmentVariable("PATH")?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? [];

    public string? FindExecutablePath(string processName)
    {
        // 1. Check if the process name is already a full path
        if (Path.IsPathRooted(processName))
        {
            return processName;
        }

        // 2. Check if the process is running and get its image name
        var processNameWithoutExt = Path.GetFileNameWithoutExtension(processName);
        var process = Process.GetProcessesByName(processNameWithoutExt)
            .FirstOrDefault();
        if (process != null && processPathExtractor.GetProcessImageName((uint)process.Id) is { } executablePath)
        {
            return executablePath;
        }

        // 3. Special handling for Visual Studio Code, which may be launched via "code.cmd" in the PATH
        if (processName.Equals("code.exe", StringComparison.OrdinalIgnoreCase))
        {
            var vscodePath = GetVsCodePath();
            if (vscodePath != null)
            {
                return vscodePath;
            }
        }

        // 4. Check the registry (App paths) for the executable path
        var pathFromRegistry = processPathExtractor.GetPathFromRegistry(processName);
        if (pathFromRegistry != null)
        {
            return pathFromRegistry;
        }

        // 5. Search in the PATH environment variable
        var pathFromEnv = GetFromEnv(processName);
        if (pathFromEnv != null)
        {
            return pathFromEnv;
        }

        return null;
    }

    private string? GetVsCodePath()
    {
        var cmdPath = GetFromEnv("code.cmd");
        if (string.IsNullOrEmpty(cmdPath))
        {
            return null;
        }

        var binDirectory = Path.GetDirectoryName(cmdPath)!;
        var parentDirectory = Directory.GetParent(binDirectory)!.FullName;
        var codeExecutablePath = Path.Combine(parentDirectory, "Code.exe");

        return File.Exists(codeExecutablePath) ? codeExecutablePath : null;
    }

    private string? GetFromEnv(string processName)
    {
        foreach (var path in _envPaths)
        {
            var fullPath = Path.Combine(path, processName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }
}