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

    // Alt+Tab tracking (independent of the configured modifier)
    private bool _altHeld;        // LeftAlt or RightAlt is currently down
    private bool _altTabActive;   // Tab was pressed → switcher overlay is open
    private bool _altTabIsSticky; // RightAlt mode: overlay persists after Alt-up, Enter confirms
    private int _altNavCount;     // Tab + arrow keypresses during this session

    public bool IsModifierHeld => _state != State.Idle;

    public long ModifierPressedAtTick { get; private set; }

    public KeyTransition ProcessKeyDown(Key key)
    {
        // Alt+Tab tracking — guard: only when the key is not the configured modifier
        if (key != _configuredModifier && HandleAltTabTracking(out var transition))
        {
            return transition;
        }

        if (key == _configuredModifier)
        {
            var hasSideEffect = ModifierKeysWithSideEffects.Contains(key);
            if (_state != State.Idle)
            {
                // key repeat
                return new KeyTransition.ModifierPressed(IsFirstPress: false, HasSideEffect: hasSideEffect);
            }

            _state = State.ModifierHeld;
            ModifierPressedAtTick = Environment.TickCount64;
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

        bool HandleAltTabTracking(out KeyTransition keyTransition)
        {
            if (key is Key.LeftAlt or Key.RightAlt)
            {
                _altHeld = true;
                _altTabActive = false;
                _altTabIsSticky = key is Key.RightAlt;
                _altNavCount = 0;
            }
            else if (key == Key.Tab && _altHeld)
            {
                _altTabActive = true;
                _altNavCount++;
                keyTransition = new KeyTransition.NoOp();
                return true;
            }
            else if (_altTabActive && IsNavKey(key))
            {
                _altNavCount++;
                keyTransition = new KeyTransition.NoOp();
                return true;
            }
            else if (key == Key.Return && _altTabActive && _altTabIsSticky)
            {
                var count = _altNavCount;
                ResetAltTabState();
                keyTransition = new KeyTransition.AltTabSwitched(count);
                return true;
            }
            else if (key == Key.Escape && _altTabActive)
            {
                ResetAltTabState();
            }

            keyTransition = new KeyTransition.NoOp();
            return false;
        }
    }

    public KeyTransition ProcessKeyUp(Key key)
    {
        // Alt+Tab tracking — guard: only when the key is not the configured modifier
        if (key != _configuredModifier)
        {
            if (key == Key.LeftAlt && _altHeld)
            {
                var hadNav = _altTabActive && !_altTabIsSticky;
                var count = _altNavCount;
                ResetAltTabState();
                return hadNav
                    ? new KeyTransition.AltTabSwitched(count)
                    : new KeyTransition.NoOp();
            }

            if (key == Key.RightAlt)
            {
                _altHeld = false; // overlay stays open; Enter will confirm
            }

            return new KeyTransition.NoOp();
        }

        var previousState = _state;
        _state = State.Idle;

        var hasSideEffect = ModifierKeysWithSideEffects.Contains(key);

        return previousState switch
        {
            State.ModifierHeld => new KeyTransition.ModifierReleasedClean(Environment.TickCount64 - ModifierPressedAtTick, hasSideEffect),
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
        ResetAltTabState();
    }

    private void ResetAltTabState()
    {
        _altHeld = false;
        _altTabActive = false;
        _altTabIsSticky = false;
        _altNavCount = 0;
    }

    private static bool IsNavKey(Key key) =>
        key is Key.Left or Key.Right or Key.Up or Key.Down;
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

    /// <summary>Left/Right Alt released (or Enter in RightAlt sticky mode) after ≥1 Tab press. <see cref="NavCount"/> is total navigation keypresses (Tab + arrow keys).</summary>
    public sealed record AltTabSwitched(int NavCount) : KeyTransition;

    /// <summary>Nothing notable: key-up/down for non-modifier key, or modifier not held.</summary>
    public sealed record NoOp : KeyTransition;
}