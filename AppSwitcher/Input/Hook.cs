using AppSwitcher.Extensions;
using AppSwitcher.Overlay;
using AppSwitcher.WindowDiscovery;
using KeyboardHookLite;
using Microsoft.Extensions.Logging;
using System.Collections.Frozen;
using System.Windows.Input;
using AppConfig = AppSwitcher.Configuration.Configuration;

namespace AppSwitcher.Input;

internal class Hook(
    ILogger<Hook> logger,
    Switcher switcher,
    Peeker peeker,
    WindowEnumerator windowEnumerator,
    OverlayShowTimer overlayShowTimer,
    ElevatedWarningService elevatedWarningService,
    AppOverlayService overlayService) : IDisposable
{
    // Keys that trigger OS-level UI actions when pressed/released in isolation:
    private static readonly FrozenSet<Key> ModifierKeysWithSideEffects = new[]
    {
        Key.LWin, Key.RWin, // opens start menu
        Key.LeftAlt, // opens menu
        Key.RightAlt, // opens menu on keyboard layouts without AltGr
        Key.Apps, // opens context menu
        Key.Capital, // toggles Caps Lock state
    }.ToFrozenSet();

    private const int SyntheticModifierTapMaxDurationMs = 200;

    private readonly KeyboardHook _hook = new();
    private AppConfig? _config;
    private bool _modifierDown;
    private bool _letterKeyPressedWithModifier;
    private long _modifierPressedAtTick;
    private readonly HashSet<Key> _suppressedLetterKeys = [];

    public void Start(AppConfig config)
    {
        _config = config;
        overlayShowTimer.Configure(onExpired: () => overlayService.Show(_config!.Applications), config.OverlayShowDelayMs);
        logger.LogInformation("Starting hook");
        _hook.KeyboardPressed += Hook_KeyboardPressed;
    }

    private void Stop()
    {
        logger.LogInformation("Stopping hook");
        _hook.KeyboardPressed -= Hook_KeyboardPressed;
    }

    public void Dispose()
    {
        Stop();
        _hook.Dispose();
    }

    public void UpdateConfiguration(AppConfig config)
    {
        _config = config;
        overlayShowTimer.Configure(onExpired: () => overlayService.Show(_config!.Applications), config.OverlayShowDelayMs);
        // Reset state when configuration changes (especially if modifier key changes)
        ResetModifierState();
    }

    private void Hook_KeyboardPressed(object? sender, KeyboardHookEventArgs e)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(_config);

            if (e.IsInjected())
            {
                logger.LogDebug("Ignoring injected event for {Event}", e.ToFriendlyString());
                return;
            }

            logger.LogDebug("{Event}, ModifierDown: {ModifierDown}", e.ToFriendlyString(), _modifierDown);

            if (IsConfiguredModifier(e.InputEvent.Key))
            {
                HandleModifierKeyPress(e);
            }
            else if (IsLetter(e.InputEvent.Key))
            {
                HandleLetterKeyPress(e);
            }
            else if (IsDigit(e.InputEvent.Key))
            {
                HandleDigitKeyPress(e);
            }
            else if (_modifierDown && !IsConfiguredModifier(e.InputEvent.Key))
            {
                logger.LogDebug("Unrelated key {Key} pressed while modifier down - resetting state", e.InputEvent.Key);
                ResetModifierState();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error handling key press");
        }
    }

    private void HandleModifierKeyPress(KeyboardHookEventArgs e)
    {
        var wasModifierDown = _modifierDown;
        _modifierDown = e.IsKeyDown();

        var hasSideEffects = ModifierKeysWithSideEffects.Contains(e.InputEvent.Key);
        if (hasSideEffects)
        {
            logger.LogDebug("Modifier key {Key} with side effects - suppressing", e.InputEvent.Key);
            e.SuppressKeyPress = true;
        }

        if (e.IsKeyDown()) // modifier down
        {
            _letterKeyPressedWithModifier = false;
            if (!wasModifierDown) // first press only — not a key repeat
            {
                _modifierPressedAtTick = Environment.TickCount64;
                if (_config!.OverlayEnabled)
                {
                    overlayShowTimer.Start();
                }
            }
        }
        else if (!_letterKeyPressedWithModifier) // modifier up without any letter key pressed
        {
            var wasOverlayVisible = overlayService.IsVisible;
            overlayShowTimer.Cancel();
            overlayService.Hide();

            if (!wasOverlayVisible && hasSideEffects)
            {
                // No letter key was pressed while the modifier was held, so we send a synthetic key event for the modifier itself
                // This allows the modifier key to function normally when pressed and released on its own
                // without interfering with the app switching functionality
                var pressDurationMs = Environment.TickCount64 - _modifierPressedAtTick;
                if (pressDurationMs <= SyntheticModifierTapMaxDurationMs)
                {
                    var result = KeyboardInput.SendSyntheticKeyDownUp(e.InputEvent.Key);
                    logger.LogDebug("Sent synthetic key for modifier {Key}, press duration {Duration}ms, success: {Result}", e.InputEvent.Key, pressDurationMs, result);
                }
                else
                {
                    logger.LogDebug("Skipped synthetic key for modifier {Key} - press duration {Duration}ms exceeded threshold", e.InputEvent.Key, pressDurationMs);
                }
            }
        }
        else // modifier up after letter key was pressed
        {
            overlayShowTimer.Cancel();
            overlayService.Hide();
            FinishPeek();
        }
    }

    private void HandleLetterKeyPress(KeyboardHookEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(_config);

        if (!e.IsKeyDown() && _suppressedLetterKeys.Remove(e.InputEvent.Key))
        {
            e.SuppressKeyPress = true;
            logger.LogDebug("Suppressing key up for previously suppressed letter {Key}", e.InputEvent.Key);
            FinishPeek();
        }
        else if (_modifierDown)
        {
            _letterKeyPressedWithModifier = true;

            if (e.IsKeyDown())
            {
                var letter = e.InputEvent.Key;

                var matchingApps = _config.Applications.Where(a => a.Key == letter).ToList();
                if (matchingApps.Count > 0)
                {
                    e.SuppressKeyPress = true;
                    if (_suppressedLetterKeys.Add(letter))
                    {
                        logger.LogDebug("{Modifier} + {Letter} detected", _config.Modifier, letter);
                        var currentWindow = windowEnumerator.GetCurrentWindow();
                        var window = switcher.Execute(matchingApps);
                        if (_config.PeekEnabled && window is not null && currentWindow is not null && currentWindow.ProcessId != window.ProcessId)
                        {
                            peeker.Arm(currentWindow, window);
                            if (!overlayService.IsVisible)
                            {
                                // do not show overlay if peek mode is arming
                                overlayShowTimer.Cancel();
                            }
                        }

                        if (window is { NeedsElevation: true })
                        {
                            // switching to elevated app so need to reset the state to avoid ghost modifier side effect
                            ResetModifierState();
                            elevatedWarningService.Show();
                        }
                        else
                        {
                            RefreshOrHideOverlay();
                        }
                    }
                }
            }
        }
    }

    private void FinishPeek()
    {
        if (peeker.TryFinish(out var peekResult))
        {
            switcher.ActivateWindow(peekResult.PreviousWindow, pulseBorder: false);
            if (peekResult.TargetWasMinimized)
            {
                switcher.HideWindow(peekResult.TargetHandle);
            }
        }
    }

    private void HandleDigitKeyPress(KeyboardHookEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(_config);

        if (!e.IsKeyDown() && _suppressedLetterKeys.Remove(e.InputEvent.Key))
        {
            e.SuppressKeyPress = true;
            logger.LogDebug("Suppressing key up for previously suppressed digit {Key}", e.InputEvent.Key);
        }
        else if (_modifierDown)
        {
            _letterKeyPressedWithModifier = true;

            if (e.IsKeyDown())
            {
                var digit = e.InputEvent.Key;
                var index = DigitKeyToIndex(digit);

                if (switcher.SwitchToWindowByIndex(_config.Applications, index))
                {
                    e.SuppressKeyPress = true;
                    _suppressedLetterKeys.Add(digit);

                    logger.LogDebug("{Modifier} + {Digit} detected, switched to window #{Number}", _config.Modifier, digit, index + 1);
                    RefreshOrHideOverlay();
                }
            }
        }
    }

    private void RefreshOrHideOverlay()
    {
        ArgumentNullException.ThrowIfNull(_config);

        if (_config.OverlayKeepOpenWhileModifierHeld && overlayService.IsVisible)
        {
            overlayService.Show(_config.Applications);
        }
        else
        {
            overlayService.Hide();
        }
    }

    private static bool IsLetter(Key key) => key is >= Key.A and <= Key.Z;
    private static bool IsDigit(Key key) => key is >= Key.D0 and <= Key.D9;
    private bool IsConfiguredModifier(Key key) => _config!.Modifier == key;

    // Inverse of AppOverlayService.IndexToKey: D1→0, D2→1, …, D9→8, D0→9
    private static int DigitKeyToIndex(Key key) => key == Key.D0 ? 9 : key - Key.D1;

    private void ResetModifierState()
    {
        _modifierDown = false;
        _suppressedLetterKeys.Clear();
        peeker.Cancel();
        overlayShowTimer.Cancel();
        overlayService.Hide();
    }
}