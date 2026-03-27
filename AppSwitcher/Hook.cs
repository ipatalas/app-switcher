using AppSwitcher.Extensions;
using AppSwitcher.Utils;
using KeyboardHookLite;
using Microsoft.Extensions.Logging;
using System.Windows.Input;

namespace AppSwitcher;

internal class Hook(
    ILogger<Hook> logger,
    Switcher switcher,
    ModifierIdleTimer modifierIdleTimer,
    OverlayShowTimer overlayShowTimer,
    AppOverlayService overlayService) : IDisposable
{
    private readonly KeyboardHook _hook = new();
    private Configuration.Configuration? _config;
    private bool _modifierDown;
    private bool _letterKeyPressedWithModifier;
    private readonly HashSet<Key> _suppressedLetterKeys = [];

    public void Start(Configuration.Configuration config)
    {
        _config = config;
        modifierIdleTimer.Configure(onExpired: ResetModifierState, config.ModifierIdleTimeoutMs);
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

    public void UpdateConfiguration(Configuration.Configuration config)
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
        e.SuppressKeyPress = true;

        if (e.IsKeyDown()) // modifier down
        {
            _letterKeyPressedWithModifier = false;
            // Restart timer on each key repeat
            modifierIdleTimer.Restart();

            if (!wasModifierDown && _config!.OverlayEnabled) // first press only — not a key repeat
            {
                overlayShowTimer.Start();
            }
        }
        else if (!_letterKeyPressedWithModifier) // modifier up without any letter key pressed
        {
            var wasOverlayVisible = overlayService.IsVisible;
            overlayShowTimer.Cancel();
            overlayService.Hide();
            modifierIdleTimer.Cancel();

            if (!wasOverlayVisible)
            {
                // No letter key was pressed while the modifier was held, so we send a synthetic key event for the modifier itself
                // This allows the modifier key to function normally when pressed and released on its own
                // without interfering with the app switching functionality
                var result = KeyboardHelper.SendSyntheticKeyDownUp(e.InputEvent.Key);
                logger.LogDebug("Sent synthetic key for modifier {Key}, success: {Result}", e.InputEvent.Key, result);
            }
        }
        else // modifier up after letter key was pressed
        {
            overlayShowTimer.Cancel();
            overlayService.Hide();
            modifierIdleTimer.Cancel();
        }
    }

    private void HandleLetterKeyPress(KeyboardHookEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(_config);

        if (!e.IsKeyDown() && _suppressedLetterKeys.Remove(e.InputEvent.Key))
        {
            e.SuppressKeyPress = true;
            logger.LogDebug("Suppressing key up for previously suppressed letter {Key}", e.InputEvent.Key);
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
                    _suppressedLetterKeys.Add(letter);

                    logger.LogDebug("{Modifier} + {Letter} detected", _config.Modifier, letter);
                    switcher.Execute(matchingApps);
                    overlayService.Hide();

                    modifierIdleTimer.Restart();
                }
                else // unbound letter key
                {
                    modifierIdleTimer.Cancel();
                }
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
                    overlayService.Hide();
                    modifierIdleTimer.Restart();
                }
                else // no matching NextWindow app or window index out of range
                {
                    modifierIdleTimer.Cancel();
                }
            }
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
        overlayShowTimer.Cancel();
        overlayService.Hide();
        modifierIdleTimer.Cancel();
    }
}
