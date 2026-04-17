# Architecture & Patterns

## Project Structure

```
AppSwitcher/
├── Configuration/       # Configuration loading, validation, hot-reload
├── Extensions/          # Extension methods
├── Input/               # Keyboard hook, key state machine, dynamic mode
├── Overlay/             # Overlay window coordination (AppOverlayService, timer)
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

## UI Architecture

This project uses WPF with MVVM. See [WPF Patterns](wpf-patterns.md) for detailed guidance on ViewModels, UserControls, Dependency Properties, and XAML best practices.

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

## Singleton Constraint

`Hook` is registered as `AddSingleton`. Any service injected into `Hook` must also be a singleton (or stateless). Injecting a scoped or transient service will silently capture it for the application lifetime.

## LiteDB / SettingsDocument

`SettingsDocument` handles missing LiteDB fields via C# property initializers — no migration needed when adding new `bool` fields. Existing records get the initializer default (`false`). Only use `SeedDefaults` in `ConfigurationService` when a new-user default differs from `false`.
