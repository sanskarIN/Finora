# Finora Store Readiness

This document separates source completeness from platform/store validation. A check is not complete until executed on the appropriate supported toolchain/device/store workflow.

## Common release gates

- [ ] `python build/scripts/verify_structure.py` passes.
- [ ] Structural preflight confirms required Android backup-rule resources, masked backup/PIN fields, XAML handler resolution, and no raw exception-alert regressions.
- [ ] `dotnet workload restore` succeeds on supported build hosts.
- [ ] NuGet restore completes without unresolved vulnerabilities/license surprises.
- [ ] `dotnet format Finora.sln --verify-no-changes --no-restore` passes.
- [ ] Unit, integration, migration, backup/restore, notification, diagnostics, storage-path, metadata-persistence, and UI-contract tests pass.
- [ ] Release builds pass for Android, Windows, iOS, and Mac Catalyst.
- [ ] No signing keys, passwords, certificates, API keys, production secrets, real finance databases, or real receipt files are present in source/artifacts/logs.
- [ ] Exact restored third-party dependency licenses are reviewed and `THIRD_PARTY_NOTICES.md` updated if required.
- [ ] Privacy/terms/store disclosures match actual permissions and behavior.
- [ ] Fresh-install and upgrade/migration paths are tested.
- [ ] Encrypted backup create/preview/restore succeeds; wrong-password, tampered-backup, linked-receipt, and semantically-invalid graph rejection are tested.
- [ ] Full local data deletion is verified, including receipt files.
- [ ] App lock, PIN lockout, verifier-missing/provider-failure behavior, biometric fallback, inactivity lock, and sensitive-screen behavior are tested.
- [ ] Backup password/new PIN/confirm PIN fields are masked and clear after use.
- [ ] Notification permission denial/grant/revocation, dedupe replacement failure, stale cancellation, and restart reconciliation are tested.
- [ ] Privacy logger redaction/rotation/link refusal and ViewModel generic error behavior are tested.
- [ ] Stale managed share-copy cleanup does not remove fresh/unrelated/diagnostic files.
- [ ] Accessibility is tested with large text, screen reader, keyboard where applicable, focus order, contrast, reduced motion, and lock-screen semantics.
- [ ] Light/dark/system themes are smoke-tested.
- [ ] Data integrity report passes on a clean release-candidate dataset and detects synthetic aggregate corruption.
- [ ] Direct EF Added/Modified metadata invariant tests pass.

## Android

- [ ] Supported .NET/MAUI + Android workload installed on release host.
- [ ] Release build succeeds for `net10.0-android`.
- [ ] Signed AAB produced using credentials outside repository.
- [ ] Package ID `in.sanskar.finora` is final and matches store configuration.
- [ ] Adaptive icon, monochrome icon, splash, launcher label, dark/light behavior verified on physical devices/emulators.
- [ ] Android 8+ minimum/API behavior validated against configured minimum.
- [ ] Runtime notification permission flow verified on versions that require it.
- [ ] Alarm/reminder behavior tested across force-stop/reboot/doze limitations; OS limitations documented.
- [ ] Failed deduplicated reminder replacement preserves prior OS reminder; successful replacement does not leave duplicate active rows.
- [ ] BiometricPrompt behavior tested with enrolled, unavailable, cancelled, and lockout states with PIN fallback.
- [ ] `FLAG_SECURE` behavior verified on sensitive Finora surfaces and documented as device/OS dependent.
- [ ] `android:allowBackup="false"` is present in final merged manifest.
- [ ] `@xml/backup_rules` is packaged and excludes root/file/database/sharedpref/external domains for legacy full backup.
- [ ] `@xml/data_extraction_rules` is packaged and excludes root/file/database/sharedpref/external domains for Android 12+ cloud backup and device transfer.
- [ ] Representative device/emulator backup/restore or transfer testing confirms Finora private finance data is not copied by ordinary Android backup/device-transfer paths.
- [ ] File picker/share sheet backup, import, CSV/PDF export, receipt flows verified.
- [ ] Receipt symbolic-link/reparse traversal protection is exercised on a compatible test host/device where practical.
- [ ] Backup/restore with attachments verified after process restart.
- [ ] Stale cache share-copy cleanup verified after grace period without affecting diagnostics/unrelated cache files.
- [ ] Upgrade from previous released schema tested using copy of synthetic v1 data.
- [ ] Play Console data-safety answers match local-first/no-analytics/no-account/no-automatic-backup behavior.

