using AppSwitcher.Configuration.Storage;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace AppSwitcher.Configuration.Migrations;

internal class MigrationRunner(
    LiteDatabase db,
    IEnumerable<IMigration> migrations,
    ILogger<MigrationRunner> logger)
{
    private const string CollectionName = "_migrations";

    public bool RunPending()
    {
        var collection = db.GetCollection<MigrationRecord>(CollectionName);
        var executedVersions = collection.FindAll().Select(r => r.Version).ToHashSet();

        var pending = migrations
            .Where(m => !executedVersions.Contains(m.Version))
            .OrderBy(m => m.Version)
            .ToList();

        if (pending.Count == 0)
        {
            logger.LogDebug("No pending migrations");
            return true;
        }

        foreach (var migration in pending)
        {
            logger.LogInformation("Running migration v{Version}", migration.Version);
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
                logger.LogInformation("Migration v{Version} completed", migration.Version);
            }
            catch (Exception ex)
            {
                db.Rollback();
                logger.LogError(ex, "Migration v{Version} failed — rolled back", migration.Version);
                return false;
            }
        }

        return true;
    }
}