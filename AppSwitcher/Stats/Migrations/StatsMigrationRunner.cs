using AppSwitcher.Configuration.Storage;
using AppSwitcher.Stats.Storage;
using Microsoft.Extensions.Logging;

namespace AppSwitcher.Stats.Migrations;

internal class StatsMigrationRunner(
    StatsDbProvider dbProvider,
    IEnumerable<IStatsMigration> migrations,
    ILogger<StatsMigrationRunner> logger)
{
    private const string CollectionName = "_migrations";

    public bool RunPending()
    {
        if (!dbProvider.Exists())
        {
            logger.LogDebug("Stats DB does not exist — skipping migrations");
            return true;
        }

        using var db = dbProvider.Get();
        var collection = db.GetCollection<MigrationRecord>(CollectionName);
        var executedVersions = collection.FindAll().Select(r => r.Version).ToHashSet();

        var pending = migrations
            .Where(m => !executedVersions.Contains(m.Version))
            .OrderBy(m => m.Version)
            .ToList();

        if (pending.Count == 0)
        {
            logger.LogDebug("No pending stats migrations");
            return true;
        }

        foreach (var migration in pending)
        {
            logger.LogInformation("Running stats migration v{Version}", migration.Version);
            db.BeginTrans();
            try
            {
                migration.Up(db);
                collection.Insert(new MigrationRecord
                {
                    Version = migration.Version,
                    ExecutedAt = DateTime.UtcNow
                });
                db.Commit();
                logger.LogInformation("Stats migration v{Version} completed", migration.Version);
            }
            catch (Exception ex)
            {
                db.Rollback();
                logger.LogError(ex, "Stats migration v{Version} failed — rolled back", migration.Version);
                return false;
            }
        }

        db.Checkpoint();

        return true;
    }
}