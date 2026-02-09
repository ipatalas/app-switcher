using AppSwitcher.WindowDiscovery;
using Microsoft.Extensions.DependencyInjection;

namespace AppSwitcher.CLI;

internal static class ServiceCollectionExtensions
{
    public static void AddCliHandler(this IServiceCollection services)
    {
        var builder = new CliBuilder();
        builder
            .AddCommand("--log-all-windows", "Log all windows to log file",
                sp => sp.GetRequiredService<WindowHelper>().LogAllWindows())
            .AddCommand("--enable-auto-start", "Add application to system Startup",
                sp =>
                {
                    if (!sp.GetRequiredService<AutoStart>().CreateShortcut())
                    {
                        MessageBox.Show(
                            "There was an error while trying to create auto start shortcut. See log file for details.",
                            "AppSwitcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                })
            .AddOption("--debug", "Enable debug logging", opts => opts.EnableDebugLogging = true)
            .AddOption("--trace", "Enable trace logging", opts => opts.EnableTraceLogging = true);

        services.AddSingleton(builder);
        services.AddSingleton<CliOptions>();
        services.AddTransient<CliHandler>();
    }
}