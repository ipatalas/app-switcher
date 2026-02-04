# Windows API & Platform-Specific Code

## P/Invoke with CsWin32

Use CsWin32 source generator for Windows API calls:

- Define methods in `NativeMethods.txt`
- Access via `PInvoke.*` static methods

```csharp
PInvoke.SetForegroundWindow(hwnd);
PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);
```

## Target Platform

- Windows only (win-x86, win-x64)
- .NET 8.0 Windows Desktop
- C# 13 language version
