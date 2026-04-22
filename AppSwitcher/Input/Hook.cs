using AppSwitcher.Extensions;
using AppSwitcher.Overlay;
using AppSwitcher.Stats;
using AppSwitcher.WindowDiscovery;
using KeyboardHookLite;
using Microsoft.Extensions.Logging;
using System.Collections.Frozen;
using System.Windows;
using System.Windows.Input;
using AppConfig = AppSwitcher.Configuration.Configuration;

namespace AppSwitcher.Input;

internal class Hook(
    ILogger<Hook> logger,
    Switcher switcher,
    Peeker peeker,
    IWindowEnumerator windowEnumerator,
    OverlayShowTimer overlayShowTimer,
    WarningOverlayService warningOverlayService,
    AppOverlayService overlayService,
    ProcessInspector processInspector,
    DynamicModeService dynamicModeService,
    StatsService statsService) : IDisposable
{
    private const int SyntheticModifierTapMaxDurationMs = 200;

    private readonly KeyboardHook _hook = new();
    private readonly KeyStateMachine _stateMachine = new();
    private AppConfig? _config;
    private readonly HashSet<Key> _suppressedLetterKeys = [];
    private readonly HashSet<Key> _suppressedDigitKeys = [];
    private long? _previousLetterUpTick;

    // there are apps which run un-elevated will still steal key events
    private readonly FrozenSet<string> _processesStealingKeyEvents = new[]
    {
        "WindowsSandboxRemoteSession.exe", // Windows Sandbox window intercepts key events when active
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public void Start(AppConfig config)
    {
        _config = config;
        _stateMachine.Configure(config.Modifier);
        overlayShowTimer.Configure(onExpired: () => overlayService.Show(config.Applications, config.DynamicModeEnabled), config.OverlayShowDelayMs);
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
        _stateMachine.Configure(config.Modifier);
        overlayShowTimer.Configure(onExpired: () => overlayService.Show(config.Applications, config.DynamicModeEnabled), config.OverlayShowDelayMs);
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

            logger.LogDebug("{Event}, ModifierDown: {ModifierDown}", e.ToFriendlyString(), _stateMachine.IsModifierHeld);

            if (e.IsKeyDown())
            {
                HandleKeyDown(e);
            }
            else
            {
                HandleKeyUp(e);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error handling key press");
        }
    }

    private void HandleKeyDown(KeyboardHookEventArgs e)
    {
        switch (_stateMachine.ProcessKeyDown(e.InputEvent.Key))
        {
            case KeyTransition.ModifierPressed t:
                if (t.HasSideEffect)
                {
                    SuppressModifier(e);
                }

                if (t.IsFirstPress && _config!.OverlayEnabled)
                {
                    overlayShowTimer.Start();
                }

                break;
            case KeyTransition.LetterKeyPressed { Key: var letter }:
                HandleLetterPressed(e, letter);
                break;
            case KeyTransition.DigitKeyPressed { Key: var digit }:
                HandleDigitPressed(e, digit);
                break;
            case KeyTransition.UnrelatedKeyReset:
                logger.LogDebug("Unrelated key {Key} pressed while modifier down - resetting state", e.InputEvent.Key);
                ResetModifierState();
                break;
        }
    }

    private void HandleKeyUp(KeyboardHookEventArgs e)
    {
        var letterWasSuppressed = _suppressedLetterKeys.Remove(e.InputEvent.Key);
        var digitWasSuppressed = _suppressedDigitKeys.Remove(e.InputEvent.Key);

        if (letterWasSuppressed || digitWasSuppressed)
        {
            e.SuppressKeyPress = true;
            logger.LogDebug("Suppressing key up for previously suppressed {Key}", e.InputEvent.Key);
            _previousLetterUpTick = Environment.TickCount64;
            FinishPeek();
            return;
        }

        var wasOverlayVisible = overlayService.IsVisible;
        switch (_stateMachine.ProcessKeyUp(e.InputEvent.Key))
        {
            case KeyTransition.ModifierReleasedClean t:
                overlayShowTimer.Cancel();
                overlayService.Hide();
                _previousLetterUpTick = null;
                if (t.HasSideEffect)
                {
                    SuppressModifier(e);

                    if (!wasOverlayVisible)
                    {
                        if (t.HeldDurationMs <= SyntheticModifierTapMaxDurationMs)
                        {
                            var result = KeyboardInput.SendSyntheticKeyDownUp(e.InputEvent.Key);
                            logger.LogDebug(
                                "Sent synthetic key for modifier {Key}, press duration {Duration}ms, success: {Result}",
                                e.InputEvent.Key, t.HeldDurationMs, result);
                        }
                        else
                        {
                            logger.LogDebug(
                                "Skipped synthetic key for modifier {Key} - press duration {Duration}ms exceeded threshold",
                                e.InputEvent.Key, t.HeldDurationMs);
                        }
                    }
                }

                break;

            case KeyTransition.ModifierReleasedAfterAction t:
                overlayShowTimer.Cancel();
                overlayService.Hide();
                _previousLetterUpTick = null;
                if (t.HasSideEffect)
                {
                    SuppressModifier(e);
                }
                FinishPeek();
                break;
        }
    }

    private void SuppressModifier(KeyboardHookEventArgs e)
    {
        e.SuppressKeyPress = true;
        logger.LogDebug("Modifier key {Key} with side effects - suppressing", e.InputEvent.Key);
    }

    private void HandleLetterPressed(KeyboardHookEventArgs e, Key letter)
    {
        ArgumentNullException.ThrowIfNull(_config);

        var matchingApps = _config.Applications.Where(a => a.Key == letter).ToList();
        var isDynamic = false;

        if (matchingApps.Count == 0 && _config.DynamicModeEnabled)
        {
            matchingApps = [.. dynamicModeService.GetAppsForKey(letter, _config.Applications)];
            isDynamic = true;
        }

        if (matchingApps.Count > 0)
        {
            e.SuppressKeyPress = true;
            if (_suppressedLetterKeys.Add(letter))
            {
                logger.LogDebug("{Modifier} + {Letter} detected", _config.Modifier, letter);
                var currentWindow = windowEnumerator.GetCurrentWindow();
                var result = switcher.Execute(matchingApps);
                var isAppStealingKeyEvents = result != null && _processesStealingKeyEvents.Contains(result.ProcessName);

                if (_config.StatsEnabled && result is { WasStarted: false })
                {
                    statsService.Enqueue(new SwitchEvent(
                        ProcessName: result.ProcessName,
                        ProcessId: result.ProcessId,
                        ProcessPath: result.ProcessPath,
                        TotalChoices: windowEnumerator.GetTotalChoicesCount(),
                        ModifierDownTick: _stateMachine.ModifierPressedAtTick,
                        LetterDownTick: Environment.TickCount64,
                        PreviousLetterUpTick: _previousLetterUpTick,
                        IsDynamic: isDynamic));
                }

                if (_config.PeekEnabled && result?.WasStarted == false && currentWindow is not null &&
                    currentWindow.ProcessId != result.ProcessId && !isAppStealingKeyEvents)
                {
                    peeker.Arm(currentWindow, result, isDynamic);
                    if (!overlayService.IsVisible)
                    {
                        // do not show overlay if peek mode is arming
                        overlayShowTimer.Cancel();
                    }
                }

                if (result is { NeedsElevation: true })
                {
                    // switching to elevated app so need to reset the state to avoid ghost modifier side effect
                    ResetModifierState();
                    warningOverlayService.Show(WarningContent.Elevated);
                }
                else if (isAppStealingKeyEvents)
                {
                    ResetModifierState();
                    warningOverlayService.Show(WarningContent.KeyEventsStealing);
                }
                else
                {
                    RefreshOrHideOverlay();
                    if (result?.WasStarted == true)
                    {
                        MonitorPotentialElevation(result);
                    }
                }
            }
        }
    }

    private void MonitorPotentialElevation(AppSwitchResult result)
    {
        _ = Task.Run(async () =>
        {
            var elevated = await processInspector.WaitForPotentialElevation(result.ProcessPath);
            if (elevated)
            {
                logger.LogDebug("Newly started process {ProcessPath} is elevated, showing warning", result.ProcessPath);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ResetModifierState();
                    warningOverlayService.Show(WarningContent.Elevated);
                });
            }
        });
    }

    private void HandleDigitPressed(KeyboardHookEventArgs e, Key digit)
    {
        ArgumentNullException.ThrowIfNull(_config);

        var index = DigitKeyToIndex(digit);
        if (switcher.SwitchToWindowByIndex(_config.Applications, index))
        {
            e.SuppressKeyPress = true;
            _suppressedDigitKeys.Add(digit);

            logger.LogDebug("{Modifier} + {Digit} detected, switched to window #{Number}", _config.Modifier, digit, index + 1);
            RefreshOrHideOverlay();
        }
    }

    private void FinishPeek()
    {
        if (peeker.TryFinish(out var peekResult))
        {
            if (_config?.StatsEnabled == true)
            {
                statsService.Enqueue(new PeekEvent(TargetProcessName: peekResult.TargetProcessName,
                    ArmTick: peekResult.ArmedAtTick,
                    FinishTick: Environment.TickCount64,
                    IsDynamic: peekResult.IsDynamic));
            }

            switcher.ActivateWindow(peekResult.PreviousWindow, pulseBorder: false);
            if (peekResult.TargetWasMinimized)
            {
                switcher.HideWindow(peekResult.TargetHandle);
            }
        }
    }

    private void RefreshOrHideOverlay()
    {
        ArgumentNullException.ThrowIfNull(_config);

        if (_config.OverlayKeepOpenWhileModifierHeld && overlayService.IsVisible)
        {
            overlayService.Show(_config.Applications, _config.DynamicModeEnabled);
        }
        else
        {
            overlayService.Hide();
        }
    }

    // Inverse of AppOverlayService.IndexToKey: D1→0, D2→1, …, D9→8, D0→9
    private static int DigitKeyToIndex(Key key) => key == Key.D0 ? 9 : key - Key.D1;

    private void ResetModifierState()
    {
        _stateMachine.Reset();
        _suppressedLetterKeys.Clear();
        _suppressedDigitKeys.Clear();
        _previousLetterUpTick = null;
        peeker.Cancel();
        overlayShowTimer.Cancel();
        overlayService.Hide();
    }
}