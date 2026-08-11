# App Lock, Privacy, and Screen Protection

This document describes the current Finora 0.2.0 local app-lock/privacy behavior and the boundaries that still require native platform validation.

## Local-first security boundary

The current release stores finance data locally and does not require a Finora account. App lock therefore protects casual access to the local application UI; it is not a remote identity system and does not turn a compromised/rooted device into a trusted environment.

## PIN format

Finora accepts a local PIN containing 4–12 ASCII digits (`0`–`9`).

Other Unicode digit characters and non-digit text are not accepted by the current app-lock service.

## PIN verifier storage

The plaintext PIN is not intentionally persisted.

Current verifier flow:

1. generate a random 16-byte salt;
2. derive a 32-byte verifier with PBKDF2-SHA256;
3. current app-lock iteration count is 150,000;
4. store salt/verifier in OS `SecureStorage`;
5. store small enabled/failure/lockout state in application preferences;
6. clear managed salt/hash byte arrays where practical after use.

Verification derives a candidate hash and uses fixed-time comparison.

## Fail-closed / stale-marker behavior

Finora distinguishes two cases:

### Secure-storage provider temporarily unavailable

If the explicit PIN-enabled preference exists and SecureStorage access itself fails, `HasPinAsync` returns locked/enabled rather than silently disabling app lock.

### Secure storage readable but verifier missing/corrupt

If SecureStorage is readable and the salt/hash are absent or malformed, Finora clears stale verifier/lockout/enabled state so the app is not permanently trapped behind a verifier that no longer exists.

This is a recovery behavior for locally inconsistent lock metadata, not a password-recovery bypass.

## Failed PIN attempts and lockout

Failed verification increments a bounded counter through `PinAttemptPolicy`. When the policy returns a lockout duration, the app stores a UTC lock-until timestamp.

The policy has unit coverage for escalation boundaries. The exact user experience still requires native validation across lifecycle/restart/time changes.

## PIN removal

Removing the PIN first removes secure verifier material. If the secure-storage removal operation fails, Finora reports a generic failure and does not falsely announce that app lock was removed.

Biometric preference should not remain represented as a valid independent unlock factor after successful PIN removal.

## Biometrics and Windows Hello

Biometric/Windows Hello is optional and requires PIN fallback in the current design.

Platform behavior includes availability check/authentication through native APIs. Cancellation, unavailability, provider errors, or lockout must not bypass the locked state.

Provider-specific error strings are normalized to stable application-owned text before normal user-facing alerts. Raw OS/provider error text must not be passed directly into a `Result.Failure`/alert path.

## Secret-entry UI

Settings uses dedicated masked `Entry` controls for:

- backup password;
- new PIN;
- confirm PIN.

The lock screen PIN is also masked.

Secret entry should not be regressed to ordinary `DisplayPromptAsync` text input. Structural preflight checks the named Settings password fields and searches for unsafe password/PIN prompt patterns.

UI fields are cleared after the handled operation paths. Managed `string` instances cannot be guaranteed to be physically zeroed from runtime memory immediately.

## Privacy mode

`PrivacyMode` and `HideAmountsOnLaunch` protect passive monetary display.

Current passive display protection covers:

- account balances;
- transaction history;
- Transaction Tools;
- account-detail transaction rows;
- budgets;
- savings cards/forecast text;
- recurring rules/occurrences;
- reconciliation preview/history;
- transaction-detail split rows;
- reports.

The shared converter displays `••••` when hiding is active and otherwise formats a `Money` value with currency-aware decimal precision.

## Chart privacy

Masking text is not sufficient if a chart still exposes value magnitude.

When report amounts are hidden, quantitative chart collections are suppressed. The UI can still show non-monetary labels/status/date context and an explanation that values are hidden.

## Current amount privacy limitations

Privacy mode is a UI display control, not data encryption at rest.

Financial values still exist in:

- SQLite;
- local backups when decrypted in memory during explicit operations;
- exported files created by explicit user action;
- app state required to perform calculations.

Editable amount fields are intentionally visible while the user is editing/entering those values.

## Sensitive-screen protection

Finora uses platform-supported capture controls where implemented:

- Android secure-window behavior;
- supported Windows display-affinity behavior.

No universal screenshot/screen-recording prevention is claimed for all platforms. iOS/Mac Catalyst and unsupported Windows capture paths require honest platform-specific limitation documentation.

A camera pointed at the screen, OS-level privileged software, rooted/jailbroken devices, accessibility/system services, or unsupported capture mechanisms may still expose content.

## No background location

The current release does not collect background location. Transaction location is an optional manually entered text field.

Do not add location permission/background collection simply because a transaction has a `ManualLocation` property.

## Notification privacy

Local notification title/body is generic because it can appear outside the Finora app lock.

Notification payloads must not include:

- transaction amount;
- account name;
- merchant/payee;
- note;
- manual location;
- receipt name;
- PIN/backup password/security secrets.

## Diagnostic privacy

The privacy logger is intentionally restrictive.

It records bounded/sanitized event tokens and exception type information, not raw exception messages/stacks or arbitrary finance properties.

Forbidden diagnostic contents include:

- amounts;
- account names;
- merchant/payee names;
- notes;
- manual locations;
- receipt names/contents;
- PINs;
- backup passwords;
- encryption keys;
- signing secrets;
- raw storage/database/provider paths/messages.

## Error display

Deliberate user validation messages can remain actionable. Unexpected storage/database/cryptographic/provider/path failures should map to generic user-safe messages while privacy diagnostics record only safe event/type information.

Structural preflight searches primary app alert code for raw `ex.Message`-style leakage patterns.

## Android automatic backup/device transfer

Android source currently combines:

- `android:allowBackup="false"`;
- `android:usesCleartextTraffic="false"`;
- legacy `backup_rules.xml` exclusions;
- Android 12+ `data_extraction_rules.xml` exclusions.

The rules exclude ordinary root/file/database/shared-preference/external data domains from platform backup/device-transfer mechanisms.

Final merged-manifest/package/device behavior still requires Android build/device evidence.

## Local premium demo

`LocalPremiumDemoEnabled` is a development/demo preference. It is not secure commercial entitlement and must not be represented as tamper-proof licensing.

Paid entitlement requires a future reviewed store/server-backed design.

## Threat model relationship

This document is an implementation guide. For attack assumptions, assets, trust boundaries, controls, and residual risks, see [THREAT_MODEL.md](THREAT_MODEL.md).

## Release validation

Before release, test with synthetic data:

- PIN setup/change/remove;
- valid/invalid PIN;
- escalating lockout;
- restart during lockout;
- SecureStorage unavailable vs readable-missing verifier;
- biometric success/cancel/unavailable/lockout;
- PIN fallback after biometric failure;
- masked secret fields;
- field clearing after operations;
- privacy mode across all passive monetary surfaces;
- chart suppression while hidden;
- Android capture protection;
- supported Windows capture protection;
- notification content on lock screen;
- privacy logger redaction/rotation/path behavior;
- Android merged backup/data-transfer exclusions.