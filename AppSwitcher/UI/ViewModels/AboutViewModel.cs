using AppSwitcher.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Xml.Linq;

namespace AppSwitcher.UI.ViewModels;

internal partial class AboutViewModel : ObservableObject
{
    private readonly AutoStart _autoStart = null!;
    private readonly ILogger<AboutViewModel> _logger = null!;

    public string AppName => "AppSwitcher";
    public string AppVersion => "Version " + Utils.AppVersion.Version;
    public string DotNetVersion => ".NET " + Environment.Version;
    public ImageSource? AppIcon { get; }

    [ObservableProperty]
    private bool _launchAtStartup;

    protected AboutViewModel()
    {
    }

    public AboutViewModel(AutoStart autoStart, IconExtractor iconExtractor, ILogger<AboutViewModel> logger)
    {
        _autoStart = autoStart;
        _logger = logger;
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
            Process.Start("explorer.exe", folder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open log folder: {Folder}", folder);
        }
    }

    [RelayCommand]
    private void OpenWebsite()
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://www.app-switcher.com") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open website");
        }
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
