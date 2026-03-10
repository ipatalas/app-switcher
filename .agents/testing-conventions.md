# Testing Conventions

## Project & Framework

- Test project: `AppSwitcher.Tests`
- Framework: **xUnit 2.x**
- Assertions: **AwesomeAssertions** (`result.Should()...`)
- Logger stub: `NullLogger<T>.Instance` from `Microsoft.Extensions.Logging.Abstractions`

## File & Namespace Layout

Test files mirror the source directory structure exactly:

| Source | Test |
|--------|------|
| `Configuration/Foo.cs` | `AppSwitcher.Tests/Configuration/FooTests.cs` |
| `Configuration/Storage/Bar.cs` | `AppSwitcher.Tests/Configuration/Storage/BarTests.cs` |
| `Utils/Baz.cs` | `AppSwitcher.Tests/Utils/BazTests.cs` |
| `UI/ViewModels/Qux.cs` | `AppSwitcher.Tests/UI/ViewModels/QuxTests.cs` |

Namespace follows the folder: `namespace AppSwitcher.Tests.Configuration;`, `namespace AppSwitcher.Tests.Utils;`, etc.

## Naming

- Test classes: `{ClassUnderTest}Tests`
- Test methods: `{Method}_{ExpectedOutcome}_{Condition}` — e.g. `FindExecutablePath_ReturnsNull_WhenProcessNotRunning`
- SUT field: `_sut`

## Test Structure

- Follow **AAA** (Arrange / Act / Assert) with a blank line separating each phase
- One logical assertion per test; use `And` chaining only when asserting the same object
- Use `[Theory]` + `[InlineData]` to collapse equivalent cases
- Implement `IDisposable` (or `IAsyncDisposable`) on the test class for any setup that needs teardown

## Assertions

- Prefer `BeEquivalentTo(expected)` over individual property assertions for mapping/conversion tests — it catches missing fields automatically when the type grows
- Use `.Be()` for record equality (value semantics) and simple scalar checks
- Use `Should().NotThrow()` / `Should().ThrowExactly<T>()` for exception path tests

## Internal Types

- `internal enum` values used as `[InlineData]` parameters must be cast to their underlying type (`uint`) in the attribute and cast back inside the method body (CS0051 / xUnit1000 enforcement)
- Type alias to avoid `Configuration.Configuration` ambiguity: `using AppConfig = AppSwitcher.Configuration.Configuration;`

## Fakes & Dependencies

- Prefer hand-written fakes over mocking libraries for simple interfaces (single fake class per test file, sealed, returns configurable fields)
- Extract an interface (e.g. `IProcessPathExtractor`) when a concrete dependency contains P/Invoke or registry calls that must be seeded; register both the interface and the concrete type in `ServicesConfiguration`
- LiteDB can be used in-memory (`new LiteDatabase(":memory:")`) — no interface needed for `ConfigurationService` tests
- Avoid testing classes that depend solely on P/Invoke, live process state, or WPF UI components without introducing abstractions first