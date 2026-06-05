# Changelog

## 1.0.1

- Fixes a Windows-key release issue that could make the system behave as if `Win` was still pressed.
- Makes the watchdog scheduled task opt-in instead of installing it by default.
- Keeps release artifacts free of debug symbols.

## 1.0.0

- Initial open-source release.
- Blocks `Win+Tab` with a low-level keyboard hook.
- Adds startup shortcut support.
- Adds watchdog scheduled task support.
- Adds project branding, icon, documentation, and CI.
