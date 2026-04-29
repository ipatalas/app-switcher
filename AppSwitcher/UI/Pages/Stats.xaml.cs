using AppSwitcher.Stats;
using AppSwitcher.UI.ViewModels;
using AppSwitcher.UI.ViewModels.Common;
using Microsoft.Extensions.Logging;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace AppSwitcher.UI.Pages;

internal partial class Stats : Page
{
    private readonly DispatcherTimer _debounceTimer;

    public Stats(StatsSettingsViewModel viewModel, SessionStats sessionStats, ILogger<Stats> logger)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Snap to the correct initial state without any animation so the first
        // render already shows the right values, regardless of StatsEnabled.
        ApplyPrivacyState(viewModel.State.StatsEnabled, animate: false);

        // Animate only on subsequent user-driven changes.
        viewModel.State.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ISettingsState.StatsEnabled))
            {
                ApplyPrivacyState(viewModel.State.StatsEnabled, animate: true);
            }
        };

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounceTimer.Tick += async (_, _) =>
        {
            _debounceTimer.Stop();
            try
            {
                await viewModel.RefreshCommand.ExecuteAsync(null);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error refreshing stats");
            }
        };

        Loaded += async (_, _) =>
        {
            sessionStats.DataChanged += OnSessionDataChanged;
            try
            {
                await viewModel.RefreshCommand.ExecuteAsync(null);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error loading stats");
            }
        };

        Unloaded += (_, _) =>
        {
            sessionStats.DataChanged -= OnSessionDataChanged;
            _debounceTimer.Stop();
        };
    }

    // ── Privacy state management ───────────────────────────────────────────────

    /// <summary>
    /// Applies the correct blur radius and overlay opacity for the given
    /// <paramref name="statsEnabled"/> state. When <paramref name="animate"/>
    /// is <see langword="false"/> the values are snapped immediately (used on
    /// first load to avoid an unwanted intro animation).
    /// </summary>
    private void ApplyPrivacyState(bool statsEnabled, bool animate)
    {
        var blurEffect = (BlurEffect)BentoLayer.Effect;
        var targetRadius = statsEnabled ? 0.0 : 20.0;
        var targetOpacity = statsEnabled ? 0.0 : 1.0;

        if (!animate)
        {
            // Cancel any in-flight animations and set values directly.
            blurEffect.BeginAnimation(BlurEffect.RadiusProperty, null);
            blurEffect.Radius = targetRadius;

            PrivacyOverlay.BeginAnimation(OpacityProperty, null);
            PrivacyOverlay.Opacity = targetOpacity;
            PrivacyOverlay.IsHitTestVisible = !statsEnabled;
            return;
        }

        if (statsEnabled)
        {
            // Blur out then release the property so future snaps work cleanly.
            var duration = TimeSpan.FromMilliseconds(200);
            var blurOut = new DoubleAnimation(0.0, duration);
            blurOut.Completed += (_, _) =>
            {
                blurEffect.BeginAnimation(BlurEffect.RadiusProperty, null);
                blurEffect.Radius = 0.0;
            };
            blurEffect.BeginAnimation(BlurEffect.RadiusProperty, blurOut);

            // Fade out overlay, then restore the base values.
            var fadeOut = new DoubleAnimation(0.0, duration);
            fadeOut.Completed += (_, _) =>
            {
                PrivacyOverlay.BeginAnimation(OpacityProperty, null);
                PrivacyOverlay.Opacity = 0.0;
                PrivacyOverlay.IsHitTestVisible = false;
            };
            PrivacyOverlay.BeginAnimation(OpacityProperty, fadeOut);
        }
        else
        {
            // Make overlay interactive immediately so the button is clickable.
            PrivacyOverlay.IsHitTestVisible = true;

            var duration = TimeSpan.FromMilliseconds(250);
            blurEffect.BeginAnimation(BlurEffect.RadiusProperty, new DoubleAnimation(20.0, duration));
            PrivacyOverlay.BeginAnimation(OpacityProperty, new DoubleAnimation(1.0, duration));
        }
    }

    // ── Session data ───────────────────────────────────────────────────────────

    private void OnSessionDataChanged()
    {
        Dispatcher.InvokeAsync(() =>
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        });
    }
}