using AppSwitcher.Utils;
using AppSwitcher.WindowDiscovery;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace AppSwitcher.UI.ViewModels;

internal partial class AboutViewModel : ObservableObject
{
    private readonly AutoStart _autoStart = null!;
    private readonly ILogger<AboutViewModel> _logger = null!;
    private readonly ISnackbarService _snackbarService = null!;
    private readonly WindowHelper _windowHelper = null!;

    public string AppName => "AppSwitcher";

    public string AppVersion { get; } = "Version " + Utils.AppVersion.Version;

    public string AppWebsite => "https://app-switcher.com";

    public string DotNetVersion { get; } = ".NET " + Environment.Version;

    public ImageSource? AppIcon { get; }

    [ObservableProperty]
    private bool _launchAtStartup;

    [UsedImplicitly(Reason = "Design-time constructor")]
    public AboutViewModel()
    {
        AppVersion = "Version 1.2.3";
        _launchAtStartup = true;
        AppIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/default_app_icon.png"));
    }

    public AboutViewModel(AutoStart autoStart, IconExtractor iconExtractor, ILogger<AboutViewModel> logger, ISnackbarService snackbarService, WindowHelper windowHelper)
    {
        _autoStart = autoStart;
        _logger = logger;
        _snackbarService = snackbarService;
        _windowHelper = windowHelper;
        _launchAtStartup = autoStart.IsEnabled();
        AppIcon = iconExtractor.GetByProcessName(Environment.ProcessPath ?? string.Empty);
    }

    partial void OnLaunchAtStartupChanged(bool value)
    {
        if (value)
        {
            _autoStart.CreateShortcut();
        }
        else
        {
            _autoStart.RemoveShortcut();
        }
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        var folder = ResolveLogFolder();
        try
        {
#if DEBUG_ERROR_HANDLING
            throw new InvalidOperationException("Simulated failure in OpenLogFolder");
#endif
            Process.Start("explorer.exe", folder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open log folder: {Folder}", folder);
            _snackbarService.Show(
                "Could not open log folder",
                $"The folder could not be opened: {folder}",
                ControlAppearance.Danger,
                new SymbolIcon { Symbol = SymbolRegular.ErrorCircle20 },
                TimeSpan.FromSeconds(5));
        }
    }

    [RelayCommand]
    private void OpenWebsite()
    {
        try
        {
#if DEBUG_ERROR_HANDLING
            throw new InvalidOperationException("Simulated failure in OpenWebsite");
#endif
            Process.Start(new ProcessStartInfo(AppWebsite) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open website");
            _snackbarService.Show(
                "Could not open website",
                $"Please navigate to {AppWebsite} manually.",
                ControlAppearance.Danger,
                new SymbolIcon { Symbol = SymbolRegular.ErrorCircle20 },
                TimeSpan.FromSeconds(5));
        }
    }

    [RelayCommand]
    private void CopyDiagnostics()
    {
        try
        {
#if DEBUG_ERROR_HANDLING
            throw new InvalidOperationException("Simulated failure in CopyDiagnostics");
#endif
            var windows = _windowHelper.GetAllWindows();
            var csv = BuildCsv(windows);
            System.Windows.Clipboard.SetText(csv);

            _snackbarService.Show(
                "Copied to clipboard",
                $"Diagnostics for {windows.Count} windows copied.",
                ControlAppearance.Success,
                new SymbolIcon { Symbol = SymbolRegular.ClipboardCheckmark20 },
                TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy diagnostics");
            _snackbarService.Show(
                "Failed to copy diagnostics",
                "Could not copy window information to the clipboard.",
                ControlAppearance.Danger,
                new SymbolIcon { Symbol = SymbolRegular.ErrorCircle20 },
                TimeSpan.FromSeconds(5));
        }
    }

    private static string BuildCsv(IReadOnlyList<ApplicationWindow> windows)
    {
        var sb = new StringBuilder(windows.Count * 500);
        sb.AppendLine("ProcessId,Handle,ProcessImageName,ProductName,Title,State,Style,StyleEx,IsCloaked");
        foreach (var w in windows)
        {
            sb.AppendLine(string.Join(",",
                w.ProcessId,
                w.Handle,
                CsvEscape(w.ProcessImageName),
                CsvEscape(w.GetProductName()),
                CsvEscape(w.Title),
                w.State,
                CsvEscape(WindowStyleHelpers.GetString(w.Style)),
                CsvEscape(WindowStyleHelpers.GetString(w.StyleEx)),
                w.IsCloaked));
        }
        return sb.ToString();
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }
        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    private static string ResolveLogFolder()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var configPath = Path.Combine(baseDir, "nlog.config");
        if (!File.Exists(configPath))
        {
            return baseDir;
        }

        try
        {
            var doc = XDocument.Load(configPath);
            XNamespace nlogNs = "http://www.nlog-project.org/schemas/NLog.xsd";
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

            var fileTarget = doc.Descendants(nlogNs + "target")
                .FirstOrDefault(t => (string?)t.Attribute(xsi + "type") == "File");

            if (fileTarget is null)
            {
                return baseDir;
            }

            var fileName = (string?)fileTarget.Attribute("fileName");
            if (string.IsNullOrEmpty(fileName))
            {
                return baseDir;
            }

            // Strip NLog layout variables (${...}) to get a plain path skeleton
            var cleanPath = Regex.Replace(fileName, @"\$\{[^}]+\}", string.Empty);
            var dir = Path.GetDirectoryName(cleanPath);

            if (string.IsNullOrEmpty(dir))
            {
                return baseDir;
            }

            return Path.IsPathRooted(dir) ? dir : Path.Combine(baseDir, dir);
        }
        catch
        {
            return baseDir;
        }
    }
}
