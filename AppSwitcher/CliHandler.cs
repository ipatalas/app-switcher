using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AppSwitcher;

internal class CliHandler
{
    private readonly ILogger<CliHandler> _logger;
    private readonly WindowHelper _windowHelper;
    private readonly AutoStart _autoStart;

    public CliHandler(ILogger<CliHandler> logger, WindowHelper windowHelper, AutoStart autoStart)
    {
        _logger = logger;
        _windowHelper = windowHelper;
        _autoStart = autoStart;
    }

    public bool Handle(string[] args)
    {
        if (args.Length == 0)
        {
            return false;
        }

        var command = args[0];

        _logger.LogDebug("Command: {Command}", command);

        switch (command)
        {
            case "--log-all-windows":
                _windowHelper.LogAllWindows();
                break;
            case "--auto-start":
                if (!_autoStart.CreateShortcut())
                {
                    MessageBox.Show("There was an error while trying to create auto start shortcut. See log file for details.", "AppSwitcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                break;
            case "--help":
                var message = new StringBuilder();
                message.AppendLine("AppSwitcher.exe [command]");
                message.AppendLine();
                message.AppendLine("Commands:");
                message.AppendLine("--log-all-windows: Log all windows to log file");
                message.AppendLine("--auto-start: Add application to system Startup");
                message.AppendLine("--help: Show this help message");

                MessageBox.Show(message.ToString(), "AppSwitcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
                break;
            default:
                _logger.LogWarning("Unknown command: {Command}", command);
                break;
        }

        return true;
    }
}
