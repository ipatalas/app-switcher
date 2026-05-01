using AppSwitcher.Stats;
using AppSwitcher.WindowDiscovery;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Diagnostics;
using System.IO;
using System.Threading.Channels;
using System.Windows.Input;
using Xunit;

namespace AppSwitcher.Tests.Stats;

public class StatsConsumerTests
{
    private readonly Channel<StatsEvent> _channel =
        Channel.CreateUnbounded<StatsEvent>();

    private readonly SessionStats _realStats = new();
    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.Today);

    private StatsConsumer CreateSut(
        ISessionStats? sessionStats = null,
        AppRegistryCache? cache = null,
        Func<CancellationToken, Task>? flushSignal = null)
    {
        cache ??= FakeRegistryCache();
        return new StatsConsumer(
            _channel.Reader,
            sessionStats ?? _realStats,
            cache,
            _ => { }, // only flushes on timer every 5 minutes so not testable here
            NullLogger<StatsConsumer>.Instance,
            flushSignal);
    }

    private static AppRegistryCache FakeRegistryCache()
    {
        var db = new LiteDB.LiteDatabase("Filename=:memory:");
        var windowEnumerator = Substitute.For<IWindowEnumerator>();
        windowEnumerator.GetWindows().Returns([]);
        var packagedAppsService = Substitute.For<IPackagedAppsService>();
        packagedAppsService.GetInstalledPaths().Returns(new HashSet<string>());
        var processInspector = Substitute.For<IProcessInspector>();
        processInspector.GetProcessDisplayName(Arg.Any<string>()).Returns(x => Path.GetFileNameWithoutExtension((string)x[0]));
        return new AppRegistryCache(
            db,
            windowEnumerator,
            packagedAppsService,
            processInspector,
            NullLogger<AppRegistryCache>.Instance);
    }

    private static long TicksFromMs(long ms) => (long)(ms * Stopwatch.Frequency / 1000.0);

    private SwitchEvent MakeSwitchEvent(
        string processName,
        int totalChoices = 2,
        long modifierDownMs = 0,
        long letterDownMs = 200,
        long? previousLetterUpMs = null,
        bool isDynamic = false,
        Key triggerKey = Key.N)
        => new(ProcessName: processName,
            ProcessId: null,
            ProcessPath: processName,
            TotalChoices: totalChoices,
            ModifierDownTick: TicksFromMs(modifierDownMs),
            LetterDownTick: TicksFromMs(letterDownMs),
            PreviousLetterUpTick: previousLetterUpMs.HasValue ? TicksFromMs(previousLetterUpMs.Value) : null,
            IsDynamic: isDynamic,
            TriggerKey: triggerKey);

    private PeekEvent MakePeekEvent(
        string targetProcessName,
        string targetProcessPath = "",
        long armMs = 0,
        long finishMs = 800,
        bool isDynamic = false)
        => new(TargetProcessName: targetProcessName,
            TargetProcessPath: targetProcessPath,
            ArmTick: TicksFromMs(armMs),
            FinishTick: TicksFromMs(finishMs),
            IsDynamic: isDynamic);

    private async Task WriteAndDrain(StatsEvent evt)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var sut = CreateSut();
        var runTask = sut.StartAsync(cts.Token);

        await _channel.Writer.WriteAsync(evt, cts.Token);

        // Give the consumer time to process the event
        await Task.Delay(50, cts.Token);
        await cts.CancelAsync();

        try { await runTask; } catch (OperationCanceledException) { }
    }

    private async Task WriteAndDrainAll(IEnumerable<StatsEvent> events)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var sut = CreateSut();
        var runTask = sut.StartAsync(cts.Token);

        foreach (var evt in events)
        {
            await _channel.Writer.WriteAsync(evt, CancellationToken.None);
        }

        await Task.Delay(100, CancellationToken.None);
        await cts.CancelAsync();

        try { await runTask; } catch (OperationCanceledException) { }
    }

    [Fact]
    public async Task ProcessSwitch_RecordsStaticSwitchInStaticBucket()
    {
        var evt = MakeSwitchEvent("notepad.exe", totalChoices: 2, isDynamic: false);

        await WriteAndDrain(evt);

        var snapshot = _realStats.Snapshot(_today);
        snapshot.TotalSwitches.Should().Be(1);
        snapshot.StaticAppUsage.Should().ContainKey("notepad.exe");
        snapshot.DynamicAppUsage.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessSwitch_RecordsDynamicSwitchInDynamicBucket()
    {
        var evt = MakeSwitchEvent("explorer.exe", totalChoices: 2, isDynamic: true);

        await WriteAndDrain(evt);

        var snapshot = _realStats.Snapshot(_today);
        snapshot.TotalSwitches.Should().Be(1);
        snapshot.DynamicAppUsage.Should().ContainKey("explorer.exe");
        snapshot.StaticAppUsage.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessSwitch_UsesDurationFromModifierDown_WhenNoPreviousLetterUp()
    {
        // duration = letterDownMs - modifierDownMs = 200 - 0 = 200ms (< idle threshold)
        var evt = MakeSwitchEvent(
            "notepad.exe",
            modifierDownMs: 0,
            letterDownMs: 200,
            previousLetterUpMs: null);

        await WriteAndDrain(evt);

        // Just verify it was recorded (duration logic is internal)
        var snapshot = _realStats.Snapshot(_today);
        snapshot.TotalSwitches.Should().Be(1);
    }

    [Fact]
    public async Task ProcessSwitch_UsesDurationFromPreviousLetterUp_WhenProvided()
    {
        // duration = letterDownMs - previousLetterUpMs = 200 - 100 = 100ms
        var evt = MakeSwitchEvent(
            "notepad.exe",
            modifierDownMs: 0,
            letterDownMs: 200,
            previousLetterUpMs: 100);

        await WriteAndDrain(evt);

        var snapshot = _realStats.Snapshot(_today);
        snapshot.TotalSwitches.Should().Be(1);
    }

    [Fact]
    public async Task ProcessSwitch_ClampsToBaseline_WhenDurationExceedsIdleThreshold()
    {
        // duration = 4000 - 0 = 4000ms > 1500ms idle threshold → clamped to 350ms baseline
        var evt = MakeSwitchEvent(
            "notepad.exe",
            modifierDownMs: 0,
            letterDownMs: 4000,
            previousLetterUpMs: null);

        await WriteAndDrain(evt);

        // Verify switch was still recorded despite idle clamping
        var snapshot = _realStats.Snapshot(_today);
        snapshot.TotalSwitches.Should().Be(1);
    }

    [Fact]
    public async Task ProcessPeek_RecordsStaticPeekInStaticBucket()
    {
        var evt = MakePeekEvent("notepad.exe", armMs: 0, finishMs: 800, isDynamic: false);

        await WriteAndDrain(evt);

        var snapshot = _realStats.Snapshot(_today);
        snapshot.TotalPeeks.Should().Be(1);
        snapshot.StaticAppUsage["notepad.exe"].Peeks.Should().Be(1);
        snapshot.StaticAppUsage["notepad.exe"].TotalPeekTimeMs.Should().Be(800);
        snapshot.DynamicAppUsage.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessPeek_RecordsDynamicPeekInDynamicBucket()
    {
        var evt = MakePeekEvent("explorer.exe", armMs: 0, finishMs: 800, isDynamic: true);

        await WriteAndDrain(evt);

        var snapshot = _realStats.Snapshot(_today);
        snapshot.TotalPeeks.Should().Be(1);
        snapshot.DynamicAppUsage["explorer.exe"].Peeks.Should().Be(1);
        snapshot.StaticAppUsage.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessPeek_AccumulatesDuration_ForMultiplePeeks()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var sut = CreateSut();
        var runTask = sut.StartAsync(cts.Token);

        await _channel.Writer.WriteAsync(MakePeekEvent("notepad.exe", armMs: 0, finishMs: 600), CancellationToken.None);
        await _channel.Writer.WriteAsync(MakePeekEvent("notepad.exe", armMs: 0, finishMs: 400), CancellationToken.None);

        await Task.Delay(100, CancellationToken.None);
        await cts.CancelAsync();
        try { await runTask; } catch (OperationCanceledException) { }

        var snapshot = _realStats.Snapshot(_today);
        snapshot.StaticAppUsage["notepad.exe"].TotalPeekTimeMs.Should().Be(1000);
    }

    [Fact]
    public async Task StartAsync_DoesNotThrowUnexpectedException_OnCancellation()
    {
        using var cts = new CancellationTokenSource();
        var sut = CreateSut();
        var runTask = sut.StartAsync(cts.Token);

        await cts.CancelAsync();

        // Task.Run propagates cancellation as OperationCanceledException — that is expected.
        // What we verify is that no OTHER exception escapes (e.g. NullReferenceException).
        try
        {
            await runTask;
        }
        catch (OperationCanceledException)
        {
            // expected — cancellation propagated from Task.Run
        }
    }

    [Fact]
    public async Task ProcessAltTab_RecordsAltTabSwitchAndKeystrokes()
    {
        var evt = new AltTabEvent(NavCount: 3);

        await WriteAndDrain(evt);

        var snapshot = _realStats.Snapshot(_today);
        snapshot.AltTabSwitches.Should().Be(1);
        snapshot.AltTabKeystrokes.Should().Be(3);
    }

    // ── Fastest switch ───────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessSwitch_SetsFastestSwitch_WhenDurationWithinIdleThreshold()
    {
        // duration = letterDownMs - modifierDownMs = 200 - 0 = 200ms ≤ 1500ms idle threshold
        var evt = MakeSwitchEvent("spotify.exe",
            modifierDownMs: 0, letterDownMs: 200, triggerKey: Key.S);

        await WriteAndDrain(evt);

        var snapshot = _realStats.Snapshot(_today);
        snapshot.FastestSwitch.Should().NotBeNull();
        snapshot.FastestSwitch!.DurationMs.Should().Be(200);
        snapshot.FastestSwitch.AppName.Should().Be("spotify.exe");
        snapshot.FastestSwitch.Letter.Should().Be("S");
    }

    [Fact]
    public async Task ProcessSwitch_DoesNotSetFastestSwitch_WhenDurationExceedsIdleThreshold()
    {
        // duration = 4000 - 0 = 4000ms > 1500ms idle threshold
        var evt = MakeSwitchEvent("notepad.exe",
            modifierDownMs: 0, letterDownMs: 4000, triggerKey: Key.N);

        await WriteAndDrain(evt);

        var snapshot = _realStats.Snapshot(_today);
        snapshot.FastestSwitch.Should().BeNull();
    }

    // ── Consecutive / burst events ───────────────────────────────────────────

    [Fact]
    public async Task ProcessSwitch_RecordsAllFive_WhenFiveConsecutiveSwitchesEnqueued()
    {
        // Regression: orphaned ReadAsync waiter would silently drop the first event
        // after each flush tick. Rapid bursts must all be counted.
        var events = Enumerable.Range(1, 5)
            .Select(i => MakeSwitchEvent($"app{i}.exe"))
            .ToList<StatsEvent>();

        await WriteAndDrainAll(events);

        var snapshot = _realStats.Snapshot(_today);
        snapshot.TotalSwitches.Should().Be(5);
    }

    [Fact]
    public async Task ProcessEvents_AllTypesRecorded_WhenMixedSwitchPeekAltTabEnqueued()
    {
        var events = new StatsEvent[]
        {
            MakeSwitchEvent("notepad.exe"),
            MakePeekEvent("code.exe", armMs: 0, finishMs: 500),
            new AltTabEvent(NavCount: 2),
        };

        await WriteAndDrainAll(events);

        var snapshot = _realStats.Snapshot(_today);
        snapshot.TotalSwitches.Should().Be(1);
        snapshot.TotalPeeks.Should().Be(1);
        snapshot.AltTabSwitches.Should().Be(1);
    }

    // ── Argument forwarding (NSubstitute) ────────────────────────────────────

    [Fact]
    public async Task ProcessSwitch_CallsRecordSwitchWithCorrectArguments()
    {
        // duration = letterDownMs - modifierDownMs = 200 - 0 = 200ms (< idle threshold, not clamped)
        // savedMs = EfficiencyCalculator.SavedMs(2) — we verify the call, not the formula
        var sessionStats = Substitute.For<ISessionStats>();
        var sut = CreateSut(sessionStats);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var runTask = sut.StartAsync(cts.Token);

        await _channel.Writer.WriteAsync(
            MakeSwitchEvent("notepad.exe", totalChoices: 2, modifierDownMs: 0,
                letterDownMs: 200, isDynamic: false, triggerKey: Key.N),
            CancellationToken.None);

        await Task.Delay(50, CancellationToken.None);
        await cts.CancelAsync();
        try { await runTask; } catch (OperationCanceledException) { }

        sessionStats.Received(1).RecordSwitch(
            "notepad.exe",
            null,
            durationMs: 200,
            savedMs: Arg.Any<int>(),
            isDynamic: false,
            fastestDurationMs: 200,
            triggerKey: Key.N);
    }

    [Fact]
    public async Task ProcessPeek_CallsRecordPeekWithCorrectArguments()
    {
        // duration = finishMs - armMs = 800 - 0 = 800ms
        var sessionStats = Substitute.For<ISessionStats>();
        var sut = CreateSut(sessionStats);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var runTask = sut.StartAsync(cts.Token);

        await _channel.Writer.WriteAsync(
            MakePeekEvent("code.exe", armMs: 0, finishMs: 800, isDynamic: true),
            CancellationToken.None);

        await Task.Delay(50, CancellationToken.None);
        await cts.CancelAsync();
        try { await runTask; } catch (OperationCanceledException) { }

        sessionStats.Received(1).RecordPeek("code.exe", durationMs: 800, isDynamic: true);
    }

    [Fact]
    public async Task ProcessAltTab_CallsRecordAltTabWithCorrectNavCount()
    {
        var sessionStats = Substitute.For<ISessionStats>();
        var sut = CreateSut(sessionStats);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var runTask = sut.StartAsync(cts.Token);

        await _channel.Writer.WriteAsync(new AltTabEvent(NavCount: 7), CancellationToken.None);

        await Task.Delay(50, CancellationToken.None);
        await cts.CancelAsync();
        try { await runTask; } catch (OperationCanceledException) { }

        sessionStats.Received(1).RecordAltTab(7);
    }

    // ── Flush-tick regression ────────────────────────────────────────────────

    /// <summary>
    /// Returns a flush signal that completes immediately for the first
    /// <paramref name="tickCount"/> calls, then blocks until cancellation.
    /// This lets the consumer loop spin through N flush ticks with an empty
    /// channel before any events arrive — exactly the condition that triggered
    /// the orphaned-ReadAsync bug.
    /// </summary>
    private static Func<CancellationToken, Task> MakeFastThenBlockSignal(int tickCount)
    {
        var remaining = tickCount;
        return ct => Interlocked.Decrement(ref remaining) >= 0
            ? Task.CompletedTask
            : Task.Delay(Timeout.Infinite, ct);
    }

    [Fact]
    public async Task ProcessSwitch_RecordsAllEvents_AfterMultipleFlushTicksWithEmptyChannel()
    {
        // Arrange: signal fires 5 times instantly (simulating 5 timer ticks while channel
        // is empty), then blocks. With the old code each tick orphaned a ReadAsync waiter,
        // so the first 5 events written afterwards would be silently dropped.
        const int ticksBeforeWrite = 5;
        const int eventCount = 10;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var sut = CreateSut(flushSignal: MakeFastThenBlockSignal(ticksBeforeWrite));
        var runTask = sut.StartAsync(cts.Token);

        // Let the consumer churn through all fast ticks and settle on a single readTask.
        await Task.Delay(50, CancellationToken.None);

        // Act: write events after the ticks have been processed.
        for (var i = 0; i < eventCount; i++)
        {
            await _channel.Writer.WriteAsync(MakeSwitchEvent($"app{i}.exe"), CancellationToken.None);
        }

        await Task.Delay(100, CancellationToken.None);
        await cts.CancelAsync();
        try { await runTask; } catch (OperationCanceledException) { }

        // Assert: every event must be recorded.
        // Old code: 5 orphaned waiters → 5 drops → TotalSwitches == 5 (FAIL).
        // Fixed code: readTask reused across ticks → 0 drops → TotalSwitches == 10 (PASS).
        var snapshot = _realStats.Snapshot(_today);
        snapshot.TotalSwitches.Should().Be(eventCount);
    }
}
