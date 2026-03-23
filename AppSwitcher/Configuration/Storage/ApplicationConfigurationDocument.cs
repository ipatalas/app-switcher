using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace AppSwitcher.Configuration.Storage;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Used for LiteDB serialization")]
internal class ApplicationConfigurationDocument
{
    public Key Key { get; init; }

    public string ProcessPath { get; init; } = string.Empty;

    public CycleMode CycleMode { get; init; }

    public bool StartIfNotRunning { get; init; }

    public ApplicationType Type { get; init; } = ApplicationType.Win32;

    public string? Aumid { get; init; }

    public ApplicationConfiguration ToApplicationConfiguration() =>
        new(Key, ProcessPath, CycleMode, StartIfNotRunning, Type, Aumid);

    public static ApplicationConfigurationDocument FromApplicationConfiguration(ApplicationConfiguration config) =>
        new()
        {
            Key = config.Key,
            ProcessPath = config.ProcessPath,
            CycleMode = config.CycleMode,
            StartIfNotRunning = config.StartIfNotRunning,
            Type = config.Type,
            Aumid = config.Aumid
        };
}