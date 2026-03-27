# C# Code Conventions

## Formatting

**Note**: Most formatting is enforced via `.editorconfig`. Follow these additional guidelines:

- **Indentation**: 4 spaces (no tabs)
- **Line endings**: CRLF
- **Final newline**: None
- **Braces**: Opening brace on new line (Allman style)
- **Braces always required**: Every `if`, `else`, `for`, `foreach`, `while`, `do`, `using`, and `lock` body **must** use braces, even for single-statement bodies. Never omit braces.

```csharp
// Correct
if (condition)
{
    DoSomething();
}
else
{
    DoOtherThing();
}

// WRONG - never omit braces
if (condition)
    DoSomething();
```

## Namespaces

Use file-scoped namespaces (enforced as warning):

```csharp
// Correct
namespace AppSwitcher.Configuration;

// Incorrect
namespace AppSwitcher.Configuration
{
    ...
}
```

## Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Private fields | _camelCase (underscore prefix) | `_logger`, `_config` |
| Types | PascalCase | `ConfigurationManager` |
| Interfaces | I-prefix PascalCase | `INavigationViewPageProvider` |
| Properties/Methods | PascalCase | `GetConfiguration()` |
| Local variables | camelCase | `matchingWindows` |
| Parameters | camelCase | `appConfig` |

**IMPORTANT**: Private field underscore prefix is enforced as ERROR.

## Type Declarations

- Use `var` everywhere when type is apparent
- Prefer primary constructors:

```csharp
// Preferred
internal class Hook(ILogger<Hook> logger, Switcher switcher) : IDisposable

// Also acceptable for complex classes
public SettingsViewModel(ConfigurationManager configurationManager, IconExtractor iconExtractor)
```

- Use records for immutable data types:

```csharp
public record Configuration(Key Modifier, IReadOnlyList<ApplicationConfiguration> Applications);
```

- Use collection expressions:

```csharp
private readonly List<HWND> _nextWindows = [];
INPUT[] inputs = [input];
```

## Null Handling

- Nullable reference types are enabled project-wide
- Use `ArgumentNullException.ThrowIfNull()` for parameter validation
- Prefer `is null` / `is not null`:

```csharp
if (window is null) { ... }
if (appConfig is not null) { ... }
```

## Pattern Matching

Use pattern matching for range checks and type tests:

```csharp
// Range checks
if (key is >= Key.A and <= Key.Z)

// Null checks
if (appConfig is not null)
```

## Expression-Bodied Members

- Use for simple properties and accessors
- Use for simple one-liner methods
- Avoid for constructors and complex methods

```csharp
// Good - expression body for simple property
public string ModifierKeyDisplay => ModifierKey.ToString();

// Good - expression body for simple method
private bool IsLetter(Key key) => key is >= Key.A and <= Key.Z;
```

## Imports

- No separation of import directive groups
- System namespaces are NOT sorted first
- Using directives go outside namespace
