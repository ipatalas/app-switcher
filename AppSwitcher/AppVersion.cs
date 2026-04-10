namespace AppSwitcher;
#if !DEBUG
using System.Reflection;
#endif

public static class AppVersion
{
    public static string Version { get; }

    static AppVersion()
    {
#if DEBUG
        Version = "v[DEBUG]";
#else
        var ver = Assembly.GetEntryAssembly()?.GetName().Version!;
        Version = $"v{ver.Major}.{ver.Minor}.{ver.Build}";
#endif
    }
}