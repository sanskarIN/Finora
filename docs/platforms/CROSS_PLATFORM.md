# Finora Cross-Platform Support Matrix

Finora uses two presentation/runtime families so that the finance core can reach the major mobile, desktop, and browser environments without forcing platform-specific APIs into the domain layer.

## Support matrix

| Platform | Primary path | Source/build status | Finance persistence status | Release-parity status |
|---|---|---|---|---|
| Android | .NET MAUI (`net10.0-android`) | Existing native target | Local SQLite/EF Core | Existing MAUI surface; device/store validation still required |
| iPhone / iPad | .NET MAUI (`net10.0-ios`) | Existing native target | Local SQLite/EF Core | Existing MAUI surface; Apple signing/device validation still required |
| Windows 10/11 | .NET MAUI + Avalonia desktop | Existing MAUI target plus universal desktop host | Local SQLite/EF Core | MAUI remains the primary release path; Avalonia host expands desktop portability |
| macOS | .NET MAUI Mac Catalyst + Avalonia desktop | Existing Mac Catalyst target plus universal desktop host | Local SQLite/EF Core | Native release validation/signing still required |
| Linux | Avalonia desktop (`net10.0`) | Universal desktop host added | Local SQLite/EF Core foundation; landing UI does not read finance rows | Runtime/storage foundation present; app-lock/privacy and full MAUI-screen UI parity remain work |
| Web / modern browsers | Avalonia WebAssembly (`net10.0-browser`) | Browser host added | **Disabled by design in this phase** | UI/build path exists; secure browser-local finance persistence and full feature parity remain blockers |
| ChromeOS | Android package and/or Web/PWA path | Delivery paths present | Android uses SQLite; Web follows browser boundary | Dedicated ChromeOS-native validation remains required |

## Architecture rule

The following projects remain platform-neutral and are shared by all presentation paths:

- `Finora.Shared`
- `Finora.Domain`
- `Finora.Application`

`Finora.Infrastructure` is reusable by native desktop hosts where its SQLite/filesystem assumptions are valid. Platform hosts must own platform-specific behavior such as filesystem roots, notifications, biometrics, secure storage, screenshot/sensitive-screen controls, and browser persistence.

## Presentation paths

### MAUI path

`src/Finora.App/` remains the mature application surface for Android, iOS/iPadOS, Mac Catalyst, and Windows. Existing finance workflows, privacy controls, backup/restore, reports, attachments, notifications, and platform integrations remain here while the universal UI is expanded.

### Universal Avalonia path

- `src/Finora.Universal/` contains the platform-neutral Avalonia application/view layer and the `IUniversalRuntime` capability boundary.
- `src/Finora.Universal.Desktop/` hosts the UI on Linux, Windows, and macOS and initializes the existing local SQLite finance-store/storage boundary in the operating system's local application-data directory. The current landing surface intentionally does not query or display finance rows until app-lock, privacy-mode, and feature parity are implemented and validated for that host.
- `src/Finora.Universal.Browser/` hosts the same universal view in WebAssembly and provides an installable web-app manifest.

## Why browser persistence is gated

Finora must not silently replace native SQLite with an unvalidated browser store. Before browser finance persistence can be enabled, the browser adapter must demonstrate:

1. minor-unit and currency correctness;
2. transaction/account/category/budget/savings/recurrence parity;
3. durable schema migration and crash/interruption handling;
4. encrypted backup export and authenticated restore parity;
5. attachment/receipt storage rules appropriate to browser storage;
6. integrity checks and deterministic recovery;
7. storage-quota and eviction behavior that is clearly communicated to users;
8. privacy-safe clearing/reset behavior;
9. offline behavior without a required Finora account or cloud service;
10. automated browser tests across the supported browser matrix.

Until those conditions are met, the WebAssembly host reports persistent finance storage as unavailable. This is an intentional safety boundary, not an accidental missing dependency.

## CI coverage

`.github/workflows/cross-platform.yml` builds the universal desktop host on:

- Ubuntu;
- Windows;
- macOS.

It also restores/builds the WebAssembly host with the .NET WebAssembly workload. Existing `ci.yml` continues to validate the MAUI platform targets and the shared test suites.

## Definition of "supported"

A project compiling for a target is only the first level of support. A platform should be called release-ready only after its packaging, signing, upgrade, accessibility, privacy, storage, backup/restore, file picker/share behavior, notifications (where applicable), time-zone behavior, and platform-store policy have been validated on real supported environments.
