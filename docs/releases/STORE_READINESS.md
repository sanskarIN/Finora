# Finora Store Readiness

This document separates source completeness from platform/store validation. A check is not considered complete until it is executed on the appropriate supported toolchain/device.

## Common release gates

- [ ] `python build/scripts/verify_structure.py` passes.
- [ ] `dotnet workload restore` succeeds on supported build hosts.
- [ ] NuGet restore completes without unresolved vulnerabilities or license surprises.
- [ ] `dotnet format Finora.sln --verify-no-changes --no-restore` passes.
- [ ] Unit, integration, migration, backup/restore, and UI-contract tests pass.
- [ ] Release builds pass for Android, Windows, iOS, and Mac Catalyst.
- [ ] No signing keys, passwords, certificates, API keys, or production secrets are present in source/artifacts/logs.
- [ ] Exact restored third-party dependency licenses are reviewed and `THIRD_PARTY_NOTICES.md` is updated if required.
- [ ] Privacy/terms/store disclosures match actual permissions and behavior.
- [ ] Fresh-install and upgrade/migration paths are tested.
- [ ] Encrypted backup create/preview/restore succeeds; wrong-password and tampered-backup rejection are tested.
- [ ] Full local data deletion is verified, including receipt files.
- [ ] App lock, PIN lockout, biometric fallback, inactivity lock, and sensitive-screen behavior are tested.
- [ ] Notification permission denial/grant/revocation and reminder deduplication are tested.
- [ ] Accessibility is tested with large text, screen reader, keyboard where applicable, focus order, contrast, and reduced motion.
- [ ] Light/dark/system themes are smoke-tested.
- [ ] Data integrity report passes on a clean release-candidate dataset.

## Android

- [ ] Supported .NET/MAUI + Android workload is installed on the release host.
- [ ] Release build succeeds for `net10.0-android`.
- [ ] Signed AAB is produced using credentials outside the repository.
- [ ] Package ID `in.sanskar.finora` is final and matches store configuration.
- [ ] Adaptive icon, monochrome icon, splash, launcher label, and dark/light behavior are verified on physical devices/emulators.
- [ ] Android 8+ minimum/API behavior is validated against the configured minimum.
- [ ] Runtime notification permission flow is verified on versions that require it.
- [ ] Alarm/reminder behavior is tested across force-stop/reboot/doze limitations; any OS limitations are documented.
- [ ] BiometricPrompt behavior is tested with enrolled, unavailable, cancelled, and lockout states.
- [ ] `FLAG_SECURE` behavior is verified on sensitive Finora surfaces and documented as device/OS dependent.
- [ ] File picker/share sheet backup, import, CSV/PDF export, and receipt flows are verified.
- [ ] Backup/restore with attachments is verified after process restart.
- [ ] Upgrade from the previous released schema is tested using a copy of synthetic v1 data.
- [ ] Play Console data-safety answers match the local-first/no-analytics/no-account behavior.

## Windows

- [ ] Release build succeeds for `net10.0-windows10.0.19041.0`.
- [ ] Final MSIX/package identity, publisher, icons, capabilities, and signing are configured outside source-control secrets.
- [ ] Windows Hello available/unavailable/cancelled paths are tested with PIN fallback.
- [ ] Toast permission/scheduling behavior is tested for packaged identity.
- [ ] Display-affinity capture protection is tested and documented for unsupported capture paths.
- [ ] Window resizing, minimum practical window size, keyboard navigation, high DPI, and multi-monitor behavior are tested.
- [ ] File picker/share/export/backup/restore and attachment opening are tested under packaged permissions.
- [ ] Package upgrade preserves/migrates the database and app-private receipt files.

## iOS

- [ ] Supported macOS/Xcode/.NET/MAUI toolchain is installed.
- [ ] Release/archive build succeeds for `net10.0-ios`.
- [ ] Bundle ID/provisioning/signing are configured outside the repository.
- [ ] App icon and launch appearance are validated on current supported device sizes.
- [ ] LocalAuthentication available/unavailable/cancelled/lockout paths are tested with PIN fallback.
- [ ] UserNotifications permission denial/grant/revocation and scheduled reminders are tested.
- [ ] File picker/share/backup/restore/import/export/receipt flows are tested on-device.
- [ ] Screenshot/capture limitation is documented because iOS does not offer a universal equivalent to Android `FLAG_SECURE` for ordinary apps.
- [ ] Dynamic Type, VoiceOver, reduced motion, dark mode, and orientation/layout behavior are tested.
- [ ] App Store privacy declarations match local-first behavior and actual platform permissions.

## Mac Catalyst

- [ ] Release/archive build succeeds for `net10.0-maccatalyst`.
- [ ] App signing/notarization/distribution configuration is performed outside repository secrets.
- [ ] LocalAuthentication and UserNotifications behavior are verified.
- [ ] Keyboard/mouse navigation, resizable windows, menu/focus behavior, dark mode, and high DPI are tested.
- [ ] File picker/share/backup/restore/import/export/attachment flows are tested.
- [ ] Accessibility is verified with VoiceOver and keyboard focus.

## Store copy and assets

- [ ] Product name: Finora.
- [ ] Attribution: “Made by the Sanskar”.
- [ ] Repository: https://github.com/sanskarIN/Finora
- [ ] Creator profile: https://www.github.com/sanskarIN
- [ ] Business/security contact: sanskarin@outlook.in
- [ ] Support: supportramsandesh@gmail.com
- [ ] Store screenshots contain synthetic data only.
- [ ] Store listing does not claim cloud sync, tamper-proof local premium licensing, guaranteed financial outcomes, or bug-free operation.
- [ ] Privacy copy states that uninstalling without an external backup can remove local data.

## Release decision

A release is ready only when every applicable checkbox is backed by an actual build/test/device result. Source presence alone is not proof of platform behavior.
