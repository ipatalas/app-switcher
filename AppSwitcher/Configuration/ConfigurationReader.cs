using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppSwitcher.Configuration;

internal class ConfigurationReader(ILogger<ConfigurationReader> logger)
{
    private readonly JsonSerializerOptions _options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public bool ConfigurationExists() => File.Exists("config.json");

    public Configuration? ReadConfiguration()
    {
        var sw = Stopwatch.StartNew();
        var configPath = "config.json";

        if (!File.Exists(configPath))
        {
            logger.LogError("Configuration file not found, using default configuration");
            return null;
        }

        try
        {
            using var fileStream = File.OpenRead(configPath);
            return JsonSerializer.Deserialize<Configuration>(fileStream, _options)!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading configuration file");
            return null;
        }
        finally
        {
            logger.LogDebug($"Read configuration in {sw.ElapsedMilliseconds}ms");
        }
    }
}
