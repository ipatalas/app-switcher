using Microsoft.Extensions.Logging;
using System.Windows.Input;

namespace AppSwitcher.Configuration;

internal class ConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;
    private readonly Key[] _validModifiers = { Key.RightCtrl, Key.RightAlt, Key.Apps, Key.RightShift };

    public ConfigurationValidator(ILogger<ConfigurationValidator> logger)
    {
        this._logger = logger;
    }

    public ValidationResult ValidateAndLog(Configuration configuration)
    {
        var result = Validate(configuration);

        if (result.Status == ValidationResultStatus.Error)
        {
            _logger.LogError("Config validation error: {Error}", result.Message);
        }

        return result;
    }

    public ValidationResult Validate(Configuration configuration)
    {
        if (!_validModifiers.Contains(configuration.Modifier))
        {
            return ValidationResult.Error($"Invalid modifier key ({configuration.Modifier}) - must be one of: {string.Join(", ", _validModifiers)}");
        }

        if (configuration.Applications.Count == 0)
        {
            return ValidationResult.Error("No applications configured - AppSwitcher will do nothing");
        }

        foreach (var app in configuration.Applications)
        {
            if (string.IsNullOrWhiteSpace(app.Process))
            {
                return ValidationResult.Error("Application process must be set correctly");
            }

            if (app.Key == Key.None || app.Key is not (>=Key.A and <= Key.Z))
            {
                return ValidationResult.Error(app.ProcessName, "Application key was not detected correctly - is should be a single letter");
            }

            if (app.StartIfNotRunning && app.HasFullProcessPath is false)
            {
                return ValidationResult.Error(app.ProcessName, "Application process must be a full path if StartIfNotRunning is set to true");
            }
        }

        return ValidationResult.Success;
    }
}

internal record ValidationResult(ValidationResultStatus Status, string? Process, string? Message)
{
    public static ValidationResult Success => new(ValidationResultStatus.Success, null, null);
    public static ValidationResult Error(string message) => new(ValidationResultStatus.Error, null, message);
    public static ValidationResult Error(string process, string message) => new(ValidationResultStatus.Error, process, message);
}

internal enum ValidationResultStatus
{
    Success,
    Error
}