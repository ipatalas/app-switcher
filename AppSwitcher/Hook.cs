using AppSwitcher.Extensions;
using KeyboardHookLite;
using Microsoft.Extensions.Logging;
using System.Windows.Input;

namespace AppSwitcher;

internal class Hook(ILogger<Hook> logger, Switcher switcher) : IDisposable
{
    private readonly KeyboardHook _hook = new();
    private Configuration.Configuration? _config;
    private bool _modifierDown;

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
            logger.LogDebug("{Key} {Type} - _modifierDown: {_modifierDown}",
                e.InputEvent.Key, e.KeyPressType, _modifierDown);

            if (IsModifier(e.InputEvent.Key))
            {
                logger.LogTrace("Suppressing {Key} {Type}", e.InputEvent.Key, e.KeyPressType);
                _modifierDown = e.IsKeyDown();
                e.SuppressKeyPress = true;
                return;
            }

            if (_modifierDown && IsLetter(e.InputEvent.Key))
            {
                HandleKeyPress(e, _config);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error handling key press");
        }
    }

    private void HandleKeyPress(KeyboardHookEventArgs e, Configuration.Configuration config)
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
