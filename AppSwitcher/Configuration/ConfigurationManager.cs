using AppSwitcher.Configuration.Migrations;
using Microsoft.Extensions.Logging;

namespace AppSwitcher.Configuration;

internal class ConfigurationManager(
    ConfigurationService configService,
    ConfigurationValidator configValidator,
    MigrationRunner migrationRunner,
    PackagedAppPathSanitizer pathSanitizer,
    ILogger<ConfigurationManager> logger)
{
    private Configuration? _currentConfiguration;
    private bool _migrationsRun;

    public event Action<Configuration>? ConfigurationChanged;

    public Configuration? GetConfiguration()
    {
        if (!_migrationsRun)
        {
            if (!migrationRunner.RunPending())
            {
                return null;
            }
            _migrationsRun = true;
        }

        if (_currentConfiguration is null)
        {
            LoadConfiguration();
        }

        return _currentConfiguration;
    }

    public bool UpdateConfiguration(Configuration newConfig)
    {
        try
        {
            configService.WriteConfiguration(newConfig);
            _currentConfiguration = newConfig;
            logger.LogInformation("Configuration updated successfully");
            ConfigurationChanged?.Invoke(newConfig);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating configuration");
            return false;
        }
    }

    private void LoadConfiguration()
    {
        try
        {
            var config = configService.ReadConfiguration();

            var sanitized = pathSanitizer.Sanitize(config);
            if (sanitized is not null)
            {
                configService.WriteConfiguration(sanitized);
                config = sanitized;
            }

            configValidator.ValidateAndLog(config);

            _currentConfiguration = config;
            logger.LogInformation("Configuration loaded successfully");
            ConfigurationChanged?.Invoke(config);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading configuration");
        }
    }
}