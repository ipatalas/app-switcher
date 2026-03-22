# AppSwitcher

AppSwitcher is a simple Windows application that allows you to switch between running applications using a hotkey.
Instead of using Alt-Tab you can have predefined hotkeys to switch to particular applications.
I've been using similar application ([rCmd](https://lowtechguys.com/rcmd/)) for Mac recently and this has completely changed the way I switch between applications.
I couldn't find any similar application for Windows, so I've decided to create one.

## Requirements

- Windows 10 or newer
- .NET 8(or higher) Desktop Runtime — required unless using a self-contained release (see [Installation](#installation))

## Installation

### Choosing a release

| Release | Requires .NET installed | Installer |
|---|---|---|
| **Installer** | Yes | Yes |
| **Installer self-contained** | No | Yes |
| **Portable** | Yes | No |
| **Portable self-contained** | No | No |

- **Installer** — recommended for most users. Runs a setup wizard and adds AppSwitcher to Programs. Requires [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
- **Installer self-contained** — same as above but bundles the .NET runtime, so no separate install is needed. Larger download (~45 MB vs ~5 MB).
- **Portable** — just a ZIP, no setup. Extract anywhere and run `AppSwitcher.exe`. Requires [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
- **Portable self-contained** — ZIP with the .NET runtime bundled. Extract and run, no dependencies.

### Steps

1. Download the latest release from the [releases page](https://github.com/ipatalas/app-switcher/releases)
2. Run the installer or unzip the archive
3. Run `AppSwitcher.exe`

The application will start in the background. You can see it by checking the icon in the system tray.
Open the Settings window from the tray icon to configure your hotkeys and preferences.

> Note: Configured modifier will be suppressed and won't be passed to the active application.
> This is to avoid any side effects in foreground application. However, if modifier is pressed without any letter key it will be passed to the active application as usual.
> That means if your modifier is `Apps` key you can still use it to open the context menu by pressing it without any letter key.

## How to use

Each hotkey can be defined as a combination of a modifier key (Ctrl, Alt, [Apps](https://en.wikipedia.org/wiki/Menu_key)) and a letter key (A-Z).
For example, you can switch to Notepad by pressing `Apps+N` or to Windows Terminal by pressing `Apps+T`.
If a hotkey is pressed and the application is not running it will NOT be started by default.
Pressing a hotkey for an application that is already running will bring it to the front. Also, if the application is minimized it will be restored.
Every hotkey that is defined in the configuration will be available system-wide, will be suppressed and won't reach foreground application.
That means that if you have a hotkey defined as `Ctrl+V` and you press it while working in Notepad the hotkey won't be passed to Notepad, hence nothing will be pasted.
Bear that in mind when defining hotkeys.

### CLI commands

Simply run the following command:
```shell
AppSwitcher.exe <command>
```

Available commands:

| Command               | Description                                                            |
|-----------------------|------------------------------------------------------------------------|
| `--log-all-windows`   | Log all windows to the log file, might be useful when troubleshooting  |
| `--enable-auto-start` | Enable application auto start on system boot                           |
| `--debug`             | Enable debug logging, useful for troubleshooting, do not use otherwise |
| `--trace`             | Enable trace logging, useful for troubleshooting, do not use otherwise |
| `--help`              | Show the help message with available commands                          |


## Features

### Cycle mode

By default, the application will be brought to the front if it's running but subsequently pressing the hotkey will do nothing.
You can change this behavior via the cycle mode setting per each application.
Available options are:

- `NextApp` - the default behavior, cycles between applications assigned to the same letter
- `Hide` - if the application is currently active it will be hidden - this is useful to quickly toggle an application
- `NextWindow` - if the application is currently active the next window of the same application will be brought to the front

### Start if not running

If you want to start the application if it's not running you can enable the `Start if not running` option per each application configuration.

## Known issues

Configured modifier key down and up events are being suppressed to avoid side effects (e.g. if your modifier is `Apps` a context menu would open when switching between apps).
That has a downside that if your have a complex shortcut with multiple modifiers (e.g. `Ctrl+Shift+T`) and one of these modifiers is configured in AppSwitcher (e.g. `Ctrl`) the shortcut won't work at all because `Ctrl` key down event will be suppressed and the application will never receive the full shortcut (`Ctrl+Shift+T`).
Bear that in mind when choosing your modifier. My recommendation is to use `Apps` key as a modifier because it's not used as a modifier in general so there should be no conflict here. 
Pressing `Apps` key without any letter key will still work as expected and open the context menu.