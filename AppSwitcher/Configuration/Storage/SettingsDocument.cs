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

    public bool PulseBorderEnabled { get; init; } = true;

    public AppThemeSetting Theme { get; init; } = AppThemeSetting.System;

    public bool OverlayEnabled { get; init; } = true;

    public int OverlayShowDelayMs { get; init; } = 1000;

    public Configuration ToConfiguration() =>
        new(
            ModifierIdleTimeoutMs,
            Modifier,
            Applications.Select(a => a.ToApplicationConfiguration()).ToList(),
            PulseBorderEnabled,
            Theme,
            OverlayEnabled,
            OverlayShowDelayMs);

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
            OverlayShowDelayMs = config.OverlayShowDelayMs
        };
}