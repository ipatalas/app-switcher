using AppSwitcher.Input;
using AppSwitcher.WindowDiscovery;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Windows.Win32.UI.WindowsAndMessaging;
using Xunit;

namespace AppSwitcher.Tests.Input;

public class PeekerTests : IDisposable
{
    private readonly Peeker _sut = new(NullLogger<Peeker>.Instance);

    public void Dispose() => _sut.Dispose();

    private static ApplicationWindow MakeWindow(uint processId = 1, SHOW_WINDOW_CMD state = SHOW_WINDOW_CMD.SW_NORMAL) =>
        new(
            Handle: default,
            Title: "Test",
            ProcessId: processId,
            ProcessImagePath: "test.exe",
            State: state,
            Position: default,
            Size: new Size(100, 100),
            Style: default,
            StyleEx: WindowStyleEx.WS_EX_APPWINDOW,
            IsCloaked: false,
            NeedsElevation: false);

    private static AppSwitchResult MakeResult(uint processId = 1, SHOW_WINDOW_CMD state = SHOW_WINDOW_CMD.SW_NORMAL) =>
        new(
            ProcessId: processId,
            ProcessPath: "test.exe",
            Handle: default,
            State: state,
            NeedsElevation: false,
            WasStarted: false);

    [Fact]
    public void TryFinish_ReturnsFalse_WhenNeverArmed()
    {
        var result = _sut.TryFinish(out _);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryFinish_ReturnsFalse_WhenReleasedBeforeThreshold()
    {
        _sut.Arm(MakeWindow(processId: 1), MakeResult(processId: 2));

        await Task.Delay(Peeker.PeekThresholdMs / 2);
        var result = _sut.TryFinish(out _);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryFinish_ReturnsTrue_AndReturnsPreviousWindow_WhenReleasedAfterThreshold()
    {
        var previousWindow = MakeWindow(processId: 1);
        _sut.Arm(previousWindow, MakeResult(processId: 2));

        await Task.Delay(Peeker.PeekThresholdMs + 100);
        var result = _sut.TryFinish(out var peekResult);

        result.Should().BeTrue();
        peekResult!.PreviousWindow.Should().BeSameAs(previousWindow);
    }

    [Fact]
    public void TryFinish_ReturnsFalse_AfterCancel()
    {
        _sut.Arm(MakeWindow(processId: 1), MakeResult(processId: 2));
        _sut.Cancel();

        var result = _sut.TryFinish(out _);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryFinish_ReturnsFalse_WhenCalledTwice()
    {
        _sut.Arm(MakeWindow(processId: 1), MakeResult(processId: 2));
        await Task.Delay(Peeker.PeekThresholdMs + 100);

        _sut.TryFinish(out _);
        var result = _sut.TryFinish(out _);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Cancel_PreventsPeekFromFiring_EvenAfterThreshold()
    {
        _sut.Arm(MakeWindow(processId: 1), MakeResult(processId: 2));
        _sut.Cancel();

        await Task.Delay(Peeker.PeekThresholdMs + 100);
        var result = _sut.TryFinish(out _);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryFinish_ReturnsTargetWasMinimized_True_WhenTargetWasMinimized()
    {
        var minimizedTarget = MakeResult(processId: 2, state: SHOW_WINDOW_CMD.SW_SHOWMINIMIZED);
        _sut.Arm(MakeWindow(processId: 1), minimizedTarget);

        await Task.Delay(Peeker.PeekThresholdMs + 100);
        _sut.TryFinish(out var peekResult);

        peekResult!.TargetWasMinimized.Should().BeTrue();
    }

    [Fact]
    public async Task TryFinish_ReturnsTargetWasMinimized_False_WhenTargetWasNotMinimized()
    {
        var normalTarget = MakeResult(processId: 2, state: SHOW_WINDOW_CMD.SW_NORMAL);
        _sut.Arm(MakeWindow(processId: 1), normalTarget);

        await Task.Delay(Peeker.PeekThresholdMs + 100);
        _sut.TryFinish(out var peekResult);

        peekResult!.TargetWasMinimized.Should().BeFalse();
    }
}
