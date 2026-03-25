using AppSwitcher.Configuration;
using AppSwitcher.UI.ViewModels;
using AppSwitcher.UI.Windows;
using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.Logging;
using System.IO;
using Application = System.Windows.Application;

namespace AppSwitcher.Utils;

internal class AppOverlayService(
    AppOverlayWindow window,
    AppOverlayViewModel viewModel,
    WindowHelper windowHelper,
    IconExtractor iconExtractor,
    ILogger<AppOverlayService> logger)
{
    public bool IsVisible { get; private set; }

    public void Show(IReadOnlyList<ApplicationConfiguration> applications)
    {
        var runningProcessNames = windowHelper.GetWindows()
            .Select(w => Path.GetFileName(w.ProcessImageName).ToLowerInvariant())
            .ToHashSet();

        var running = new List<OverlayAppItem>();
        var launchable = new List<OverlayAppItem>();

        foreach (var app in applications)
        {
            var icon = iconExtractor.GetByProcessPath(app.ProcessPath);
            var displayName = Path.GetFileNameWithoutExtension(app.ProcessPath);
            var item = new OverlayAppItem(app.Key, displayName, icon);

            if (runningProcessNames.Contains(app.ProcessName.ToLowerInvariant()))
            {
                running.Add(item);
            }
            else
            {
                launchable.Add(item);
            }
        }

        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            viewModel.Update(running, launchable);
            window.Show();
            IsVisible = true;
            logger.LogDebug("Overlay shown: {Running} running, {Launchable} launchable apps", running.Count,
                launchable.Count);
        });
    }

    public void Hide()
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            window.Hide();
            IsVisible = false;
            logger.LogDebug("Overlay hidden");
        });
    }
}