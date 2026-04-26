using AppSwitcher.Stats.Storage;
using Microsoft.Extensions.Logging;

namespace AppSwitcher.Stats;

internal class StatsRepository(StatsDbProvider dbProvider, ILogger<StatsRepository> logger)
{
    /// <summary>
    /// Returns all historical daily buckets, excluding today.
    /// </summary>
    public IReadOnlyList<DailyBucketDocument> GetAllHistoricBuckets()
    {
        var today = DateTime.Today;

        try
        {
            using var db = dbProvider.Get();
            var col = db.GetCollection<DailyBucketDocument>(DailyBucketDocument.CollectionName);
            return col.Find(b => b.Date < today)
                      .OrderByDescending(b => b.Date)
                      .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load historical stats from database");
            return [];
        }
    }
}