using AppSwitcher.Input;
using AwesomeAssertions;
using System.Windows.Input;
using Xunit;
using static AppSwitcher.Input.KeyTransition;

namespace AppSwitcher.Tests.Input;

public class KeyStateMachineTests
{
    private readonly KeyStateMachine _sut = new();

    public static TheoryData<Key, bool> ModifiersSideEffects => new()
    {
        { Key.Apps, true },
        { Key.LWin, true },
        { Key.RWin, true },
        { Key.LeftAlt, true },
        { Key.RightAlt, true },
        { Key.Capital, true },

        { Key.LeftCtrl, false },
        { Key.RightCtrl, false },
        { Key.LeftShift, false },
        { Key.RightShift, false },
    };

    public KeyStateMachineTests()
    {
        _sut.Configure(Key.Apps);
    }

    // ── ProcessKeyDown: modifier key ────────────────────────────────────────

    [Fact]
    public void ProcessKeyDown_ReturnsModifierPressedFirstPress_OnFirstModifierPress()
    {
        var result = _sut.ProcessKeyDown(Key.Apps);

        result.Should().BeOfType<ModifierPressed>()
            .Which.IsFirstPress.Should().BeTrue();
        _sut.IsModifierHeld.Should().BeTrue();
    }

    [Fact]
    public void ProcessKeyDown_ReturnsModifierPressedNotFirstPress_OnModifierKeyRepeat()
    {
        _sut.ProcessKeyDown(Key.Apps);

        var result = _sut.ProcessKeyDown(Key.Apps);

        result.Should().BeOfType<ModifierPressed>()
            .Which.IsFirstPress.Should().BeFalse();
        _sut.IsModifierHeld.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(ModifiersSideEffects))]
    public void ProcessKeyDown_ReturnsModifierPressedWithSideEffect_OnModifier(Key modifierKey, bool expectedHasSideEffect)
    {
        _sut.Configure(modifierKey);
        var result = _sut.ProcessKeyDown(modifierKey);

        result.Should().BeOfType<ModifierPressed>()
            .Which.HasSideEffect.Should().Be(expectedHasSideEffect);
    }

    [Fact]
    public void ProcessKeyDown_ReturnsNoOp_WhenModifierNotHeldAndNonModifierPressed()
    {
        var result = _sut.ProcessKeyDown(Key.A);

        result.Should().BeOfType<NoOp>();
        _sut.IsModifierHeld.Should().BeFalse();
    }

    // ── ProcessKeyUp: modifier key ──────────────────────────────────────────

    [Fact]
    public void ProcessKeyUp_ReturnsModifierReleasedClean_WhenNoLettersPressed()
    {
        _sut.ProcessKeyDown(Key.Apps);

        var result = _sut.ProcessKeyUp(Key.Apps);

        result.Should().BeOfType<ModifierReleasedClean>()
            .Which.HeldDurationMs.Should().BeGreaterThanOrEqualTo(0);
        _sut.IsModifierHeld.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(ModifiersSideEffects))]
    public void ProcessKeyUp_ReturnsModifierReleaseCleanWithSideEffect_WhenModifierAlonePressedAndReleased(Key modifierKey, bool expectedHasSideEffect)
    {
        _sut.Configure(modifierKey);
        _sut.ProcessKeyDown(modifierKey);

        var result = _sut.ProcessKeyUp(modifierKey);

        result.Should().BeOfType<ModifierReleasedClean>()
            .Which.HasSideEffect.Should().Be(expectedHasSideEffect);
    }

    [Fact]
    public void ProcessKeyUp_ReturnsNoOp_WhenModifierWasNotHeld()
    {
        var result = _sut.ProcessKeyUp(Key.Apps);

        result.Should().BeOfType<NoOp>();
    }

    [Fact]
    public void ProcessKeyUp_ReturnsNoOp_ForNonModifierKey()
    {
        _sut.ProcessKeyDown(Key.Apps);

        var result = _sut.ProcessKeyUp(Key.A);

        result.Should().BeOfType<NoOp>();
        _sut.IsModifierHeld.Should().BeTrue();
    }

    // ── ProcessKeyDown: letter keys ─────────────────────────────────────────

    [Theory]
    [InlineData(Key.A)]
    [InlineData(Key.Z)]
    public void ProcessKeyDown_ReturnsLetterKeyPressed_WhenLetterPressedWhileModifierHeld(Key key)
    {
        _sut.ProcessKeyDown(Key.Apps);

        var result = _sut.ProcessKeyDown(key);

        result.Should().BeOfType<LetterKeyPressed>()
            .Which.Key.Should().Be(key);
    }

    // ── ProcessKeyDown: digit keys ──────────────────────────────────────────

    [Theory]
    [InlineData(Key.D0)]
    [InlineData(Key.D9)]
    public void ProcessKeyDown_ReturnsDigitKeyPressed_WhenDigitPressedWhileModifierHeld(Key key)
    {
        _sut.ProcessKeyDown(Key.Apps);

        var result = _sut.ProcessKeyDown(key);

        result.Should().BeOfType<DigitKeyPressed>()
            .Which.Key.Should().Be(key);
    }

    // ── ProcessKeyUp: after action keys ─────────────────────────────────────

    [Fact]
    public void ProcessKeyUp_ReturnsModifierReleasedAfterAction_WhenLetterWasPressedWhileHeldAndModifierReleasedFirst()
    {
        _sut.ProcessKeyDown(Key.Apps);
        _sut.ProcessKeyDown(Key.A);

        var result = _sut.ProcessKeyUp(Key.Apps);

        result.Should().BeOfType<ModifierReleasedAfterAction>();
        _sut.IsModifierHeld.Should().BeFalse();
    }

    [Fact]
    public void ProcessKeyUp_ReturnsModifierReleasedAfterAction_WhenLetterWasPressedWhileHeldAndLetterReleasedFirst()
    {
        _sut.ProcessKeyDown(Key.Apps);
        _sut.ProcessKeyDown(Key.A);
        _sut.ProcessKeyUp(Key.A);

        var result = _sut.ProcessKeyUp(Key.Apps);

        result.Should().BeOfType<ModifierReleasedAfterAction>();
        _sut.IsModifierHeld.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(ModifiersSideEffects))]
    public void ProcessKeyDown_ReturnsModifierReleasedAfterActionWithSideEffect_WhenLetterWasPressedWhileHeld(Key modifierKey, bool expectedHasSideEffect)
    {
        _sut.Configure(modifierKey);
        _sut.ProcessKeyDown(modifierKey);
        _sut.ProcessKeyDown(Key.A);

        var result = _sut.ProcessKeyUp(modifierKey);

        result.Should().BeOfType<ModifierReleasedAfterAction>()
            .Which.HasSideEffect.Should().Be(expectedHasSideEffect);
    }

    [Fact]
    public void ProcessKeyUp_ReturnsModifierReleasedAfterAction_WhenDigitWasPressedWhileHeld()
    {
        _sut.ProcessKeyDown(Key.Apps);
        _sut.ProcessKeyDown(Key.D1);

        var result = _sut.ProcessKeyUp(Key.Apps);

        result.Should().BeOfType<ModifierReleasedAfterAction>();
        _sut.IsModifierHeld.Should().BeFalse();
    }

    [Fact]
    public void ProcessKeyUp_ReturnsModifierReleasedAfterAction_WhenMultipleLettersPressedWhileHeld()
    {
        _sut.ProcessKeyDown(Key.Apps);
        _sut.ProcessKeyDown(Key.A);
        _sut.ProcessKeyDown(Key.B);

        var result = _sut.ProcessKeyUp(Key.Apps);

        result.Should().BeOfType<ModifierReleasedAfterAction>();
    }

    // ── Unrelated key handling ──────────────────────────────────────────────

    [Fact]
    public void ProcessKeyDown_ReturnsUnrelatedKeyReset_WhenUnrelatedKeyPressedWhileModifierHeld()
    {
        _sut.ProcessKeyDown(Key.Apps);

        var result = _sut.ProcessKeyDown(Key.F5);

        result.Should().BeOfType<UnrelatedKeyReset>();
        _sut.IsModifierHeld.Should().BeFalse();
    }

    [Fact]
    public void ProcessKeyDown_ReturnsNoOp_ForUnrelatedKeyWhenModifierNotHeld()
    {
        var result = _sut.ProcessKeyDown(Key.F5);

        result.Should().BeOfType<NoOp>();
    }

    // ── Reset ───────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_ClearsModifierHeldState()
    {
        _sut.ProcessKeyDown(Key.Apps);

        _sut.Reset();

        _sut.IsModifierHeld.Should().BeFalse();
    }

    [Fact]
    public void Reset_AllowsModifierToBePressedAgainAsFirstPress()
    {
        _sut.ProcessKeyDown(Key.Apps);
        _sut.Reset();

        var result = _sut.ProcessKeyDown(Key.Apps);

        result.Should().BeOfType<ModifierPressed>()
            .Which.IsFirstPress.Should().BeTrue();
    }

    // ── Configure ───────────────────────────────────────────────────────────

    [Fact]
    public void Configure_ChangesRecognisedModifierKey()
    {
        _sut.Configure(Key.LWin);

        var result = _sut.ProcessKeyDown(Key.LWin);

        result.Should().BeOfType<ModifierPressed>()
            .Which.IsFirstPress.Should().BeTrue();
    }

    [Fact]
    public void Configure_OldModifierNoLongerTriggersModifierPressed()
    {
        _sut.Configure(Key.LWin);

        var result = _sut.ProcessKeyDown(Key.Apps); // old modifier

        result.Should().BeOfType<NoOp>();
    }

    [Fact]
    public void Configure_ResetsStateMachineState()
    {
        _sut.ProcessKeyDown(Key.Apps); // modifier held

        _sut.Configure(Key.LWin); // reconfigure while held

        _sut.IsModifierHeld.Should().BeFalse();
    }

    // ── HeldDurationMs ─────────────────────────────────────────────

    [Fact]
    public async Task ProcessKeyUp_ReturnsApproximateHeldDuration_InModifierReleasedClean()
    {
        const int delayMs = 50;
        _sut.ProcessKeyDown(Key.Apps);

        await Task.Delay(delayMs);
        var result = _sut.ProcessKeyUp(Key.Apps);

        result.Should().BeOfType<ModifierReleasedClean>()
            .Which.HeldDurationMs.Should().BeGreaterThanOrEqualTo(delayMs - 10);
    }

    // ── Full sequence: press → action → release ──────────────────────────────

    [Fact]
    public void FullSequence_ModifierPressLetterRelease_ProducesExpectedTransitions()
    {
        _sut.ProcessKeyDown(Key.Apps).Should().BeOfType<ModifierPressed>()
            .Which.IsFirstPress.Should().BeTrue();
        _sut.ProcessKeyDown(Key.Apps).Should().BeOfType<ModifierPressed>()
            .Which.IsFirstPress.Should().BeFalse();
        _sut.ProcessKeyDown(Key.A).Should().BeOfType<LetterKeyPressed>();
        _sut.ProcessKeyUp(Key.A).Should().BeOfType<NoOp>();
        _sut.ProcessKeyUp(Key.Apps).Should().BeOfType<ModifierReleasedAfterAction>();
        _sut.IsModifierHeld.Should().BeFalse();
    }

    [Fact]
    public void FullSequence_ModifierPressDigitRelease_ProducesExpectedTransitions()
    {
        _sut.ProcessKeyDown(Key.Apps).Should().BeOfType<ModifierPressed>();
        _sut.ProcessKeyDown(Key.D1).Should().BeOfType<DigitKeyPressed>();
        _sut.ProcessKeyUp(Key.Apps).Should().BeOfType<ModifierReleasedAfterAction>();
        _sut.IsModifierHeld.Should().BeFalse();
    }

    [Fact]
    public void FullSequence_ModifierPressRelease_ProducesCleanRelease()
    {
        _sut.ProcessKeyDown(Key.Apps).Should().BeOfType<ModifierPressed>();
        _sut.ProcessKeyUp(Key.Apps).Should().BeOfType<ModifierReleasedClean>();
        _sut.IsModifierHeld.Should().BeFalse();
    }

    [Fact]
    public void FullSequence_ModifierPressUnrelatedRelease_ResetsAndReleasesCleanly()
    {
        _sut.ProcessKeyDown(Key.Apps).Should().BeOfType<ModifierPressed>();
        _sut.ProcessKeyDown(Key.F5).Should().BeOfType<UnrelatedKeyReset>();
        _sut.IsModifierHeld.Should().BeFalse();
    }

    [Fact]
    public void FullSequence_CanBePressedAgainAfterCleanRelease()
    {
        _sut.ProcessKeyDown(Key.Apps);
        _sut.ProcessKeyUp(Key.Apps);

        var result = _sut.ProcessKeyDown(Key.Apps);

        result.Should().BeOfType<ModifierPressed>()
            .Which.IsFirstPress.Should().BeTrue();
    }
}
