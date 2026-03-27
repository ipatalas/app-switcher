using AppSwitcher.Configuration.Storage;
using LiteDB;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Windows.Input;

namespace AppSwitcher.Configuration;

internal class ConfigurationService(LiteDatabase database, ILogger<ConfigurationService> logger)
{
    private const string CollectionName = "settings";
    private const int SettingsDocumentId = 1;

    public Configuration ReadConfiguration()
    {
        var sw = Stopwatch.StartNew();

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
        finally
        {
            logger.LogDebug("Read configuration in {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    public void WriteConfiguration(Configuration config)
    {
        var sw = Stopwatch.StartNew();

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
        finally
        {
            logger.LogDebug("Wrote configuration in {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    private SettingsDocument SeedDefaults(ILiteCollection<SettingsDocument> collection)
    {
        var defaults = new SettingsDocument
        {
            Id = SettingsDocumentId,
            ModifierIdleTimeoutMs = 0,
            Modifier = Key.RightCtrl,
            Applications = [],
            PulseBorderEnabled = true,
            Theme = AppThemeSetting.System,
            OverlayEnabled = false,
            OverlayShowDelayMs = 1000,
            OverlayKeepOpenWhileModifierHeld = true
        };

        collection.Insert(defaults);
        logger.LogInformation("Default configuration seeded successfully");
        return defaults;
    }
}