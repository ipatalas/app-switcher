# AppSwitcher

AppSwitcher is a simple Windows application that allows you to switch between running applications using a hotkey. 
Instead of using Alt-Tab you can have predefined hotkeys to switch to particular applications. 
I've been using similar application for Mac recently and this has changed the way I work with applications. 
I couldn't find any similar application for Windows so I've decided to create one. 

## Requirements

- Windows 10 or newer (very likely it will work on Windows 7 and 8 as well)
- .NET 8 Desktop Runtime ([link](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)) is recommended but there is also a self-contained version available in the releases

## Installation

1. Download the latest release from the [releases page](https://github.com/ipatalas/app-switcher/releases) 
2. Unzip the archive
3. Run `AppSwitcher.exe`

The application will start in the background. You can see it by checking the icon in the system tray.
For the time being there is no UI available. You can configure the hotkeys by editing the `config.json` file directly.
You can start by copying the `config.json.example` file and renaming it to `config.json`. 
It contains a few examples of hotkeys that you can use.

## How to use

Each hotkey can be defined as a combination of a modifier key (Ctrl, Alt, [Menu](https://en.wikipedia.org/wiki/Menu_key)) and a letter key (A-Z).
For example in the default configuration you can switch to Notepad by pressing `Menu+N` or to Windows Terminal by pressing `Menu+T`.
If a hotkey is pressed and the application is not running it will NOT be started (it's on the roadmap). 
Pressing a hotkey for an application that is already running will bring it to the front. Also if the application is minimized it will be restored.
Every hotkey that is defined in the configuration file will be available system-wide and will be surpressed and won't reach active application.
That means that if you have a hotkey defined as `Ctrl+V` and you press it while working in Notepad the hotkey won't be passed to Notepad, hence nothing will be pasted.
Bear that in mind when defining hotkeys.

## Features

### Cycle mode

By default the application will be brought to the front if it's running but subsequently pressing the hotkey will do nothing.
You can change this behavior by setting the `cycleMode` option in the configuration file per each hotkey.
Available options are:

- `Default` - the default behavior so nothing will happen
- `Hide` - if the application is currently active it will be hidden - this is useful to quickly toggle an application
- `NextWindow` - if the application is currently active the next window of the same application will be brought to the front

## Roadmap:

- Add support for multiple modifiers (eg. Ctrl-Alt-A)
- ~~Add option to show/hide for specific apps~~
- ~~Add option to cycle between different windows of the same app~~
- Add option to run the app if it's not running when the hotkey is pressed
- Add option to have a single hotkey to cycle between different apps (CycleMode.NextApp?)
- Installer + autostart
- ~~Trace logging to help with debugging (log all windows)~~