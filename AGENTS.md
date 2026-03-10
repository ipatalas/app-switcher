# AGENTS.md - AppSwitcher

AppSwitcher is a Windows desktop application (.NET 8.0, C# 13) that enables hotkey-based window switching.

## Build Commands

```bash
# Build
dotnet build

# Build (Release)
dotnet build -c Release

# Format code
dotnet format

# Publish (framework-dependent)
msbuild /t:Publish /p:PublishProfile=FolderProfile /p:Configuration=Release

# Publish (self-contained, single file)
msbuild /t:Publish /p:PublishProfile=SelfContainedProfile /p:Configuration=Release
```

## Additional Documentation

- [C# Code Conventions](.agents/csharp-conventions.md) - Naming, formatting, language features
- [Architecture & Patterns](.agents/architecture.md) - DI, project structure, logging
- [WPF Patterns](.agents/wpf-patterns.md) - MVVM, UserControls, Dependency Properties, XAML
- [Windows API](.agents/windows-api.md) - P/Invoke patterns and platform-specific code
- [Testing Conventions](.agents/testing-conventions.md) - xUnit, AwesomeAssertions, fakes, file layout
