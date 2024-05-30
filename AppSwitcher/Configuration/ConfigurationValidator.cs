using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;

namespace AppSwitcher.Configuration;

internal class ConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> logger;
    private readonly Key[] validModifiers = { Key.LeftCtrl, Key.RightCtrl, Key.LeftAlt, Key.RightAlt, Key.Apps, Key.F };

    public ConfigurationValidator(ILogger<ConfigurationValidator> logger)
    {
        this.logger = logger;
    }

    public ValidationResult ValidateAndLog(Configuration configuration)
    {
        var result = Validate(configuration);

        if (result.Status == ValidationResultStatus.Error)
        {
            logger.LogError("Config validation error: {Error}", result.Message);
        }

        return result;
    }

    public ValidationResult Validate(Configuration configuration)
    {
        if (configuration.Modifier == Key.None)
        {
            return ValidationResult.Error("Modifier key must be set correctly");
        }  

        if (!validModifiers.Contains(configuration.Modifier))
        {
            return ValidationResult.Error($"Invalid modifier key - {configuration.Modifier}");
        }

        if (configuration.Applications.Count == 0)
        {
            return ValidationResult.Error("No applications configured - AppSwitcher will do nothing");
        }

        foreach (var app in configuration.Applications)
        {
            if (app.Key == Key.None || app.Key is not (>=Key.A and <= Key.Z))
            {
                return ValidationResult.Error("Application key was not detected correctly - is should be a single letter");
            }

            if (string.IsNullOrWhiteSpace(app.Process))
            {
                return ValidationResult.Error("Application process must be set correctly");
            }
        }

        return ValidationResult.Success;
    }
}

internal record ValidationResult(ValidationResultStatus Status, string? Message)
{
    public static ValidationResult Success => new(ValidationResultStatus.Success, null);
    public static ValidationResult Error(string message) => new(ValidationResultStatus.Error, message);
}

internal enum ValidationResultStatus
{
    Success,
    Error
}