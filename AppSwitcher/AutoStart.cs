using IWshRuntimeLibrary;
using Microsoft.Extensions.Logging;
using System.IO;

namespace AppSwitcher;
internal class AutoStart
{
    private readonly ILogger<AutoStart> _logger;

    public AutoStart(ILogger<AutoStart> logger)
    {
        _logger = logger;
    }

    public bool CreateShortcut()
    {
        var executablePath = Environment.ProcessPath;
        if (executablePath is null)
        {
            _logger.LogError("Cannot get current process executable path.");
            return false;
        }
        var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var fileName = Path.GetFileNameWithoutExtension(executablePath);
        var originalFileDirectory = Path.GetDirectoryName(executablePath);

        string link = Path.Combine(startupPath, fileName + ".lnk");
        var shell = new WshShell();
        if (shell.CreateShortcut(link) is IWshShortcut shortcut)
        {
            shortcut.TargetPath = executablePath;
            shortcut.WorkingDirectory = originalFileDirectory;
            shortcut.Save();
        }

        return true;
    }
}
