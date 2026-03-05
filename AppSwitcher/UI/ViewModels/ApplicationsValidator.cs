using AppSwitcher.Configuration;
using System.Windows.Input;

namespace AppSwitcher.UI.ViewModels;

internal record ApplicationValidationError(
    IReadOnlyList<ApplicationShortcutViewModel> AffectedApps,
    string Message);

internal class ApplicationsValidator
{
    public IReadOnlyList<ApplicationValidationError> Validate(
        IReadOnlyList<ApplicationShortcutViewModel> applications)
    {
        List<ApplicationValidationError> errors = [];

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