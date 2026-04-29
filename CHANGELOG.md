# Change Log

## [1.10.0] - 2026-04-29
- Add Dynamic Mode - automatically assign letters to running apps
- Add statistics tracking and dashboard to show most frequently used apps and other insights
- Add auto-dismissal indicator in elevated warning overlay
- Add special handling for Windows Sandbox which steals keyboard events when active
- Change app display to use Display Name instead of process name
- Fix bug when starting a process that ends up elevated; modifier state got stuck and warning overlay was not shown
- Fix bug when overlay window wasn't always highlighting current app
- Fix AppSwitcher main window showing up in the Alt+Tab list
- Other minor fixes and improvements

## [1.9.0] - 2026-04-11
- Add Peek Mode - hold the modifier key to peek at the target window, release to switch back
- Add new modifier: Caps Lock
- Fix low contrast in overlay window
- Fix overlay window stealing focus
- Fix a rare race condition where the overlay window could appear and not disappear after releasing the keys
- Other minor fixes

## [1.8.2] - 2026-04-06
- Fix a bug where unsaved setting changes were discarded after switching focus away from and back to the Settings window
- Remove Modifier Idle Timer - legacy feature, no longer needed, there's a better way to handle elevated target window
- Moved Launch at startup setting to the General page
- Fix random frozen UI when adding new application in the settings
- Fix contrast issues for selected/mouse over items in add application flyout
- Fix bug when it was possible to add duplicate app using Browse for file option
- Fix bug when it was possible to add non-executable file using Browse for file option
- Fix a bug where the app could freeze when the Settings window was open and the user exited via the tray menu
- Other minor fixes and improvements

## [1.8.1] - 2026-04-02
- Change modifier suppression - only suppress when configured modifier can have side effects
- Add new warning when switching to elevated process from non-elevated AppSwitcher
- Fix an issue where elevated apps were sometimes not focused correctly when switching from a non-elevated AppSwitcher instance
- Fix border pulse effect not always working (Win11 only)

## [1.8.0] - 2026-03-28
- Add new overlay window when modifier key is held
- Delete Startup shortcut when AppSwitcher is uninstalled
- Change defaults:
  - Start if not running: false → true
  - ModifierIdleTimer: 2000 ms → 0 ms (most people don't need it at all, and it could have annoying side effects)
- Fix settings file not gracefully closed upon application exit

## [1.7.4] - 2026-03-24
- Fix migration failure on an empty database

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
- Allow Left Ctrl and Left Alt as modifiers as well
- Fix context menu not working when only pressing the Apps key
- Fix [#4](https://github.com/ipatalas/app-switcher/issues/4) by introducing a timer as backup when key up is not detected
- Add --debug and --trace CLI switches to enable more verbose logging for troubleshooting purposes

## [1.5.0] - 2025-07-31
- Reload configuration automatically when the file is updated

## [1.4.0] - 2024-08-15
- Add CLI switch to enable application auto-start
- Automatically clean log files older than 14 days
- Update application icon :)

## [1.3.0] - 2024-06-28
- Fix null ref when there is no active window
- Fix foreground window activation outside debug sessions
- Improve focusable window filtering - [details](https://github.com/ipatalas/app-switcher/commit/23a5d6c)

## [1.2.0] - 2024-06-26
- Add support to start the application if it's not running when the hotkey is pressed
- Change allowed modifiers (Right Ctrl, Right Alt, Right Shift, Apps/Menu)
- Always suppress the modifier key

## [1.1.0] - 2024-06-18
- Add support for different CycleMode(s) - see more in the [docs](https://app-switcher.com/docs/configuration/cycle-modes/)
- Optional trace logging to help with debugging

## [1.0.0] - 2024-06-07
- Initial release
