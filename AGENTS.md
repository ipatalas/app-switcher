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

**Note**: No test framework is currently configured.

## Additional Documentation

- [C# Code Conventions](.agents/csharp-conventions.md) - Naming, formatting, language features
- [Architecture & Patterns](.agents/architecture.md) - MVVM, DI, project structure, logging
- [Windows API](.agents/windows-api.md) - P/Invoke patterns and platform-specific code
