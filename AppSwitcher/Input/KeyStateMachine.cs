using AppSwitcher.Extensions;
using System.Collections.Frozen;
using System.Windows.Input;

namespace AppSwitcher.Input;

internal sealed class KeyStateMachine
{
    private enum State { Idle, ModifierHeld, ModifierHeldWithActionKey }

    // Keys that trigger OS-level UI actions when pressed/released in isolation:
    private static readonly FrozenSet<Key> ModifierKeysWithSideEffects = new[]
    {
        Key.LWin, Key.RWin, // opens start menu
        Key.LeftAlt, // opens menu
        Key.RightAlt, // opens menu on keyboard layouts without AltGr
        Key.Apps, // opens context menu
        Key.Capital, // toggles Caps Lock state
    }.ToFrozenSet();

    private State _state = State.Idle;
    private Key _configuredModifier;
    private long _modifierPressedAtTick;

    public bool IsModifierHeld => _state != State.Idle;

    public KeyTransition ProcessKeyDown(Key key)
    {
        if (key == _configuredModifier)
        {
            var hasSideEffect = ModifierKeysWithSideEffects.Contains(key);
            if (_state != State.Idle)
            {
                // key repeat
                return new KeyTransition.ModifierPressed(IsFirstPress: false, HasSideEffect: hasSideEffect);
            }

            _state = State.ModifierHeld;
            _modifierPressedAtTick = Environment.TickCount64;
            return new KeyTransition.ModifierPressed(IsFirstPress: true, HasSideEffect: hasSideEffect);
        }

        // non-modifier key when modifier is not held
        if (_state == State.Idle)
        {
            return new KeyTransition.NoOp();
        }

        if (key.IsLetter())
        {
            _state = State.ModifierHeldWithActionKey;
            return new KeyTransition.LetterKeyPressed(key);
        }

        if (key.IsDigit())
        {
            _state = State.ModifierHeldWithActionKey;
            return new KeyTransition.DigitKeyPressed(key);
        }

        _state = State.Idle;
        return new KeyTransition.UnrelatedKeyReset();
    }

    public KeyTransition ProcessKeyUp(Key key)
    {
        if (key != _configuredModifier)
        {
            return new KeyTransition.NoOp();
        }

        var previousState = _state;
        _state = State.Idle;

        var hasSideEffect = ModifierKeysWithSideEffects.Contains(key);

        return previousState switch
        {
            State.ModifierHeld => new KeyTransition.ModifierReleasedClean(Environment.TickCount64 - _modifierPressedAtTick, hasSideEffect),
            State.ModifierHeldWithActionKey => new KeyTransition.ModifierReleasedAfterAction(hasSideEffect),
            _ => new KeyTransition.NoOp()
        };
    }

    public void Configure(Key newModifier)
    {
        _configuredModifier = newModifier;
        Reset();
    }

    /// <summary>Force state back to Idle (e.g. on config reload or elevated-app detection).</summary>
    public void Reset()
    {
        _state = State.Idle;
    }
}

internal abstract record KeyTransition
{
    internal abstract record ModifierTransition(bool HasSideEffect) : KeyTransition;

    /// <summary>Modifier was pressed. <see cref="IsFirstPress"/> distinguishes first press from key-repeat.</summary>
    public sealed record ModifierPressed(bool IsFirstPress, bool HasSideEffect) : ModifierTransition(HasSideEffect);

    /// <summary>Modifier released without any letter or digit key pressed while held.</summary>
    public sealed record ModifierReleasedClean(long HeldDurationMs, bool HasSideEffect) : ModifierTransition(HasSideEffect);

    /// <summary>Modifier released after at least one letter or digit was pressed while held (matched or not).</summary>
    public sealed record ModifierReleasedAfterAction(bool HasSideEffect) : ModifierTransition(HasSideEffect);

    /// <summary>A letter key was pressed while the modifier is held.</summary>
    public sealed record LetterKeyPressed(Key Key) : KeyTransition;

    /// <summary>A digit key was pressed while the modifier is held.</summary>
    public sealed record DigitKeyPressed(Key Key) : KeyTransition;

    /// <summary>An unrelated key was pressed while the modifier was held; state has been reset to Idle.</summary>
    public sealed record UnrelatedKeyReset : KeyTransition;

    /// <summary>Nothing notable: key-up/down for non-modifier key, or modifier not held.</summary>
    public sealed record NoOp : KeyTransition;
}