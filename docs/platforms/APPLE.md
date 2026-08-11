# iOS and Mac Catalyst Platform Guide

This document describes the current Apple targets in Finora 0.2.0.

## Targets

### iOS

- Target framework: `net10.0-ios`
- Minimum supported platform version declared by project: iOS 15.0
- Application ID: `in.sanskar.finora`
- Display version: 0.2.0
- Build version: 2

### Mac Catalyst

- Target framework: `net10.0-maccatalyst`
- Minimum supported platform version declared by project: 15.0
- Application ID: `in.sanskar.finora`
- Display version: 0.2.0
- Build version: 2

Apple archive/signing requires a supported macOS/Xcode/.NET/MAUI environment.

## Build

iOS:

```bash
dotnet workload restore src/Finora.App/Finora.App.csproj
dotnet restore src/Finora.App/Finora.App.csproj
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-ios -c Release --no-restore
```

Mac Catalyst:

```bash
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-maccatalyst -c Release --no-restore
```

Provisioning profiles, signing certificates, passwords, private keys, and notarization credentials must remain outside source control.

## iOS metadata

Current iOS Info.plist declares iPhone/iPad device families and supported orientations.

`NSFaceIDUsageDescription` states:

> Finora uses Face ID only when you enable biometric unlock for local finance data.

The purpose string must remain aligned with actual behavior and store privacy declarations.

## Mac Catalyst metadata

Current Mac Catalyst Info.plist declares the finance application category and a biometric purpose string explaining that authentication is used only when local biometric unlock is enabled.

## Biometrics / LocalAuthentication

Finora uses Apple local authentication through the platform biometric adapter where supported.

Rules:

- biometrics are optional;
- a Finora PIN remains fallback;
- success unlocks only after platform authentication succeeds;
- cancellation/unavailability/error leaves the app locked;
- provider-specific error text is normalized before ordinary user-facing display;
- biometric preference is not secure standalone entitlement.

Validate on simulator/device/hardware states supported by the release environment.

## Notifications / UserNotifications

Apple local reminders use platform UserNotifications APIs through the platform gateway.

Validate:

- permission request/denial/grant;
- schedule;
- replacement/dedupe;
- cancel;
- stale reconciliation;
- recurring pause/archive cleanup;
- generic privacy-safe notification text;
- behavior after app restart/upgrade.

## File picker/share flows

Validate:

- CSV import picker;
- receipt/document picker;
- receipt open;
- CSV/PDF export;
- encrypted backup save/share;
- encrypted backup open/preview/restore;
- cancellation;
- inaccessible file/provider;
- attachment backup/restore.

Once data is shared/saved to another provider/location, that destination controls retention/security.

## App-private storage

Finora uses `FileSystem.AppDataDirectory` for the local SQLite database, attachments, and transient restore-recovery state.

Cache share copies/diagnostics use `FileSystem.CacheDirectory`.

Validate upgrade/reinstall behavior according to Apple platform/package semantics; do not assume uninstall preserves local data.

## Restore recovery

Test encrypted restore with attachments and terminate/relaunch around multiple restore phases. On startup, recovery must resolve the durable journal/marker before normal finance navigation.

Representative Apple filesystem behavior should also be used to validate path confinement and symbolic-link handling where practical.

## iOS layout/orientation

Current iOS metadata supports portrait and landscape orientations, with iPad allowing portrait upside-down as well.

Validate:

- iPhone portrait/landscape;
- iPad sizes;
- adaptive navigation switching to tablet/flyout idiom;
- large text/Dynamic Type;
- orientation transitions while finance forms are open.

## Mac Catalyst desktop UI

Mac Catalyst uses desktop/tablet-style adaptive navigation.

Validate:

- resizable windows;
- keyboard/mouse focus;
- narrow/wide layouts;
- adaptive section preservation;
- high-DPI display;
- menu/focus behavior relevant to MAUI Shell;
- file picker/share behavior.

## Accessibility

### iOS

Validate:

- VoiceOver;
- Dynamic Type;
- reduced motion;
- semantic headings/controls;
- masked PIN/password controls;
- report text equivalents;
- light/dark/system theme.

### Mac Catalyst

Validate:

- VoiceOver;
- keyboard focus/navigation;
- larger interface;
- semantic controls;
- resizable layout;
- report text/table equivalents;
- light/dark appearance.

## Screen-capture privacy

Finora does not claim a universal iOS/Mac Catalyst equivalent to Android `FLAG_SECURE` for ordinary apps.

Privacy mode/hide-on-launch remains the application-level defense for passive monetary display. Release documentation/store copy must not promise universal screenshot prevention on Apple platforms.

## Privacy-mode test

Enable privacy/hide-on-launch and verify passive amounts and report chart magnitude are hidden on every supported finance surface.

Then disable hiding and verify currency-aware values display correctly.

## Local-calendar test

Set a non-UTC Apple device timezone and verify:

- transaction date filters;
- Dashboard financial month;
- Reports From/Through boundaries;
- monthly/yearly local grouping;
- future-dated row exclusion;
- reconciliation full statement day.

Use a DST-capable timezone for additional native QA where practical.

## App Store / distribution validation

Before iOS release:

- archive succeeds with supported Xcode;
- bundle/provisioning/signing correct;
- icon/splash validated;
- privacy declarations match local-first behavior;
- Face ID purpose string matches feature;
- notification permissions declared/used correctly;
- screenshots use synthetic data;
- encrypted backup/restore tested;
- migration tested;
- VoiceOver/Dynamic Type tested.

Before Mac Catalyst distribution:

- archive/build succeeds;
- signing/notarization/distribution configuration correct;
- finance category metadata correct;
- LocalAuthentication/UserNotifications tested;
- file sharing/backup tested;
- accessibility/keyboard/resizing tested;
- migration tested.

See [Store Readiness](../releases/STORE_READINESS.md).