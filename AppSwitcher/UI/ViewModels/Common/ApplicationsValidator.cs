using AppSwitcher.Configuration;
using System.Windows.Input;

namespace AppSwitcher.UI.ViewModels.Common;

internal record ApplicationValidationError(
    IReadOnlyList<ApplicationShortcutViewModel> AffectedApps,
    string Message);

internal class ApplicationsValidator(ConfigurationValidator configValidator)
{
    public IReadOnlyList<ApplicationValidationError> Validate(
        IReadOnlyList<ApplicationShortcutViewModel> applications)
    {
        List<ApplicationValidationError> errors = [];

        foreach (var app in applications)
        {
            if (app.Key == (Key)(-1))
            {
                // in listening mode, too soon to show error
                continue;
            }

            var result = configValidator.ValidateApplication(app);
            if (result.IsError)
            {
                errors.Add(new ApplicationValidationError([app], result.Message));
            }
        }

        var nonNextAppCycleSameLetterError = applications
            .Where(a => a.Key != Key.None)
            .GroupBy(a => a.Key)
            .Where(g => g.Count() > 1 && g.Any(item => item.CycleMode != CycleMode.NextApp))
            .SelectMany(g => g)
            .ToList();
        if (nonNextAppCycleSameLetterError.Count > 0)
        {
            errors.Add(new ApplicationValidationError(
                nonNextAppCycleSameLetterError,
                "Multiple apps with the same key are only allowed if they all are in Next App mode"));
        }

        return errors;
    }
}