## Windows

- [ ] Release build succeeds for `net10.0-windows10.0.19041.0`.
- [ ] Final MSIX/package identity, publisher, icons, capabilities, signing configured outside source-control secrets.
- [ ] Windows Hello available/unavailable/cancelled paths tested with PIN fallback and generic provider-failure messaging.
- [ ] Toast permission/scheduling behavior tested for packaged identity.
- [ ] Notification dedupe/cancellation/reconciliation behavior tested.
- [ ] Display-affinity capture protection tested and unsupported capture paths documented.
- [ ] Window resizing, minimum practical size, keyboard navigation, high DPI, multi-monitor behavior tested.
- [ ] File picker/share/export/backup/restore and attachment opening tested under packaged permissions.
- [ ] App-private path/link behavior validated on NTFS reparse-point-capable test environment.
- [ ] Package upgrade preserves/migrates database and app-private receipt files.

## iOS

- [ ] Supported macOS/Xcode/.NET/MAUI toolchain installed.
- [ ] Release/archive build succeeds for `net10.0-ios`.
- [ ] Bundle ID/provisioning/signing configured outside repository.
- [ ] App icon/launch appearance validated on supported device sizes.
- [ ] LocalAuthentication available/unavailable/cancelled/lockout paths tested with PIN fallback and generic provider-failure text.
- [ ] UserNotifications permission denial/grant/revocation and scheduled reminder replacement/cancellation tested.
- [ ] File picker/share/backup/restore/import/export/receipt flows tested on-device.
- [ ] App-private receipt no-link confinement and interrupted-restore recovery tested on representative filesystem behavior.
- [ ] Screenshot/capture limitation documented because iOS has no universal equivalent to Android `FLAG_SECURE` for ordinary apps.
- [ ] Dynamic Type, VoiceOver, reduced motion, dark mode, orientation/layout behavior tested.
- [ ] App Store privacy declarations match local-first behavior and actual platform permissions.

## Mac Catalyst

- [ ] Release/archive build succeeds for `net10.0-maccatalyst`.
- [ ] App signing/notarization/distribution configuration performed outside repository secrets.
- [ ] LocalAuthentication and UserNotifications behavior verified, including PIN fallback and reminder replacement/cancellation.
- [ ] Keyboard/mouse navigation, resizable windows, menu/focus behavior, dark mode, high DPI tested.
- [ ] File picker/share/backup/restore/import/export/attachment flows tested.
- [ ] App-private receipt link/path confinement tested on representative macOS filesystem.
- [ ] Accessibility verified with VoiceOver and keyboard focus.

## Store copy and assets

- [ ] Product name: Finora.
- [ ] Attribution: “Made by the Sanskar”.
- [ ] Repository: https://github.com/sanskarIN/Finora
- [ ] Creator profile: https://www.github.com/sanskarIN
- [ ] Business/security contact: sanskarin@outlook.in
- [ ] Support: supportramsandesh@gmail.com
- [ ] Store screenshots contain synthetic data only.
- [ ] Store listing does not claim cloud sync, automatic platform backup, tamper-proof local premium licensing, guaranteed financial outcomes, or bug-free operation.
- [ ] Privacy copy states uninstalling without separately saved encrypted backup can remove local data.
- [ ] Privacy/data-safety declaration reflects explicit Android automatic-backup/device-transfer exclusions.

## Release decision

A release is ready only when every applicable checkbox is backed by actual build/test/device/store evidence. Source presence alone, reasoning from source, or an empty classic commit-status list is not proof of platform behavior.
