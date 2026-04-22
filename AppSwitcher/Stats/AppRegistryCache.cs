using AppSwitcher.Stats.Storage;
using LiteDB;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace AppSwitcher.Stats;

internal class AppRegistryCache(LiteDatabase database, ILogger<AppRegistryCache> logger)
{
    private const string Collection = "app_registry";

    private readonly ConcurrentDictionary<string, string> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the display name for the given process, using the in-memory cache.
    /// On a cache miss, resolves via FileVersionInfo and persists to LiteDB.
    /// </summary>
    public string GetOrAdd(string processName, string? processPath)
    {
        if (_cache.TryGetValue(processName, out var cached))
        {
            return cached;
        }

        var displayName = ResolveDisplayName(processName, processPath);
        _cache[processName] = displayName;
        PersistToRegistry(processName, displayName);
        return displayName;
    }

    private string ResolveDisplayName(string processName, string? processPath)
    {
        if (processPath is null || !File.Exists(processPath))
        {
            return Path.GetFileNameWithoutExtension(processName);
        }

        try
        {
            var description = FileVersionInfo.GetVersionInfo(processPath).FileDescription;
            return string.IsNullOrWhiteSpace(description)
                ? Path.GetFileNameWithoutExtension(processName)
                : description;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read FileVersionInfo for {ProcessPath}", processPath);
            return Path.GetFileNameWithoutExtension(processName);
        }
    }

    private void PersistToRegistry(string processName, string displayName)
    {
        try
        {
            var col = database.GetCollection<AppRegistryDocument>(Collection);
            if (col.FindById(processName) is null)
            {
                col.Insert(new AppRegistryDocument
                {
                    ProcessName = processName,
                    DisplayName = displayName,
                    FirstSeen = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist app registry entry for {ProcessName}", processName);
        }
    }
}