using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppSwitcher.Configuration;

internal class ConfigurationReader
{
    private readonly ILogger<ConfigurationReader> _logger;

    private readonly JsonSerializerOptions _options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public ConfigurationReader(ILogger<ConfigurationReader> logger)
    {
        this._logger = logger;
    }

    public Configuration? ReadConfiguration()
    {
        var sw = Stopwatch.StartNew();
        var configPath = "config.json";

        if (!File.Exists(configPath))
        {
            _logger.LogError("Configuration file not found, using default configuration");
            return null;
        }

        try
        {
            using var fileStream = File.OpenRead(configPath);
            return JsonSerializer.Deserialize<Configuration>(fileStream, _options)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading configuration file");
            return null;
        }
        finally
        {
            _logger.LogDebug($"Read configuration in {sw.ElapsedMilliseconds}ms");
        }
    }
}
