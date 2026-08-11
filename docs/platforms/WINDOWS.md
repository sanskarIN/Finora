# Windows Platform Guide

This document describes the current Windows target in Finora 0.2.0.

## Target

- Target framework: `net10.0-windows10.0.19041.0`
- Minimum supported platform version: Windows 10.0.19041.0
- Application ID from MAUI project: `in.sanskar.finora`
- Package identity name: `Finora`
- Current manifest publisher placeholder/identity: `CN=Sanskar`
- Package version: `0.2.0.0`
- Application display version: 0.2.0
- Build version: 2

The final package publisher/signing identity must be configured to match the actual release certificate/store configuration. Source manifest identity is not signing evidence.

## Build

On a supported Windows .NET/MAUI host:

```powershell
dotnet workload restore src/Finora.App/Finora.App.csproj
dotnet restore src/Finora.App/Finora.App.csproj
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-windows10.0.19041.0 -c Release --no-restore
```

Package/signing credentials must stay outside source control.

## Package manifest

Current `Package.appxmanifest` contains:

- identity `Finora`;
- publisher `CN=Sanskar`;
- version `0.2.0.0`;
- `Windows.Universal` target family;
- explicit `Windows.Desktop` target family;
- min version 10.0.19041.0;
- `MaxVersionTested` 10.0.26100.0;
- application display name/description metadata.

MAUI/package build can replace placeholder logo tokens with generated assets. Validate actual packaged assets instead of assuming the manifest token itself is the final store graphic.

## Windows Hello

`PlatformBiometricService` includes Windows Hello behavior on supported Windows builds.

Finora treats Hello as an optional factor with PIN fallback, not a replacement for local PIN configuration.

Validate:

- Hello unavailable;
- not configured;
- success;
- cancel;
- provider failure;
- PIN fallback;
- app remains locked after unsuccessful Hello.

User-facing errors must remain stable/generic instead of surfacing raw provider details.

## Local notifications

Windows local reminders use scheduled toast behavior through the platform gateway.

Packaged identity can affect notification support. Validate in the final package, not only unpackaged/debug runs.

Test:

- permission/capability behavior;
- schedule;
- cancel;
- dedupe replacement;
- expired/stale reconciliation;
- generic notification text;
- behavior after app restart/package update.

## Sensitive-screen protection

Finora uses supported Windows display-affinity behavior where available.

This is capability-based, not universal screenshot protection. Validate the exact target Windows versions and document capture paths that remain possible.

## File picker/share/export

Validate under packaged permissions:

- CSV import picker;
- receipt picker;
- receipt open;
- CSV/PDF export;
- encrypted backup save/share;
- encrypted backup open/restore;
- cancellation;
- inaccessible path/file;
- large/low-disk behavior.

## App-private storage

Finora stores durable data through `FileSystem.AppDataDirectory` and cache share copies/diagnostics under `FileSystem.CacheDirectory`.

Test packaged upgrade to ensure:

- SQLite database preserved/migrated;
- receipt tree preserved;
- secure-storage/Preferences behavior remains compatible;
- restore recovery artifacts resolve correctly.

## NTFS/reparse-point safety

Windows path comparison is case-insensitive. Current path-safety logic accounts for platform comparison behavior and rejects reparse/symbolic-link traversal in protected app-private file workflows.

Use an NTFS test environment with reparse-point support to validate:

- attachment open/write;
- backup attachment read;
- restore staging/rollback;
- recovery journal paths;
- diagnostics log path;
- temporary artifact cleanup.

Do not run destructive link tests against real user data.

## Adaptive desktop UI

Windows is treated as desktop navigation by device idiom. Flyout primary sections replace the phone bottom-tab hierarchy.

Validate:

- narrow window;
- wide window;
- resize around the adaptive threshold;
- section preservation when hierarchy changes;
- minimum usable size;
- high DPI;
- multi-monitor;
- keyboard-only navigation;
- focus visibility/order.

## Accessibility

Validate:

- Narrator;
- keyboard operation;
- high contrast;
- large text/larger interface;
- semantic labels;
- report text/table equivalents;
- privacy/lock controls;
- reduced motion;
- light/dark/system appearance.

## Privacy-mode test

Enable privacy/hide-on-launch and verify passive amounts and report chart magnitude are hidden across all finance surfaces documented in the native validation matrix.

## Release/package validation

Before release:

- final MSIX/package build passes;
- identity/publisher match actual signing/store setup;
- version matches source;
- icons/visual assets resolve correctly;
- package signs with external secure credentials;
- install/update/uninstall tested;
- Windows Hello tested;
- scheduled toast tested under package identity;
- capture-protection behavior documented;
- file flows tested;
- migration/backup/restore tested;
- Narrator/keyboard/high-DPI tested;
- store privacy declarations use synthetic screenshots/data.

See [Store Readiness](../releases/STORE_READINESS.md).