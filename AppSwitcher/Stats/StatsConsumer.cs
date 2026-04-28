using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace AppSwitcher.Stats;

internal class StatsConsumer(
    ChannelReader<StatsEvent> reader,
    SessionStats sessionStats,
    AppRegistryCache registryCache,
    Action<string> flush,
    ILogger<StatsConsumer> logger)
{
    private const int IdleThresholdMs = 1500;
    private const int BaselineDurationMs = 350;
    private const int FlushIntervalMinutes = 5;

    private string? _previousProcessName;

    public Task StartAsync(CancellationToken ct)
        => Task.Run(() => RunAsync(ct), ct);

    private async Task RunAsync(CancellationToken ct)
    {
        using var flushTimer = new PeriodicTimer(TimeSpan.FromMinutes(FlushIntervalMinutes));
        var flushTask = WaitForFlushTimer(flushTimer, ct);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var readTask = reader.ReadAsync(ct).AsTask();
                var completed = await Task.WhenAny(readTask, flushTask);

                if (completed == flushTask) // timer ticked
                {
                    TryFlush();
                    flushTask = WaitForFlushTimer(flushTimer, ct);
                    continue;
                }

                var statsEvent = await readTask;
                ProcessEvent(statsEvent);
            }
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

    private static async Task WaitForFlushTimer(PeriodicTimer timer, CancellationToken ct)
    {
        await timer.WaitForNextTickAsync(ct);
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
            ? (int)(e.LetterDownTick - e.PreviousLetterUpTick.Value)
            : (int)(e.LetterDownTick - e.ModifierDownTick);

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

        var durationMs = (int)(e.FinishTick - e.ArmTick);

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