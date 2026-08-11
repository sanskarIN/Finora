# Finora Store Readiness

This document separates source completeness from platform/store validation. A check is not considered complete until it is executed on the appropriate supported toolchain/device.

## Common release gates

- [ ] `python build/scripts/verify_structure.py` passes.
- [ ] Structural preflight reports no raw minor-unit user-facing money, unmasked Settings secrets, complete-reset wiring drift, biometric provider-text regression, Android backup-rule drift, raw exception alerts, malformed XAML/project wiring, or version/schema inconsistency.
- [ ] `dotnet workload restore` succeeds on supported build hosts.
- [ ] NuGet restore completes without unresolved vulnerabilities or license surprises.
- [ ] Release formatting review is complete according to repository build guidance; formatting is not substituted for compiler/test evidence.
- [ ] Unit, integration, migration, backup/restore, and UI-contract tests pass.
- [ ] Release builds pass for Android, Windows, iOS, and Mac Catalyst.
- [ ] No signing keys, passwords, certificates, API keys, or production secrets are present in source/artifacts/logs.
- [ ] Exact restored third-party dependency licenses are reviewed and `THIRD_PARTY_NOTICES.md` is updated if required.
- [ ] Privacy/terms/store disclosures match actual permissions and behavior.
- [ ] Fresh-install and upgrade/migration paths are tested.
- [ ] Encrypted backup create/preview/restore succeeds; wrong-password, tampered-backup, invalid-graph, linked-path and interrupted-restore rejection/recovery are tested.
- [ ] Full local finance-data deletion through the dedicated reset service is verified, including receipt files and typed confirmation.
- [ ] App lock, PIN lockout, biometric fallback, inactivity lock, secret-field masking, and sensitive-screen behavior are tested.
- [ ] Biometric provider/system error strings are not exposed verbatim to user-visible text.
- [ ] Notification permission denial/grant/revocation, reminder deduplication, replacement failure and stale cancellation are tested.
- [ ] Privacy mode/hide-on-launch masks passive money across Dashboard, Accounts, transaction history/tools/detail splits, Budgets, Savings, Recurring, Reconciliation, and Reports.
- [ ] Quantitative report chart magnitude is unavailable while privacy mode hides amounts.
- [ ] Signed chart direction is correct: negative net values render below zero rather than as positive bars.
- [ ] Dashboard current/previous financial month, trailing 30/90 days and year-to-date ranges are verified.
- [ ] User-selected local calendar ranges are validated in at least one non-UTC time zone; a DST-observing zone is included on platforms where practical.
- [ ] Transaction history sort choices and 50-row incremental Load more behavior are smoke-tested.
- [ ] Complete report matrix is present: category, income/expense, account trend, budget, merchant/payee, monthly, yearly, recurring obligations, savings progress, and currency-scoped tag reporting.
- [ ] Current monthly/yearly comparisons do not include future-dated imported rows before their local date arrives.
- [ ] Accessibility is tested with large text, screen reader, keyboard where applicable, focus order, contrast, reduced motion, Dashboard period picker, transaction sort/load-more controls, Reports equivalents, Onboarding legal links, and Settings destructive/About controls.
- [ ] Light/dark/system themes are smoke-tested.
- [ ] Data integrity report passes on clean release-candidate dataset.
- [ ] About version/build shown in app matches packaged metadata and store artifact metadata.

## Android

