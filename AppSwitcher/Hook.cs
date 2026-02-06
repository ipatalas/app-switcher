using AppSwitcher.Extensions;
using AppSwitcher.Utils;
using KeyboardHookLite;
using Microsoft.Extensions.Logging;
using System.Windows.Input;

namespace AppSwitcher;

internal class Hook(ILogger<Hook> logger, Switcher switcher) : IDisposable
{
    private readonly KeyboardHook _hook = new();
    private Configuration.Configuration? _config;
    private bool _modifierDown;
    private bool _letterKeyPressedWithModifier;

    public void Start(Configuration.Configuration config)
    {
        _config = config;
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
    }

    private void Hook_KeyboardPressed(object? sender, KeyboardHookEventArgs e)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(_config);

            if (e.IsInjected())
            {
                // Ignore injected/synthetic events to prevent recursion
                logger.LogDebug("Ignoring injected event for {Key} {Type}", e.InputEvent.Key, e.KeyPressType);
                return;
            }

            logger.LogDebug("{Key} {Type} - _modifierDown: {_modifierDown}",
                e.InputEvent.Key, e.KeyPressType, _modifierDown);

            if (IsModifier(e.InputEvent.Key))
            {
                _modifierDown = e.IsKeyDown();
                e.SuppressKeyPress = true;

                if (e.IsKeyDown())
                {
                    _letterKeyPressedWithModifier = false;
                }
                else if (!_letterKeyPressedWithModifier)
                {
                    // No letter key was pressed while the modifier was held, so we send a synthetic key event for the modifier itself
                    // This allows the modifier key to function normally when pressed and released on its own
                    // without interfering with the app switching functionality
                    var result = KeyboardHelper.SendSyntheticKeyDownUp(e.InputEvent.Key);
                    logger.LogDebug("Sent synthetic key for modifier {Key}, success: {Result}", e.InputEvent.Key, result);
                }
            }
            else if (_modifierDown && IsLetter(e.InputEvent.Key))
            {
                _letterKeyPressedWithModifier = true;
                HandleLetterKeyPress(e, _config);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error handling key press");
        }
    }

    private void HandleLetterKeyPress(KeyboardHookEventArgs e, Configuration.Configuration config)
    {
        if (e.IsKeyDown())
        {
            var letter = e.InputEvent.Key;

            var appConfig = config.Applications.FirstOrDefault(a => a.Key == letter);
            if (appConfig is not null)
            {
                e.SuppressKeyPress = true;

                logger.LogDebug("{Modifier}-{Letter} detected", config.Modifier, letter);
                switcher.Execute(appConfig);
            }
        }
    }

    private bool IsLetter(Key key) => key is >= Key.A and <= Key.Z;
    private bool IsModifier(Key key) => _config!.Modifier == key;
}
