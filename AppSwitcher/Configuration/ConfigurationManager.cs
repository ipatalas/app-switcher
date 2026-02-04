using Microsoft.Extensions.Logging;
using System.IO;
using Timer = System.Threading.Timer;

namespace AppSwitcher.Configuration;

internal class ConfigurationManager : IDisposable
{
    private readonly ConfigurationService _configService;
    private readonly ConfigurationValidator _configValidator;
    private readonly ILogger<ConfigurationManager> _logger;
    private readonly FileSystemWatcher _fileWatcher;
    private Configuration? _currentConfiguration;
    private Timer? _fileChangeDebounceTimer;

    public event Action<Configuration>? ConfigurationChanged;

    public ConfigurationManager(ConfigurationService configService, ConfigurationValidator configValidator, ILogger<ConfigurationManager> logger)
    {
        _configService = configService;
        _configValidator = configValidator;
        _logger = logger;

        _fileWatcher = new FileSystemWatcher
        {
            Path = Directory.GetCurrentDirectory(),
            Filter = "config.json",
            NotifyFilter = NotifyFilters.LastWrite
        };

        _fileWatcher.Changed += OnConfigFileChanged;
        _fileWatcher.EnableRaisingEvents = true;
    }

    public Configuration? GetConfiguration()
    {
        if (_currentConfiguration == null)
        {
            LoadConfiguration();
        }
        return _currentConfiguration;
    }

    public bool UpdateConfiguration(Configuration newConfig)
    {
        _fileWatcher.EnableRaisingEvents = false;

        try
        {
            _configService.WriteConfiguration(newConfig);
            _currentConfiguration = newConfig;
            _logger.LogInformation("Configuration updated successfully");
            ConfigurationChanged?.Invoke(newConfig);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration");
            return false;
        }
        finally
        {
            _fileWatcher.EnableRaisingEvents = true;
        }
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce file changes (FileSystemWatcher can fire multiple events)
        _fileChangeDebounceTimer?.Dispose();
        _fileChangeDebounceTimer = new Timer(OnDebounceTimerElapsed, null, 100, Timeout.Infinite);
    }

    private void OnDebounceTimerElapsed(object? state)
    {
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        try
        {
            var config = _configService.ReadConfiguration();
            if (config == null)
            {
                _logger.LogError("Configuration file could not be read");
                return;
            }

            if (_configValidator.ValidateAndLog(config).Status == ValidationResultStatus.Success)
            {
                _currentConfiguration = config;
                _logger.LogInformation("Configuration (re)loaded successfully");
                ConfigurationChanged?.Invoke(config);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error (re)loading configuration");
        }
    }

    public void Dispose()
    {
        _fileWatcher.Dispose();
        _fileChangeDebounceTimer?.Dispose();
    }
}