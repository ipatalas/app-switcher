using AppSwitcher.Configuration.Storage;
using AppSwitcher.Extensions;
using LiteDB;
using Microsoft.Extensions.Logging;
using System.Windows.Input;

namespace AppSwitcher.Configuration;

internal class ConfigurationService(LiteDatabase database, ILogger<ConfigurationService> logger)
{
    private const string CollectionName = "settings";
    private const int SettingsDocumentId = 1;

    public Configuration ReadConfiguration()
    {
        using var _ = logger.MeasureTime("ReadConfiguration");

        try
        {
            var collection = database.GetCollection<SettingsDocument>(CollectionName);
            var document = collection.FindById(SettingsDocumentId);

            if (document is null)
            {
                logger.LogInformation("No configuration found - seeding defaults");
                document = SeedDefaults(collection);
            }

            return document.ToConfiguration();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading configuration from database");
            throw;
        }
    }

    public void WriteConfiguration(Configuration config)
    {
        using var _ = logger.MeasureTime("WriteConfiguration");

        try
        {
            var collection = database.GetCollection<SettingsDocument>(CollectionName);
            var document = SettingsDocument.FromConfiguration(config, SettingsDocumentId);
            collection.Upsert(document);
            logger.LogInformation("Configuration written to database successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error writing configuration to database");
            throw;
        }
    }

    private SettingsDocument SeedDefaults(ILiteCollection<SettingsDocument> collection)
    {
        var defaults = new SettingsDocument
        {
            Id = SettingsDocumentId,
            Modifier = Key.RightCtrl,
            Applications = [],
            PulseBorderEnabled = true,
            Theme = AppThemeSetting.System,
            OverlayEnabled = false,
            OverlayShowDelayMs = 1000,
            OverlayKeepOpenWhileModifierHeld = true,
            DynamicModeEnabled = true
        };

        collection.Insert(defaults);
        logger.LogInformation("Default configuration seeded successfully");
        return defaults;
    }
}