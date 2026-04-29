using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Channels;

namespace AppSwitcher.Stats;

internal class StatsConsumer(
    ChannelReader<StatsEvent> reader,
    ISessionStats sessionStats,
    AppRegistryCache registryCache,
    Action<string> flush,
    ILogger<StatsConsumer> logger,
    Func<CancellationToken, Task>? flushSignal = null)
{
    private const int IdleThresholdMs = 1500;
    private const int BaselineDurationMs = 350;
    private const int FlushIntervalMinutes = 5;

    private string? _previousProcessName;

    public Task StartAsync(CancellationToken ct)
        => Task.Run(() => RunAsync(ct), ct);

    private async Task RunAsync(CancellationToken ct)
    {
        PeriodicTimer? timer = null;
        var nextFlush = flushSignal;

        if (nextFlush is null)
        {
            timer = new PeriodicTimer(TimeSpan.FromMinutes(FlushIntervalMinutes));
            nextFlush = ct2 => timer.WaitForNextTickAsync(ct2).AsTask();
        }

        using (timer)
        {
            try
            {
                var ingestionTask = ProcessEventsAsync();
                var flushingTask = PeriodicFlushAsync();

                await Task.WhenAll(ingestionTask, flushingTask);
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in stats consumer loop");
            }
        }

        async Task ProcessEventsAsync()
        {
            await foreach (var statsEvent in reader.ReadAllAsync(ct))
            {
                ProcessEvent(statsEvent);
            }
        }

        async Task PeriodicFlushAsync()
        {
            while (!ct.IsCancellationRequested)
            {
                await nextFlush(ct);
                TryFlush();
            }
        }
    }

    private void TryFlush()
    {
        try
        {
            flush("5 min timer ticked");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during periodic stats flush");
        }
    }

    private void ProcessEvent(StatsEvent statsEvent)
    {
        switch (statsEvent)
        {
            case SwitchEvent e:
                ProcessSwitch(e);
                break;
            case PeekEvent e:
                ProcessPeek(e);
                break;
            case AltTabEvent e:
                ProcessAltTab(e);
                break;
        }
    }

    private void ProcessSwitch(SwitchEvent e)
    {
        registryCache.TryAdd(e.ProcessName, e.ProcessPath);

        var rawDurationMs = e.PreviousLetterUpTick.HasValue // is subsequent switch while holding modifier
            ? (int)Stopwatch.GetElapsedTime(e.PreviousLetterUpTick.Value, e.LetterDownTick).TotalMilliseconds
            : (int)Stopwatch.GetElapsedTime(e.ModifierDownTick, e.LetterDownTick).TotalMilliseconds;

        var durationMs = rawDurationMs > IdleThresholdMs ? BaselineDurationMs : rawDurationMs;

        var savedMs = EfficiencyCalculator.SavedMs(e.TotalChoices);

        logger.LogDebug(
            "Switch to {ProcessName}: duration={DurationMs}ms, saved={SavedMs}ms, choices={Choices}, dynamic={IsDynamic}",
            e.ProcessName, durationMs, savedMs, e.TotalChoices, e.IsDynamic);

        var fastestDurationMs = rawDurationMs <= IdleThresholdMs ? rawDurationMs : (int?)null;

        sessionStats.RecordSwitch(e.ProcessName, _previousProcessName, durationMs, savedMs, e.IsDynamic,
            fastestDurationMs: fastestDurationMs, triggerKey: e.TriggerKey);
        _previousProcessName = e.ProcessName;
    }

    private void ProcessPeek(PeekEvent e)
    {
        registryCache.TryAdd(e.TargetProcessName, e.TargetProcessPath);

        var durationMs = (int)Stopwatch.GetElapsedTime(e.ArmTick, e.FinishTick).TotalMilliseconds;

        logger.LogDebug(
            "Peek at {ProcessName}: duration={DurationMs}ms, dynamic={IsDynamic}",
            e.TargetProcessName, durationMs, e.IsDynamic);

        sessionStats.RecordPeek(e.TargetProcessName, durationMs, e.IsDynamic);
    }

    private void ProcessAltTab(AltTabEvent e)
    {
        logger.LogDebug("AltTab: navCount={NavCount}", e.NavCount);
        sessionStats.RecordAltTab(e.NavCount);
    }
}