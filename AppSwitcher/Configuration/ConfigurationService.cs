using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppSwitcher.Configuration;

internal class ConfigurationService(ILogger<ConfigurationService> logger)
{
    private const string _configPath = "config.json";

    private readonly JsonSerializerOptions _options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public Configuration? ReadConfiguration()
    {
        var sw = Stopwatch.StartNew();

        if (!File.Exists(_configPath))
        {
            logger.LogError("Configuration file not found: {ConfigPath}", _configPath);
            return null;
        }

        try
        {
            using var fileStream = File.OpenRead(_configPath);
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

    public void WriteConfiguration(Configuration newConfig)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var json = JsonSerializer.Serialize(newConfig, _options);
            File.WriteAllText(_configPath, json);
            logger.LogInformation("Configuration written to file successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error writing configuration file");
        }
        finally
        {
            logger.LogDebug($"Wrote configuration in {sw.ElapsedMilliseconds}ms");
        }
    }
}
