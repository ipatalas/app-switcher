using AppSwitcher.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using AwesomeAssertions;

namespace AppSwitcher.Tests.Utils;

public class ModifierIdleTimerTests : IDisposable
{
    private readonly ModifierIdleTimer _sut = new(NullLogger<ModifierIdleTimer>.Instance);

    public void Dispose() => _sut.Dispose();

    [Fact]
    public void Restart_DoesNotFire_WhenDisabled()
    {
        var fired = false;
        _sut.Configure(() => fired = true, timeoutMs: 0);

        _sut.Restart();

        fired.Should().BeFalse();
    }

    [Fact]
    public void Cancel_DoesNotThrow_WhenNoTimerIsRunning()
    {
        var act = _sut.Cancel;

        act.Should().NotThrow();
    }

    [Fact]
    public void Cancel_DoesNotThrow_WhenCalledAfterRestart()
    {
        _sut.Configure(() => { }, timeoutMs: 60_000);
        _sut.Restart();

        var act = _sut.Cancel;

        act.Should().NotThrow();
    }

    [Fact]
    public void Cancel_PreventsCallbackFromFiring()
    {
        var fired = false;
        _sut.Configure(() => fired = true, timeoutMs: 50);
        _sut.Restart();
        _sut.Cancel();

        Thread.Sleep(100);

        fired.Should().BeFalse();
    }

    [Fact]
    public async Task Restart_FiresCallback_AfterTimeout()
    {
        var tcs = new TaskCompletionSource<bool>();
        _sut.Configure(() => tcs.TrySetResult(true), timeoutMs: 50);

        _sut.Restart();

        var fired = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(1))) == tcs.Task;
        fired.Should().BeTrue("callback should fire within 1s");
    }

    [Fact]
    public void Restart_ResetsTimer_WhenCalledAgainBeforeExpiry()
    {
        var fireCount = 0;
        _sut.Configure(() => Interlocked.Increment(ref fireCount), timeoutMs: 80);

        _sut.Restart();
        Thread.Sleep(40);
        _sut.Restart();
        Thread.Sleep(40);
        _sut.Restart();
        Thread.Sleep(120); // now 100ms from second call: definitely fired once

        fireCount.Should().Be(1);
    }

    [Fact]
    public void Configure_CancelsRunningTimer()
    {
        var fired = false;
        _sut.Configure(() => fired = true, timeoutMs: 50);
        _sut.Restart();

        // Reconfigure before it fires — the old timer must be cancelled
        _sut.Configure(() => { }, timeoutMs: 60_000);

        Thread.Sleep(150);

        fired.Should().BeFalse();
    }

    [Fact]
    public void Dispose_DoesNotThrow_WhenCalledMultipleTimes()
    {
        _sut.Configure(() => { }, timeoutMs: 60_000);
        _sut.Restart();

        var act = () =>
        {
            _sut.Dispose();
            _sut.Dispose();
        };

        act.Should().NotThrow();
    }
}