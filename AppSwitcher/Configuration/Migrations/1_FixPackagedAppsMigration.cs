using AppSwitcher.Configuration.Storage;
using AppSwitcher.Utils;
using LiteDB;

namespace AppSwitcher.Configuration.Migrations;

internal class FixPackagedAppsMigration(IPackagedAppsService packagedAppsService) : IMigration
{
    private const string SettingsCollectionName = "settings";
    private const int SettingsDocumentId = 1;

    public int Version => 1;

    public void Up(LiteDatabase db)
    {
        var collection = db.GetCollection<SettingsDocument>(SettingsCollectionName);
        var document = collection.FindById(SettingsDocumentId);

        var updatedApps = new List<ApplicationConfigurationDocument>();
        var changed = false;

        foreach (var app in document.Applications)
        {
            if (app.Type == ApplicationType.Win32)
            {
                var info = packagedAppsService.GetByInstalledPath(app.ProcessPath);
                if (info is not null)
                {
                    updatedApps.Add(new ApplicationConfigurationDocument
                    {
                        Key = app.Key,
                        ProcessPath = app.ProcessPath,
                        CycleMode = app.CycleMode,
                        StartIfNotRunning = app.StartIfNotRunning,
                        Type = ApplicationType.Packaged,
                        Aumid = info.Aumid
                    });
                    changed = true;
                    continue;
                }
            }

            updatedApps.Add(app);
        }

        if (changed)
        {
            collection.Upsert(new SettingsDocument
            {
                Id = document.Id,
                ModifierIdleTimeoutMs = document.ModifierIdleTimeoutMs,
                Modifier = document.Modifier,
                Applications = updatedApps,
                PulseBorderEnabled = document.PulseBorderEnabled,
                Theme = document.Theme
            });
        }
    }
}