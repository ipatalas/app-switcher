# AppSwitcher

**Stop hunting. Start teleporting.**

![Windows Support](https://img.shields.io/badge/Windows-10%202004+%20/%2011-blue)

I spend my workdays on macOS and my evenings on Windows. After using [rCmd](https://lowtechguys.com/rcmd/) on Mac, I stopped searching for windows and started using muscle memory to jump to them.

Back on Windows, Alt+Tab felt slow and unpredictable for the same workflow, so I built AppSwitcher.

[**Download Latest Release**](https://app-switcher.com/) | [**Portable (.zip)**](https://github.com/ipatalas/app-switcher/releases)

<details>
<summary>▶ See it in action</summary>
<video src="https://github.com/user-attachments/assets/19c24843-4465-41b0-ac8d-437cccc3430d" controls width="100%"></video>
</details>

---

## 🚀 Why use this?

Alt+Tab is MRU (Most Recently Used), so the order keeps shifting. You have to look, scan, and tap.

AppSwitcher uses static hotkeys:

- `Apps+C` -> always Chrome
- `Apps+V` -> always VS Code
- `Apps+T` -> always Terminal

You stop thinking about where a window is and just jump there.

## ✨ Key features

- **Three Intelligent Cycle Modes:**
    * `NextApp`: Cycle between different apps assigned to the same key.
    * `NextWindow`: Cycle through all open windows of a *single* app (great for multi-instance browsers).
    * `Hide`: Minimize the app if you press the hotkey while it's already focused (the ultimate "toggle").
- **Start if not running:** optional per-app setting to start the app when no matching process is running.
- **Packaged app support:** works with modern Windows packaged apps (for example Windows Terminal) in addition to classic desktop apps.
- **Lightweight desktop app:** native C#/.NET app with a tray-first workflow.

## 🔒 Power user ethics

AppSwitcher uses a system-wide keyboard hook to detect your hotkeys. That is exactly why transparency matters.

- **Open source:** review the code and verify behavior yourself.
- **100% offline:** no telemetry, no auto-updates, no backend service.
- **No admin required by default:** runs in user space.
- **Portable option:** use the ZIP build if you want a no-installer workflow.

> **Windows SmartScreen may appear on first launch.**
> As an independent release, AppSwitcher has not built strong SmartScreen reputation yet. If prompted, click **More info** then **Run anyway**.

---

## 💽 Installation and requirements

### Requirements

- Windows 10 version 2004 (build 19041) or newer, or Windows 11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (or higher) for non-self-contained builds

### Choose a release

| Release | Requires .NET installed | Installer |
|---|---|---|
| **Installer** | Yes | Yes |
| **Installer self-contained** | No | Yes |
| **Portable** | Yes | No |
| **Portable self-contained** | No | No |

- **Installer** - recommended for most users. Runs setup and adds AppSwitcher to Programs.
- **Installer self-contained** - bundles .NET runtime, larger download.
- **Portable** - ZIP only, no setup. Extract and run `AppSwitcher.exe`.
- **Portable self-contained** - ZIP with bundled runtime, no separate dependency.

### Steps

1. Download from [app-switcher.com](https://app-switcher.com/) or [GitHub releases](https://github.com/ipatalas/app-switcher/releases).
2. Run the installer, or extract the ZIP archive.
3. Start `AppSwitcher.exe`.

The app starts in the background. Open Settings from the system tray icon to configure hotkeys.

## ⌨️ Set up your first hotkey

1. Right-click the tray icon and open **Settings**.
2. Add a hotkey and choose the target app.
3. Pick a modifier and a letter key.
4. Optional: enable **Start if not running** for that app.

Tip: the `Menu/Apps` key is often an excellent modifier because it is usually unused as a shortcut modifier.

## ⌘ CLI commands

Run:

```shell
AppSwitcher.exe <command>
```

Available commands:

| Command               | Description                                                            |
|-----------------------|------------------------------------------------------------------------|
| `--log-all-windows`   | Log all windows to the log file, useful when troubleshooting           |
| `--enable-auto-start` | Enable application auto start on system boot                           |
| `--debug`             | Enable debug logging, useful for troubleshooting, do not use otherwise |
| `--trace`             | Enable trace logging, useful for troubleshooting, do not use otherwise |
| `--help`              | Show the help message with available commands                          |

## 🛠 Technical notes and known issues

- Configured modifier down/up events are suppressed to avoid side effects in foreground apps.
- Pressing modifier alone (without letter key) still works as usual. For example, `Apps` alone can still open context menu.
- If you assign common shortcuts (for example `Ctrl+V`) as AppSwitcher hotkeys, those combinations will be intercepted system-wide and will not reach the foreground app.
- Complex shortcuts may conflict. Example: if `Ctrl` is your configured modifier, an app shortcut like `Ctrl+Shift+T` may fail because `Ctrl` is suppressed before the full combo reaches the app.
- Elevated windows limitation (UIPI): when an administrator app is focused, AppSwitcher hotkeys will not be intercepted unless AppSwitcher is also run as Administrator.

Inspired by [rCmd](https://apps.apple.com/app/rcmd-app-switcher/id1596483192) for macOS.