using AppSwitcher.Extensions;
using AppSwitcher.Stats.Storage;
using AppSwitcher.WindowDiscovery;
using LiteDB;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;

namespace AppSwitcher.Stats;

internal class AppRegistryCache(
    LiteDatabase database,
    IWindowEnumerator windowEnumerator,
    IPackagedAppsService packagedAppsService,
    IProcessInspector processInspector,
    ILogger<AppRegistryCache> logger) : IAppRegistryCache
{
    private readonly ConcurrentDictionary<string, string> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public void Prepopulate(Configuration.Configuration config)
    {
        using var _ = logger.MeasureTime("Prepopulating app registry entries");
        var windows = windowEnumerator.GetWindows();
        var col = database.GetCollection<AppRegistryDocument>(AppRegistryDocument.CollectionName);
        var appRegistryDocuments = col.FindAll();

        // from db first
        foreach (var appRegistryDocument in appRegistryDocuments)
        {
            _cache[appRegistryDocument.ProcessName] = appRegistryDocument.DisplayName;
        }

        // all running windows
        var installedPaths = packagedAppsService.GetInstalledPaths();
        foreach (var window in windows.Where(window => !_cache.ContainsKey(window.ProcessName)))
        {
            Add(window.ProcessName, window.ProcessImagePath, installedPaths);
        }

        // all configured apps - some of them may not be running now so previous loop wouldn't cover them
        foreach (var app in config.Applications.Where(app => !_cache.ContainsKey(app.ProcessName)))
        {
            Add(app.ProcessName, app.ProcessPath, installedPaths);
        }
    }

    /// <summary>
    /// Adds the process to the cache if not already present, resolving its display name
    /// via <see cref="IProcessInspector"/> and persisting to LiteDB.
    /// </summary>
    public bool TryAdd(string processName, string processPath)
    {
        if (_cache.ContainsKey(processName))
        {
            return false;
        }

        var installedPaths = packagedAppsService.GetInstalledPaths();
        Add(processName, processPath, installedPaths);
        return true;
    }

    /// <summary>
    /// Gets display name for the process. It will try to resolve it if <see cref="processPath"/> is provided
    /// </summary>
    public string GetDisplayName(string processName, string? processPath = null)
    {
        if (_cache.TryGetValue(processName, out var cached))
        {
            return cached;
        }

        if (!string.IsNullOrEmpty(processPath) && TryAdd(processName, processPath))
        {
            return _cache[processName];
        }

        return Path.GetFileNameWithoutExtension(processName);
    }

    private void Add(string processName, string processPath, IReadOnlySet<string> installedPaths)
    {
        var isPackagedApp = installedPaths.Contains(Path.GetDirectoryName(processPath)!);

        var displayName = isPackagedApp
            ? packagedAppsService.GetDisplayName(processPath)
            : null;

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = processInspector.GetProcessDisplayName(processPath);
        }

        _cache[processName] = displayName;
        PersistToRegistry(processName, displayName);
    }

    private void PersistToRegistry(string processName, string displayName)
    {
        try
        {
            var col = database.GetCollection<AppRegistryDocument>(AppRegistryDocument.CollectionName);
            if (col.FindById(processName) is null)
            {
                col.Insert(new AppRegistryDocument
                {
                    ProcessName = processName, DisplayName = displayName
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist app registry entry for {ProcessName}", processName);
        }
    }
}