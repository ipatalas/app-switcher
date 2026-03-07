using LiteDB;
using System.Windows.Input;

namespace AppSwitcher.Configuration.Storage;

internal class SettingsDocument
{
    [BsonId]
    public int Id { get; set; }

    public int? ModifierIdleTimeoutMs { get; init; }

    public Key Modifier { get; init; }

    public List<ApplicationConfigurationDocument> Applications { get; init; } = [];

    public Configuration ToConfiguration() =>
        new(
            ModifierIdleTimeoutMs,
            Modifier,
            Applications.Select(a => a.ToApplicationConfiguration()).ToList());

    public static SettingsDocument FromConfiguration(Configuration config, int id) =>
        new()
        {
            Id = id,
            ModifierIdleTimeoutMs = config.ModifierIdleTimeoutMs,
            Modifier = config.Modifier,
            Applications = config.Applications
                .Select(ApplicationConfigurationDocument.FromApplicationConfiguration)
                .ToList()
        };
}