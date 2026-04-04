using LiteDB;
using System.Windows.Input;

namespace AppSwitcher.Configuration.Storage;

// Default values set for properties here will be used when someone is bumping from v1 to v2 and there is a new field in v2
// SeedDefaults won't be called in such case so whatever defaults are set here will be used when settings are deserialized from the database and this field was missing
internal class SettingsDocument
{
    [BsonId]
    public int Id { get; set; }

    public Key Modifier { get; init; }

    public List<ApplicationConfigurationDocument> Applications { get; init; } = [];

    public bool PulseBorderEnabled { get; init; }

    public AppThemeSetting Theme { get; init; }

    public bool OverlayEnabled { get; init; }

    public int OverlayShowDelayMs { get; init; } = 1000;

    public bool OverlayKeepOpenWhileModifierHeld { get; init; }

    public Configuration ToConfiguration() =>
        new(
            Modifier,
            Applications.Select(a => a.ToApplicationConfiguration()).ToList(),
            PulseBorderEnabled,
            Theme,
            OverlayEnabled,
            OverlayShowDelayMs,
            OverlayKeepOpenWhileModifierHeld);

    public static SettingsDocument FromConfiguration(Configuration config, int id) =>
        new()
        {
            Id = id,
            Modifier = config.Modifier,
            Applications = config.Applications
                .Select(ApplicationConfigurationDocument.FromApplicationConfiguration)
                .ToList(),
            PulseBorderEnabled = config.PulseBorderEnabled,
            Theme = config.Theme,
            OverlayEnabled = config.OverlayEnabled,
            OverlayShowDelayMs = config.OverlayShowDelayMs,
            OverlayKeepOpenWhileModifierHeld = config.OverlayKeepOpenWhileModifierHeld
        };
}