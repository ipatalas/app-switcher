using AppSwitcher.Stats.Storage;
using AppConfig = AppSwitcher.Configuration.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace AppSwitcher.Stats;

internal class StatsService(
    SessionStats sessionStats,
    AppRegistryCache registryCache,
    StatsDbProvider dbProvider,
    ILoggerFactory loggerFactory) : IDisposable
{
    private readonly Channel<StatsEvent> _channel = Channel.CreateBounded<StatsEvent>(
        new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleWriter = false,
            SingleReader = true
        });

    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private readonly ILogger<StatsService> _logger = loggerFactory.CreateLogger<StatsService>();
    private StatsConsumer? _consumer;
    private Task? _consumerTask;
    private volatile CancellationTokenSource? _cts;

    private bool IsRunning => _cts is not null;

    public void Start(bool statsEnabled)
    {
        if (statsEnabled)
        {
            StartConsumer();
        }
        else
        {
            _logger.LogInformation("Stats collection is disabled — consumer not started");
        }
    }

    public void UpdateConfiguration(AppConfig config)
    {
        var shouldRun = config.StatsEnabled;

        if (shouldRun && !IsRunning)
        {
            StartConsumer();
        }
        else if (!shouldRun && IsRunning)
        {
            StopConsumer("stats disabled");
        }
    }

    public void Enqueue(StatsEvent statsEvent)
    {
        if (!IsRunning)
        {
            return;
        }

        if (!_channel.Writer.TryWrite(statsEvent))
        {
            _logger.LogWarning("Stats channel full — event dropped for {Type}", statsEvent.GetType().Name);
        }
    }

    public void Dispose()
    {
        if (IsRunning)
        {
            StopConsumer("app exit");
        }

        _flushLock.Dispose();
    }

    private void StartConsumer()
    {
        LoadTodaysBucket();

        _cts = new CancellationTokenSource();
        _consumer = new StatsConsumer(
            _channel.Reader,
            sessionStats,
            registryCache,
            Flush,
            loggerFactory.CreateLogger<StatsConsumer>());

        _consumerTask = _consumer.StartAsync(_cts.Token);
        _logger.LogInformation("Stats collection started");
    }

    private void StopConsumer(string reason)
    {
        Flush(reason);

        var cts = _cts!;
        var task = _consumerTask;

        _cts = null;
        _consumer = null;
        _consumerTask = null;

        cts.Cancel();
        task?.ContinueWith(_ => cts.Dispose(), TaskScheduler.Default);
        _logger.LogInformation("Stats collection stopped");
    }

    public void Flush(string reason)
    {
        if (!IsRunning)
        {
            return;
        }

        _flushLock.Wait();

        try
        {
            using var database = dbProvider.Get();
            var snapshot = sessionStats.Snapshot(DateTime.Now);
            var col = database.GetCollection<DailyBucketDocument>(DailyBucketDocument.CollectionName);
            col.Upsert(snapshot);
            _logger.LogDebug("Stats flushed for {Date} (reason={Reason})", snapshot.Date.ToString("yyyy-MM-dd"), reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing stats to database");
        }
        finally
        {
            _flushLock.Release();
        }
    }

    private void LoadTodaysBucket()
    {
        try
        {
            using var database = dbProvider.Get();
            var today = DateTime.Now.Date;
            var col = database.GetCollection<DailyBucketDocument>(DailyBucketDocument.CollectionName);
            var existing = col.FindById(today);
            if (existing is not null)
            {
                sessionStats.LoadFrom(existing);
                _logger.LogDebug("Loaded existing stats for {Date}", today.ToString("yyyy-MM-dd"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading today's stats bucket from database");
        }
    }
}