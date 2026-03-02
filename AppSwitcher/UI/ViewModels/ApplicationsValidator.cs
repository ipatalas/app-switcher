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

        var nonDefaultCycleSameLetterError = applications
            .Where(a => a.Key != Key.None)
            .GroupBy(a => a.Key)
            .Where(g => g.Count() > 1 && g.Any(item => item.CycleMode != CycleMode.Default))
            .SelectMany(g => g)
            .ToList();
        if (nonDefaultCycleSameLetterError.Count > 0)
        {
            errors.Add(new ApplicationValidationError(
                nonDefaultCycleSameLetterError,
                "Multiple apps with the same key are only allowed if they all are in Default mode"));
        }

        return errors;
    }
}