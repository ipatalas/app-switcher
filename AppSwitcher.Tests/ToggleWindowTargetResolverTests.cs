using AppSwitcher;
using AwesomeAssertions;
using Windows.Win32.Foundation;
using Xunit;

namespace AppSwitcher.Tests;

public class ToggleWindowTargetResolverTests
{
    private static HWND H(int value) => new((nint)value);

    [Fact]
    public void Resolve_ReturnsNull_WhenNoMatchingWindows()
    {
        var result = ToggleWindowTargetResolver.Resolve([], H(1), H(2));

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_ActivatesRemembered_WhenSiblingIsForeground()
    {
        var remembered = H(10);
        var sibling = H(20);

        var result = ToggleWindowTargetResolver.Resolve(
            [remembered, sibling],
            currentHandle: sibling,
            rememberedHandle: remembered);

        result.Should().Be(new ToggleWindowTargetResolver.Result(remembered, ToggleWindowAction.Activate));
    }

    [Fact]
    public void Resolve_HidesRemembered_WhenForegroundIsRemembered()
    {
        var remembered = H(10);
        var sibling = H(20);

        var result = ToggleWindowTargetResolver.Resolve(
            [remembered, sibling],
            currentHandle: remembered,
            rememberedHandle: remembered);

        result.Should().Be(new ToggleWindowTargetResolver.Result(remembered, ToggleWindowAction.Hide));
    }

    [Fact]
    public void Resolve_AfterHide_RestoresRemembered_WhenSiblingTookFocus()
    {
        var remembered = H(10);
        var sibling = H(20);
        HWND[] matching = [remembered, sibling];

        var hide = ToggleWindowTargetResolver.Resolve(matching, currentHandle: remembered, rememberedHandle: remembered);

        hide.Should().Be(new ToggleWindowTargetResolver.Result(remembered, ToggleWindowAction.Hide));

        var restore = ToggleWindowTargetResolver.Resolve(matching, currentHandle: sibling, rememberedHandle: remembered);

        restore.Should().Be(new ToggleWindowTargetResolver.Result(remembered, ToggleWindowAction.Activate));
    }

    [Fact]
    public void Resolve_ActivatesRemembered_WhenForegroundIsOtherApp()
    {
        var w1 = H(10);
        var w2 = H(20);
        var other = H(99);

        var result = ToggleWindowTargetResolver.Resolve([w1, w2], currentHandle: other, rememberedHandle: w2);

        result.Should().Be(new ToggleWindowTargetResolver.Result(w2, ToggleWindowAction.Activate));
    }

    [Fact]
    public void Resolve_ActivatesFirst_WhenNoRememberedAndForegroundOtherApp()
    {
        var w1 = H(10);
        var w2 = H(20);
        var other = H(99);

        var result = ToggleWindowTargetResolver.Resolve([w1, w2], currentHandle: other, rememberedHandle: null);

        result.Should().Be(new ToggleWindowTargetResolver.Result(w1, ToggleWindowAction.Activate));
    }

    [Fact]
    public void Resolve_ActivatesFirst_WhenRememberedMissingFromMatching()
    {
        var w1 = H(10);
        var w2 = H(20);
        var closed = H(30);
        var other = H(99);

        var result = ToggleWindowTargetResolver.Resolve([w1, w2], currentHandle: other, rememberedHandle: closed);

        result.Should().Be(new ToggleWindowTargetResolver.Result(w1, ToggleWindowAction.Activate));
    }

    [Fact]
    public void Resolve_UsesCurrent_WhenRememberedMissingAndForegroundMatches()
    {
        var w1 = H(10);
        var w2 = H(20);
        var closed = H(30);

        var result = ToggleWindowTargetResolver.Resolve([w1, w2], currentHandle: w2, rememberedHandle: closed);

        result.Should().Be(new ToggleWindowTargetResolver.Result(w2, ToggleWindowAction.Hide));
    }

    [Fact]
    public void Resolve_UsesRemembered_WhenCurrentHandleUnknown()
    {
        var w1 = H(10);
        var w2 = H(20);

        var result = ToggleWindowTargetResolver.Resolve([w1, w2], currentHandle: null, rememberedHandle: w2);

        result.Should().Be(new ToggleWindowTargetResolver.Result(w2, ToggleWindowAction.Activate));
    }

    [Fact]
    public void Resolve_HidesFirst_WhenForegroundIsFirstMatching()
    {
        var w1 = H(10);
        var w2 = H(20);

        var result = ToggleWindowTargetResolver.Resolve([w1, w2], currentHandle: w1, rememberedHandle: null);

        result.Should().Be(new ToggleWindowTargetResolver.Result(w1, ToggleWindowAction.Hide));
    }

    [Fact]
    public void Resolve_Activates_WhenForegroundMatchingWindowIsMinimized()
    {
        var weixin = H(0x1073a);

        var result = ToggleWindowTargetResolver.Resolve(
            [weixin],
            currentHandle: weixin,
            rememberedHandle: weixin,
            currentIsMinimized: true);

        result.Should().Be(new ToggleWindowTargetResolver.Result(weixin, ToggleWindowAction.Activate));
    }
}
