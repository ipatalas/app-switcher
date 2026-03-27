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

    public bool PulseBorderEnabled { get; init; }

    public AppThemeSetting Theme { get; init; }

    public bool OverlayEnabled { get; init; }

    public int OverlayShowDelayMs { get; init; }

    public bool OverlayKeepOpenWhileModifierHeld { get; init; }

    public Configuration ToConfiguration() =>
        new(
            ModifierIdleTimeoutMs,
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
            ModifierIdleTimeoutMs = config.ModifierIdleTimeoutMs,
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