- [ ] Supported .NET/MAUI + Android workload is installed on release host.
- [ ] Release build succeeds for `net10.0-android`.
- [ ] Signed AAB is produced using credentials outside repository.
- [ ] Package ID `in.sanskar.finora` is final and matches store configuration.
- [ ] Adaptive icon, monochrome icon, splash, launcher label, and dark/light behavior are verified on physical devices/emulators.
- [ ] Android 8+ minimum/API behavior is validated against configured minimum.
- [ ] Merged manifest contains `android:allowBackup="false"` and `android:usesCleartextTraffic="false"`.
- [ ] Merged manifest references `@xml/backup_rules` and `@xml/data_extraction_rules`.
- [ ] Packaged legacy full-backup rules exclude root/file/database/sharedpref/external domains.
- [ ] Packaged Android 12+ cloud-backup/device-transfer rules exclude root/file/database/sharedpref/external domains.
- [ ] Test profile ordinary backup/device-transfer does not restore Finora private finance database/preferences/receipts.
- [ ] Runtime notification permission flow is verified on versions that require it.
- [ ] Alarm/reminder behavior is tested across force-stop/reboot/doze limitations; any OS limitations are documented.
- [ ] Deduplicated reminder replacement failure preserves prior working reminder.
- [ ] Successful replacement disables old persisted row before stale native cancellation.
- [ ] Cancelling nonexistent reminder does not create a new `PendingIntent`; `NoCreate` behavior is verified on supported test tooling/device behavior.
- [ ] BiometricPrompt behavior is tested with enrolled, unavailable, cancelled, failed and lockout states.
- [ ] Android biometric provider `errString` is not displayed verbatim; stable Finora text retains PIN fallback.
- [ ] `FLAG_SECURE` behavior is verified on sensitive Finora surfaces and documented as device/OS dependent.
- [ ] File picker/share sheet backup, import, CSV/PDF export, and receipt flows are verified.
- [ ] Backup/restore with attachments is verified after process restart.
- [ ] Linked/reparse-style private path tests are run where Android filesystem/test environment permits.
- [ ] Dashboard period picker uses local Android calendar boundaries under a non-UTC device time zone.
- [ ] Reports monthly/yearly grouping follows local calendar and current comparisons stop at today.
- [ ] Negative net bars render below zero; text/list equivalents remain accurate.
- [ ] Privacy mode masks passive money on all finance surfaces and suppresses quantitative report charts.
- [ ] Transaction sort and 50-row Load more behavior works with touch/TalkBack.
- [ ] Onboarding Privacy/Terms and Settings revisit/About/reset controls are reachable with TalkBack.
- [ ] Upgrade from previous released schema is tested using copy of synthetic v1 data.
- [ ] Play Console data-safety answers match local-first/no-analytics/no-account/no-automatic-backup behavior.

## Windows

- [ ] Release build succeeds for `net10.0-windows10.0.19041.0`.
- [ ] Final MSIX/package identity, publisher, icons, capabilities, and signing are configured outside source-control secrets.
- [ ] About version/build matches packaged identity/artifact version.
- [ ] Windows Hello available/unavailable/cancelled/error paths are tested with PIN fallback and generic user text.
- [ ] Toast permission/scheduling behavior is tested for packaged identity.
- [ ] Reminder replacement/cancellation lifecycle is verified after app restart.
- [ ] Display-affinity capture protection is tested and documented for unsupported capture paths.
- [ ] Window resizing, minimum practical window size, keyboard navigation, high DPI, and multi-monitor behavior are tested.
- [ ] Phone-style narrow navigation and desktop flyout transitions remain usable when resizing.
- [ ] Dashboard period selector and transaction sort/load-more are keyboard operable.
- [ ] Reports signed chart direction and text/table equivalents are verified.
- [ ] Privacy mode masks passive money and hides quantitative report charts.
- [ ] Local-calendar filters/reports are smoke-tested under a non-UTC Windows time zone and, where practical, a DST transition.
- [ ] File picker/share/export/backup/restore and attachment opening are tested under packaged permissions.
- [ ] Package upgrade preserves/migrates database and app-private receipt files.
- [ ] Narrator/high-contrast/large-text behavior is verified for new Dashboard/Reports/Settings/transaction controls.

## iOS

