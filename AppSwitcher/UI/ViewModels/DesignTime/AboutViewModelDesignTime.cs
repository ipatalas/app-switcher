namespace AppSwitcher.UI.ViewModels.DesignTime;

internal class AboutViewModelDesignTime
{
    public bool LaunchAtStartup { get; set; } = true;

    public string AppName => "AppSwitcher";
    public string AppVersion => "Version 1.0.0";
    public string AppWebsite => "www.app-switcher.com";
    public string DotNetVersion => ".NET 6.0";
}
