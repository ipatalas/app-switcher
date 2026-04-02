# Change Log

## [1.8.1] - 2026-04-02
- Change modifier suppression - only suppress when configured modifier can have side effects
- Add new warning when switching to elevated process from non-elevated AppSwitcher
- Fix when sometimes elevated app was not focused correctly when switching to it from non-elevated AppSwitcher
- Fix border pulse effect not always working (Win11 only)

## [1.8.0] - 2026-03-28
- Add new overlay window when modifier key is held
- Delete Startup shortcut when AppSwitcher is uninstalled
- Change defaults:
  - Start if not running: false -> true
  - ModifierIdleTimer: 2000 ms -> 0 ms (most people don't need it at all and it could have annoying side effects)
- Fix settings file not gracefully closed upon application exit

## [1.7.4] - 2026-03-24
- Fix migration failing on empty database

## [1.7.3] - 2026-03-23
- Fix support for Packaged Apps (e.g. Windows Terminal) - Windows 10 version 20H1 (May 2020 Update, build 19041) or newer is required
- Improve hotkey assignment appearance ("breathing" animation when assigning hotkey)
- Change default cycle mode to NextWindow
- Minor fixes

## [1.7.2] - 2026-03-13
- Add installer (both installer and portable versions) to the release assets
- Fix Settings window not showing up in foreground when opened with double-click on the tray icon
- Other minor fixes and improvements

## [1.7.1] - 2026-03-11
- Change mutex to be checked at the very beginning of application startup

## [1.7.0] - 2026-03-10
- Add UI for settings management
- Add new NextApp cycle mode to have single letter cycle between different apps
- Add subtle effect when switching windows to make it more clear which window is being switched to (Win11 only)
- Change storage from JSON to LiteDB
- Change allowed modifiers (Left Ctrl, Left Alt, Left Shift, Left Win, Right Ctrl, Right Alt, Apps/Menu, Right Shift)

## [1.6.0] - 2026-02-09
- Allow LeftCtrl and LeftAlt as modifiers as well
- Fix context menu not working when only pressing the Apps key
- Fix [#4](https://github.com/ipatalas/app-switcher/issues/4) by introducing a timer as backup when key up is not detected
- Add --debug and --trace CLI switches to enable more verbose logging for troubleshooting purposes

## [1.5.0] - 2025-07-31
- Reload configuration automatically when file is updated

## [1.4.0] - 2024-08-15
- Add CLI switch to enable application auto start
- Automatically clean log files older than 14 days
- Update application icon :)

## [1.3.0] - 2024-06-28
- Fix null ref when there is no active window
- Fix setting foreground window to work outside debugging session as well :)
- Improve focusable window filtering - [details](https://github.com/ipatalas/app-switcher/commit/23a5d6c)

## [1.2.0] - 2024-06-26
- Add support to start the application if it's not running when the hotkey is pressed
- Change allowed modifiers (Right Ctrl, Right Alt, Right Shift, Apps/Menu)
- Always suppress the modifier key

## [1.1.0] - 2024-06-18
- Add support for different CycleMode(s) - see more in the [README](README.md#cycle-mode)
- Optional trace logging to help with debugging

## [1.0.0] - 2024-06-07
- Initial release