- [ ] Supported macOS/Xcode/.NET/MAUI toolchain is installed.
- [ ] Release/archive build succeeds for `net10.0-ios`.
- [ ] Bundle ID/provisioning/signing are configured outside repository.
- [ ] App icon and launch appearance are validated on current supported device sizes.
- [ ] About version/build matches packaged application metadata.
- [ ] LocalAuthentication available/unavailable/cancelled/lockout paths are tested with PIN fallback.
- [ ] Biometric failure text remains stable/generic; no raw provider/system error is shown.
- [ ] UserNotifications permission denial/grant/revocation and scheduled reminders are tested.
- [ ] Reminder replacement/cancellation behavior is verified across app restart.
- [ ] File picker/share/backup/restore/import/export/receipt flows are tested on-device.
- [ ] Screenshot/capture limitation is documented because iOS does not offer universal equivalent to Android `FLAG_SECURE` for ordinary apps.
- [ ] Dashboard/report local-calendar boundaries are tested in a non-UTC zone and a DST-observing zone.
- [ ] Current monthly/yearly comparisons stop at local today and do not expose future-dated rows early.
- [ ] Signed charts show negative values below zero and have equivalent list/text values.
- [ ] Privacy mode masks passive finance amounts and suppresses quantitative report charts.
- [ ] Transaction sort/load-more behavior is usable with VoiceOver.
- [ ] Onboarding Privacy/Terms and Settings About/reset controls are reachable with VoiceOver.
- [ ] Dynamic Type, VoiceOver, reduced motion, dark mode, and orientation/layout behavior are tested.
- [ ] App Store privacy declarations match local-first behavior and actual platform permissions.

## Mac Catalyst

- [ ] Release/archive build succeeds for `net10.0-maccatalyst`.
- [ ] App signing/notarization/distribution configuration is performed outside repository secrets.
- [ ] About version/build matches packaged app metadata.
- [ ] LocalAuthentication and UserNotifications behavior are verified.
- [ ] Biometric failures use generic Finora text with PIN fallback.
- [ ] Keyboard/mouse navigation, resizable windows, menu/focus behavior, dark mode, and high DPI are tested.
- [ ] File picker/share/backup/restore/import/export/attachment flows are tested.
- [ ] Dashboard period picker, transaction sort/load-more, and Settings/About controls are keyboard/focus accessible.
- [ ] Local-calendar reporting is tested under non-UTC and DST-observing time-zone configurations.
- [ ] Signed charts preserve negative direction and textual equivalents.
- [ ] Privacy mode masks passive monetary values and hides quantitative chart geometry.
- [ ] Accessibility is verified with VoiceOver and keyboard focus.

## Store copy and assets

- [ ] Product name: Finora.
- [ ] Attribution: “Made by the Sanskar”.
- [ ] Repository: https://github.com/sanskarIN/Finora
- [ ] Creator profile: https://www.github.com/sanskarIN
- [ ] Business/security contact: sanskarin@outlook.in
- [ ] Support: supportramsandesh@gmail.com
- [ ] App About screen exposes repository/profile, business/support contacts, Apache-2.0/notices, privacy/terms, contributing/security/support guides.
- [ ] Store screenshots contain synthetic data only.
- [ ] Store screenshots do not expose real monetary values through a passive surface while privacy mode is represented as active.
- [ ] Store listing does not claim cloud sync, tamper-proof local premium licensing, guaranteed financial outcomes, automatic exchange-rate conversion, or bug-free operation.
- [ ] Privacy copy states uninstalling without external backup can remove local data.
- [ ] Privacy/data-safety copy states current Android source explicitly excludes ordinary automatic backup/device transfer, subject to final package/device validation.
- [ ] Accessibility/support text accurately describes chart text equivalents and platform capture limitations.

## Release evidence bundle

For each release candidate retain:

- CI/check-run links for exact candidate commit;
- structural preflight output;
- unit/integration/UI-contract test results;
- Android/Windows/Apple MAUI build logs;
- migration/integrity output;
- encrypted backup/restore and crash-recovery results;
- local-calendar/time-zone regression results;
- Dashboard period/report-matrix/transaction paging smoke results;
- privacy-mode passive amount/chart results;
- signed chart direction results;
- Android merged-manifest and automatic backup/device-transfer test evidence;
- biometric/notification/capture tests;
- screen-reader/keyboard/large-text accessibility notes;
- signing/package/store-console validation records;
- dependency/license/security review.

## Release decision

A release is ready only when every applicable checkbox is backed by an actual build/test/device/store result. Source presence alone is not proof of platform behavior, and an empty classic GitHub commit-status response is not a substitute for check-run evidence.
