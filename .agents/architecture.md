# Architecture & Patterns

## Project Structure

```
AppSwitcher/
├── CLI/                 # Command-line argument handling (CliBuilder, CliHandler, CliOptions)
├── Configuration/       # Configuration loading, validation, hot-reload, LiteDB migrations
├── Extensions/          # Extension methods
├── Input/               # Keyboard hook, key state machine, dynamic mode, Hook.cs
├── Overlay/             # Overlay window coordination (AppOverlayService, WarningOverlayService)
├── Startup/             # AutoStart (Windows Startup shortcut management)
├── Stats/               # Usage statistics: channel-based async collection, LiteDB persistence
│   ├── Migrations/      # Stats DB schema migrations
│   └── Storage/         # LiteDB documents and StatsDbProvider (lazy-loaded)
├── UI/
│   ├── Controls/        # Custom WPF controls
│   ├── Converters/      # Value converters
│   ├── Pages/           # WPF pages
│   ├── ViewModels/      # MVVM ViewModels (CommunityToolkit.Mvvm)
│   │   └── Common/      # Shared VM types: SettingsState, ApplicationShortcutViewModel, etc.
│   └── Windows/         # Main windows
├── WindowDiscovery/     # Window enumeration via Windows API
├── Switcher.cs          # Core window switching logic
└── ServicesConfiguration.cs  # DI container setup
```

## UI Architecture

This project uses WPF with MVVM. See [WPF Patterns](wpf-patterns.md) for detailed guidance on ViewModels, UserControls, Dependency Properties, and XAML best practices.

## Dependency Injection

Use Microsoft.Extensions.DependencyInjection:

```csharp
services.AddSingleton<ConfigurationManager>();
services.AddSingleton<Switcher>(); // session state: next-window cycle, toggle memory, start debounce
services.AddSingleton<StatsService>();
```

### Interface Scanning

Use `AddImplementationsOf<TInterface>()` (defined in `ServicesConfiguration.cs`) to auto-register all implementations of an interface discovered via reflection:

```csharp
services.AddImplementationsOf<IMigration>(ServiceLifetime.Transient);
services.AddImplementationsOf<IStatsMigration>(ServiceLifetime.Transient);
services.AddImplementationsOf<Page>(ServiceLifetime.Transient, registerAsConcreteType: true);
```

### Shared Settings State

`ISettingsState` / `SettingsState` (singleton) is the canonical cross-ViewModel state for the settings UI. All settings ViewModels receive it via DI rather than loading configuration independently.

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
- Portable mode: if a `.portable` file exists next to the executable, both `settings.db` and `stats.db` are stored in the application directory instead of `%APPDATA%\AppSwitcher\`

## Databases

Two LiteDB databases are used:

| File | Purpose | Registration |
|------|---------|-------------|
| `settings.db` | Configuration, app registry (`AppRegistryDocument`) | `LiteDatabase` singleton, opened at startup |
| `stats.db` | Usage statistics (`DailyBucketDocument`) | `StatsDbProvider` singleton — **lazy loaded** (file is not created until stats are first enabled) |

`BsonMapper.Global` is configured once in `SetupDatabases()`: `EnumAsInteger = true` and `DateOnly` serialized as `"yyyy-MM-dd"`. Tests must replicate this in `GlobalFixture`.

## Key Dependencies

- **CommunityToolkit.Mvvm**: MVVM framework with source generators
- **WPF-UI** + **WPF-UI.Tray**: Modern WPF UI framework (system tray support)
- **KeyboardHookLite**: Global keyboard hook
- **NLog**: Logging implementation
- **Microsoft.Windows.CsWin32**: P/Invoke source generator
- **LiteDB**: Embedded NoSQL database (`settings.db` and `stats.db`)
- **gong-wpf-dragdrop**: Drag-and-drop support for the application list
- **JetBrains.Annotations**: `[NotNull]`, `[CanBeNull]` etc. for static analysis hints

## Singleton Constraint

`Hook` is registered as `AddSingleton`. Any service injected into `Hook` must also be a singleton (or stateless). Injecting a scoped or transient service will silently capture it for the application lifetime.

## Stats Subsystem

`Stats/` collects usage events (`SwitchEvent`, `PeekEvent`, `AltTabEvent`) via a bounded `Channel<StatsEvent>` (capacity 1000, drops oldest). Key classes:

- **`StatsService`** (singleton): starts/stops the consumer based on `config.StatsEnabled`; call `Enqueue()` to record events; flushes to `stats.db` via `Flush(reason)`
- **`StatsConsumer`**: single background reader off the channel
- **`AppRegistryCache`** (singleton): in-memory + `settings.db`-backed map of `processName → displayName`; call `TryAdd()` on first encounter, `GetDisplayName()` for lookup
- **`StatsDbProvider`**: lazy factory — call `Exists()` before `Get()` to avoid creating the file prematurely
- **`DailyBucketDocument`**: one LiteDB document per calendar day

## CLI Subsystem

`CLI/` handles command-line arguments parsed before the main window appears. Register commands and options in `CLI/ServiceCollectionExtensions.cs` via `AddCliHandler()`:

```csharp
builder
    .AddCommand("--log-all-windows", "Log all windows to log file", sp => ...)
    .AddOption("--debug", "Enable debug logging", opts => opts.EnableDebugLogging = true);
```

`CliHandler.Handle(args)` returns `true` if a command was executed (suppress normal startup) or `false` to continue. Options (flags and valued) mutate `CliOptions` only; commands receive the full `IServiceProvider`.

## LiteDB / SettingsDocument

`SettingsDocument` handles missing LiteDB fields via C# property initializers — no migration needed when adding new `bool` fields. Existing records get the initializer default (`false`). Only use `SeedDefaults` in `ConfigurationService` when a new-user default differs from `false`.
