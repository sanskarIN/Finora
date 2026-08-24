# ChromeOS

Finora supports ChromeOS through two delivery paths rather than a dedicated ChromeOS-native project.

## Android path

Compatible ChromeOS devices can use the existing Android application target (`net10.0-android`) where Android application support is available. This path uses the mature MAUI application and local SQLite persistence.

ChromeOS-specific validation is still required for window resizing, keyboard/mouse input, file pickers, notifications, lifecycle behavior, Android-container storage, backup exclusions, and accessibility.

## Web/PWA path

The Avalonia WebAssembly host can run in a modern ChromeOS browser and exposes an installable web-app manifest. Its finance persistence remains intentionally disabled until Finora's browser-local storage adapter passes the security and parity requirements in [`WEB.md`](WEB.md).

## Release claims

Do not describe ChromeOS as independently native-tested merely because Android or WebAssembly builds compile. A ChromeOS release/support statement must identify which delivery path was tested and on which ChromeOS/Android-container/browser environments.
