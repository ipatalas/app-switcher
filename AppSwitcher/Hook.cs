using AppSwitcher.Extensions;
using AppSwitcher.Utils;
using KeyboardHookLite;
using Microsoft.Extensions.Logging;
using System.Windows.Input;

namespace AppSwitcher;

internal class Hook(ILogger<Hook> logger, Switcher switcher, ModifierIdleTimer modifierIdleTimer) : IDisposable
{
    private readonly KeyboardHook _hook = new();
    private Configuration.Configuration? _config;
    private bool _modifierDown;
    private bool _letterKeyPressedWithModifier;

    public void Start(Configuration.Configuration config)
    {
        _config = config;
        modifierIdleTimer.Configure(onExpired: ResetModifierState, config.ModifierIdleTimeoutMs);
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
                _modifierDown = e.IsKeyDown();
                e.SuppressKeyPress = true;

                if (e.IsKeyDown()) // modifier down
                {
                    _letterKeyPressedWithModifier = false;
                    // Restart timer on each key repeat
                    modifierIdleTimer.Restart();
                }
                else if (!_letterKeyPressedWithModifier) // modifier up without any letter key pressed
                {
                    modifierIdleTimer.Cancel();

                    // No letter key was pressed while the modifier was held, so we send a synthetic key event for the modifier itself
                    // This allows the modifier key to function normally when pressed and released on its own
                    // without interfering with the app switching functionality
                    var result = KeyboardHelper.SendSyntheticKeyDownUp(e.InputEvent.Key);
                    logger.LogDebug("Sent synthetic key for modifier {Key}, success: {Result}", e.InputEvent.Key, result);
                }
                else // modifier up after letter key was pressed
                {
                    modifierIdleTimer.Cancel();
                }
            }
            else if (_modifierDown && IsLetter(e.InputEvent.Key))
            {
                _letterKeyPressedWithModifier = true;
                HandleLetterKeyPress(e);
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

    private void HandleLetterKeyPress(KeyboardHookEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(_config);

        if (e.IsKeyDown())
        {
            var letter = e.InputEvent.Key;

            var matchingApps = _config.Applications.Where(a => a.Key == letter).ToList();
            if (matchingApps.Count > 0)
            {
                e.SuppressKeyPress = true;

                logger.LogDebug("{Modifier} + {Letter} detected", _config.Modifier, letter);
                switcher.Execute(matchingApps);

                modifierIdleTimer.Restart();
            }
            else // unbound letter key
            {
                modifierIdleTimer.Cancel();
            }
        }
    }

    private bool IsLetter(Key key) => key is >= Key.A and <= Key.Z;
    private bool IsConfiguredModifier(Key key) => _config!.Modifier == key;

    private void ResetModifierState()
    {
        _modifierDown = false;
        modifierIdleTimer.Cancel();
    }
}
