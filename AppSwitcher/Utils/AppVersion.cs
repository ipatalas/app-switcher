namespace AppSwitcher.Utils;
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
        Version = "v" + Assembly.GetEntryAssembly()?.GetName().Version;
#endif
    }
}