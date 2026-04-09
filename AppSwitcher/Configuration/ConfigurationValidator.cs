using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Input;

namespace AppSwitcher.Configuration;

internal class ConfigurationValidator(ILogger<ConfigurationValidator> logger)
{
    private readonly Key[] _validModifiers =
    [
        Key.LeftCtrl, Key.LeftAlt, Key.LeftShift, Key.LWin, Key.RightCtrl,
        Key.RightAlt, Key.Apps, Key.RightShift, Key.Capital
    ];

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

        foreach (var app in configuration.Applications)
        {
            var result = ValidateApplication(app);
            if (result.Status == ValidationResultStatus.Error)
            {
                return result;
            }
        }

        return ValidationResult.Success;
    }

    public ValidationResult ValidateApplication(IApplicationConfiguration app)
    {
        if (app.Type == ApplicationType.Packaged)
        {
            if (string.IsNullOrWhiteSpace(app.Aumid))
            {
                return ValidationResult.Error(app.ProcessName, "Packaged application AUMID must be set. Re-add the application to fix it.");
            }
        }
        else
        {
            if (!File.Exists(app.ProcessPath))
            {
                return ValidationResult.Error(app.ProcessName,"Application process path missing or invalid. Re-add the application to fix it.");
            }
        }

        if (app.Key is Key.None or not (>=Key.A and <= Key.Z))
        {
            return ValidationResult.Error(app.ProcessName, "Application key invalid - it should be a single letter");
        }

        return ValidationResult.Success;
    }
}

internal record ValidationResult
{
    private ValidationResult(ValidationResultStatus Status, string? Process, string? Message)
    {
        this.Status = Status;
        this.Process = Process;
        this.Message = Message;
    }

    [MemberNotNullWhen(true, nameof(Message))]
    public bool IsError => Status == ValidationResultStatus.Error;

    public ValidationResultStatus Status { get; }
    public string? Process { get; }
    public string? Message { get; }

    public static ValidationResult Success => new(ValidationResultStatus.Success, null, null);
    public static ValidationResult Error(string message) => new(ValidationResultStatus.Error, null, message);
    public static ValidationResult Error(string process, string message) => new(ValidationResultStatus.Error, process, message);
}

internal enum ValidationResultStatus
{
    Success,
    Error
}