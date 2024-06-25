using AppSwitcher.Extensions;
using AppSwitcher.WindowDiscovery;
using KeyboardHookLite;
using Microsoft.Extensions.Logging;
using System.Windows.Input;

namespace AppSwitcher;

internal class Hook : IDisposable
{
    private readonly KeyboardHook _hook;
    private readonly ILogger<Hook> _logger;
    private readonly WindowHelper _windowHelper;
    private Configuration.Configuration? _config;
    private Switcher _switcher;

    private bool _modifierDown = false;

    public Hook(ILogger<Hook> logger, WindowHelper windowHelper, Switcher switcher)
    {
        _hook = new KeyboardHook();
        this._logger = logger;
        this._windowHelper = windowHelper;
        _switcher = switcher;
    }

    public void Start(Configuration.Configuration config)
    {
        this._config = config;
        _logger.LogInformation("Starting hook");
        _hook.KeyboardPressed += Hook_KeyboardPressed;
    }

    public void Stop()
    {
        _logger.LogInformation("Stopping hook");
        _hook.KeyboardPressed -= Hook_KeyboardPressed;
    }

    public void Dispose()
    {
        Stop();
        _hook.Dispose();
    }

    private void Hook_KeyboardPressed(object? sender, KeyboardHookEventArgs e)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(_config);

            if (IsModifier(e.InputEvent.Key))
            {
                _logger.LogTrace("Suppressing {Key} {Type}", e.InputEvent.Key, e.KeyPressType);
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
            _logger.LogError(ex, "Unexpected error handling key press");
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

                _logger.LogDebug("{Modifier}-{Letter} detected", config.Modifier, letter);
                _switcher.Execute(appConfig);
            }
        }
    }

    private bool IsLetter(Key key) => key is >= Key.A and <= Key.Z;
    private bool IsModifier(Key key) => _config!.Modifier == key;
}
