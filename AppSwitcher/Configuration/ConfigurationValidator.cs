using Microsoft.Extensions.Logging;
using System.Windows.Input;

namespace AppSwitcher.Configuration;

internal class ConfigurationValidator(ILogger<ConfigurationValidator> logger)
{
    private readonly Key[] _validModifiers = [Key.LeftCtrl, Key.LeftAlt, Key.RightCtrl, Key.RightAlt, Key.Apps, Key.RightShift];

    public ValidationResult ValidateAndLog(Configuration configuration)
    {
        var result = Validate(configuration);

        if (result.Status == ValidationResultStatus.Error)
        {
            var message = result.Message;
            if (result.Process is not null)
            {
                message = $"[{result.Process}] {message}";
            }
            logger.LogError("Config validation error: {Message}", message);
        }

        return result;
    }

    private ValidationResult Validate(Configuration configuration)
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

            if (app.Key is Key.None or not (>=Key.A and <= Key.Z))
            {
                return ValidationResult.Error(app.ProcessName, "Application key was not detected correctly - is should be a single letter");
            }

            if (app is { StartIfNotRunning: true, HasFullProcessPath: false })
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