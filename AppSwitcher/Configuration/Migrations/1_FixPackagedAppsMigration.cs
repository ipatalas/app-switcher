using AppSwitcher.WindowDiscovery;
using LiteDB;
using System.Windows.Input;

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

        if (document is null)
        {
            // first run of the application won't have any settings yet
            return;
        }

        var updatedApps = new List<ApplicationConfigurationDocument>();
        var changed = false;

        foreach (var app in document.Applications)
        {
            if (app.Type == ApplicationType.Win32)
            {
                var info = packagedAppsService.GetByInstalledPath(app.ProcessPath, null);
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

file class SettingsDocument
{
    [BsonId] public int Id { get; set; }

    public int? ModifierIdleTimeoutMs { get; init; }

    public Key Modifier { get; init; }

    public List<ApplicationConfigurationDocument> Applications { get; init; } = [];

    public bool PulseBorderEnabled { get; init; }

    public AppThemeSetting Theme { get; init; }
}

file class ApplicationConfigurationDocument
{
    public Key Key { get; init; }

    public string ProcessPath { get; init; } = string.Empty;

    public CycleMode CycleMode { get; init; }

    public bool StartIfNotRunning { get; init; }

    public ApplicationType Type { get; init; } = ApplicationType.Win32;

    public string? Aumid { get; init; }
}