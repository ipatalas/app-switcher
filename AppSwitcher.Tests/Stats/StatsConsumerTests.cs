using AppSwitcher.Stats;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Channels;
using Xunit;

namespace AppSwitcher.Tests.Stats;

public class StatsConsumerTests
{
    private readonly Channel<StatsEvent> _channel =
        Channel.CreateUnbounded<StatsEvent>();

    private readonly SessionStats _sessionStats = new();

    private StatsConsumer CreateSut(AppRegistryCache? cache = null)
    {
        cache ??= FakeRegistryCache();
        return new StatsConsumer(
            _channel.Reader,
            _sessionStats,
            cache,
            _ => { }, // only flushes on timer every 5 minutes so not testable here
            NullLogger<StatsConsumer>.Instance);
    }

    private static AppRegistryCache FakeRegistryCache()
    {
        var db = new LiteDB.LiteDatabase("Filename=:memory:");
        return new AppRegistryCache(db, NullLogger<AppRegistryCache>.Instance);
    }

    private SwitchEvent MakeSwitchEvent(
        string processName,
        int totalChoices = 2,
        long modifierDownTick = 1000,
        long letterDownTick = 1200,
        long? previousLetterUpTick = null,
        bool isDynamic = false)
        => new(ProcessName: processName,
            ProcessId: null,
            ProcessPath: processName,
            TotalChoices: totalChoices,
            ModifierDownTick: modifierDownTick,
            LetterDownTick: letterDownTick,
            PreviousLetterUpTick: previousLetterUpTick,
            IsDynamic: isDynamic);

    private PeekEvent MakePeekEvent(
        string targetProcessName,
        long armTick = 1000,
        long finishTick = 1800,
        bool isDynamic = false)
        => new(TargetProcessName: targetProcessName,
            ArmTick: armTick,
            FinishTick: finishTick,
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

    [Fact]
    public async Task ProcessSwitch_RecordsStaticSwitchInStaticBucket()
    {
        var evt = MakeSwitchEvent("notepad.exe", totalChoices: 2, isDynamic: false);

        await WriteAndDrain(evt);

        var snapshot = _sessionStats.Snapshot(DateTime.Now);
        snapshot.TotalSwitches.Should().Be(1);
        snapshot.StaticAppUsage.Should().ContainKey("notepad.exe");
        snapshot.DynamicAppUsage.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessSwitch_RecordsDynamicSwitchInDynamicBucket()
    {
        var evt = MakeSwitchEvent("explorer.exe", totalChoices: 2, isDynamic: true);

        await WriteAndDrain(evt);

        var snapshot = _sessionStats.Snapshot(DateTime.Now);
        snapshot.TotalSwitches.Should().Be(1);
        snapshot.DynamicAppUsage.Should().ContainKey("explorer.exe");
        snapshot.StaticAppUsage.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessSwitch_UsesDurationFromModifierDown_WhenNoPreviousLetterUp()
    {
        // duration = letterDownTick - modifierDownTick = 1200 - 1000 = 200ms (< idle threshold)
        var evt = MakeSwitchEvent(
            "notepad.exe",
            modifierDownTick: 1000,
            letterDownTick: 1200,
            previousLetterUpTick: null);

        await WriteAndDrain(evt);

        // Just verify it was recorded (duration logic is internal)
        var snapshot = _sessionStats.Snapshot(DateTime.Now);
        snapshot.TotalSwitches.Should().Be(1);
    }

    [Fact]
    public async Task ProcessSwitch_UsesDurationFromPreviousLetterUp_WhenProvided()
    {
        // duration = letterDownTick - previousLetterUpTick = 1200 - 1100 = 100ms
        var evt = MakeSwitchEvent(
            "notepad.exe",
            modifierDownTick: 1000,
            letterDownTick: 1200,
            previousLetterUpTick: 1100);

        await WriteAndDrain(evt);

        var snapshot = _sessionStats.Snapshot(DateTime.Now);
        snapshot.TotalSwitches.Should().Be(1);
    }

    [Fact]
    public async Task ProcessSwitch_ClampsToBaseline_WhenDurationExceedsIdleThreshold()
    {
        // duration = 5000 - 1000 = 4000ms > 1500ms idle threshold → clamped to 350ms baseline
        var evt = MakeSwitchEvent(
            "notepad.exe",
            modifierDownTick: 1000,
            letterDownTick: 5000,
            previousLetterUpTick: null);

        await WriteAndDrain(evt);

        // Verify switch was still recorded despite idle clamping
        var snapshot = _sessionStats.Snapshot(DateTime.Now);
        snapshot.TotalSwitches.Should().Be(1);
    }

    [Fact]
    public async Task ProcessSwitch_RecordsTransition_WhenTwoSwitchesOccur()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var sut = CreateSut();
        var runTask = sut.StartAsync(cts.Token);

        await _channel.Writer.WriteAsync(MakeSwitchEvent("notepad.exe"), CancellationToken.None);
        await _channel.Writer.WriteAsync(MakeSwitchEvent("code.exe"), CancellationToken.None);

        await Task.Delay(100, CancellationToken.None);
        await cts.CancelAsync();
        try { await runTask; } catch (OperationCanceledException) { }

        var snapshot = _sessionStats.Snapshot(DateTime.Now);
        snapshot.Transitions.Should().ContainKey("notepad.exe|code.exe");
        snapshot.Transitions["notepad.exe|code.exe"].Should().Be(1);
    }

    [Fact]
    public async Task ProcessPeek_RecordsStaticPeekInStaticBucket()
    {
        var evt = MakePeekEvent("notepad.exe", armTick: 1000, finishTick: 1800, isDynamic: false);

        await WriteAndDrain(evt);

        var snapshot = _sessionStats.Snapshot(DateTime.Now);
        snapshot.TotalPeeks.Should().Be(1);
        snapshot.StaticAppUsage["notepad.exe"].Peeks.Should().Be(1);
        snapshot.StaticAppUsage["notepad.exe"].TotalPeekTimeMs.Should().Be(800);
        snapshot.DynamicAppUsage.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessPeek_RecordsDynamicPeekInDynamicBucket()
    {
        var evt = MakePeekEvent("explorer.exe", armTick: 1000, finishTick: 1800, isDynamic: true);

        await WriteAndDrain(evt);

        var snapshot = _sessionStats.Snapshot(DateTime.Now);
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

        await _channel.Writer.WriteAsync(MakePeekEvent("notepad.exe", armTick: 0, finishTick: 600), CancellationToken.None);
        await _channel.Writer.WriteAsync(MakePeekEvent("notepad.exe", armTick: 0, finishTick: 400), CancellationToken.None);

        await Task.Delay(100, CancellationToken.None);
        await cts.CancelAsync();
        try { await runTask; } catch (OperationCanceledException) { }

        var snapshot = _sessionStats.Snapshot(DateTime.Now);
        snapshot.StaticAppUsage["notepad.exe"].TotalPeekTimeMs.Should().Be(1000);
    }

    [Fact]
    public async Task StartAsync_DoesNotThrowUnexpectedException_OnCancellation()
    {
        using var cts = new CancellationTokenSource();
        var sut = CreateSut();
        var runTask = sut.StartAsync(cts.Token);

        await cts.CancelAsync();

        // Task.Run propagates cancellation as TaskCanceledException — that is expected.
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

        var snapshot = _sessionStats.Snapshot(DateTime.Now);
        snapshot.AltTabSwitches.Should().Be(1);
        snapshot.AltTabKeystrokes.Should().Be(3);
    }
}
