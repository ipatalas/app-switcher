# Architecture & Patterns

## Project Structure

```
AppSwitcher/
├── Configuration/       # Configuration loading, validation, hot-reload
├── Extensions/          # Extension methods
├── UI/
│   ├── Controls/        # Custom WPF controls
│   ├── Pages/           # WPF pages
│   ├── ViewModels/      # MVVM ViewModels (CommunityToolkit.Mvvm)
│   └── Windows/         # Main windows
├── Utils/               # Utility classes
├── WindowDiscovery/     # Window enumeration via Windows API
├── Hook.cs              # Global keyboard hook management
├── Switcher.cs          # Core window switching logic
└── ServicesConfiguration.cs  # DI container setup
```

## MVVM Pattern

Use CommunityToolkit.Mvvm source generators for ViewModels:

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(ModifierKeyDisplay))]
private Key _modifierKey = Key.Apps;

[RelayCommand]
private void RemoveApplication(ApplicationShortcutViewModel application)
{
    Applications.Remove(application);
}
```

## Dependency Injection

Use Microsoft.Extensions.DependencyInjection:

```csharp
services.AddSingleton<ConfigurationManager>();
services.AddSingleton<Switcher>();
```

## Error Handling & Logging

Use structured logging with NLog/Microsoft.Extensions.Logging:

```csharp
// Catch and log unexpected exceptions
try
{
    // operation
}
catch (Exception ex)
{
    logger.LogError(ex, "Unexpected error handling key press");
}

// Structured logging with typed loggers
_logger.LogInformation("Starting {ProcessName}", appConfig.NormalizedProcessName);
_logger.LogWarning("{ProcessName} process not found", appConfig.NormalizedProcessName);
_logger.LogDebug("Switching to {ProcessName}", appConfig.NormalizedProcessName);
```

## Configuration Files

- Runtime config: `config.json` (hot-reloadable)
- JSON schema: `config.schema.json`
- Logging config: `nlog.config`

## Key Dependencies

- **CommunityToolkit.Mvvm**: MVVM framework with source generators
- **WPF-UI**: Modern WPF UI framework
- **KeyboardHookLite**: Global keyboard hook
- **NLog**: Logging implementation
- **Microsoft.Windows.CsWin32**: P/Invoke source generator
