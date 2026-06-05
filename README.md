# WinTabGuard

WinTabGuard is a tiny Windows utility that blocks the `Win+Tab` Task View shortcut while leaving the Task View button and the rest of Windows alone.

It is built for people who trigger `Win+Tab` accidentally and want that one shortcut gone without installing a heavy keyboard remapper.

## Features

- Blocks `Win+Tab` with a low-level Windows keyboard hook.
- Keeps tracking the Windows key state internally, which makes the block more reliable than a single instant key-state check.
- Blocks the `Tab` event only, so the Windows key release is still delivered normally.
- Runs silently in the background.
- Can install an optional watchdog scheduled task that restarts the app if it exits.
- Writes a small local log to `%LOCALAPPDATA%\WinTabGuard\WinTabGuard.log`.

## Install

Build and install from PowerShell:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-Release.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Install-WinTabGuard.ps1
```

The installer copies the executable to:

```text
%LOCALAPPDATA%\WinTabGuard\WinTabGuard.exe
```

It also creates a Startup shortcut so WinTabGuard starts when you sign in.

To also install the optional watchdog task:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Install-WinTabGuard.ps1 -EnableWatchdog
```

## Uninstall

```powershell
.\scripts\Uninstall-WinTabGuard.ps1
```

## Build

Requirements:

- Windows
- .NET 8 SDK

```powershell
dotnet build .\WinTabGuard.sln
dotnet publish .\src\WinTabGuard\WinTabGuard.csproj -c Release -r win-x64 -o .\artifacts\publish
```

## How it works

WinTabGuard installs a `WH_KEYBOARD_LL` hook with `SetWindowsHookEx`. When it sees `Tab` while either Windows key is pressed, it returns `1` from the hook callback, which tells Windows that the key event has been handled and should not continue to Task View.

The app also tracks left and right Windows key down/up events itself. That avoids relying only on `GetAsyncKeyState`, which can be timing-sensitive for shell shortcuts.

## Limitations

Windows shell shortcuts can be sensitive to timing, integrity level, and foreground context. WinTabGuard is intentionally small and user-mode only; it does not install drivers or modify system files.

## License

MIT
