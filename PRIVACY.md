# Finora Privacy

Finora is designed as a **local-first personal finance application**. The current release does not require a Finora account, email address, phone number, login, subscription account, or internet connection for core finance functionality.

## Where financial data is stored

Current-release financial data is stored on the user's device in app-private storage, including:

- accounts and opening/current balance state;
- transactions, transfers, refunds, adjustments, categories, tags, splits, merchants/payees, notes, payment methods, and manually entered locations;
- budgets and budget periods;
- savings goals and contributions;
- recurring rules/occurrence state;
- reconciliation and transaction revision history;
- receipt/attachment files and their integrity metadata;
- local app preferences/reminder state.

Finora does not automatically copy this data to a Finora server because the current release has no Finora cloud service.

## No required analytics/advertising telemetry

The current source does not include a required analytics service, advertising SDK, advertising identifier collection, or remote telemetry endpoint.

Repository/build platforms may have their own infrastructure logs when developers build Finora; those are separate from the installed application's local finance storage model.

## Location

Finora does **not** collect device location in the background. A transaction location field is populated only when the user manually enters text. The current product does not require GPS/location permission for that field.

## Receipts and attachments

Receipt/document files selected by the user are copied into Finora's app-private attachment storage. Finora stores local metadata such as content type, byte size, original filename, relative private path, and SHA-256 checksum so file integrity/lifecycle can be managed.

Receipt contents are not automatically uploaded.

## Import

CSV import occurs only after the user selects a file through system UI and reviews/matches columns. The selected data is parsed locally and committed to the local database after validation.

## Export

CSV/PDF export is generated locally. Finora invokes the operating system share/save interface only after user action.

Once a user exports or shares finance data to another application/location, that destination's privacy/security controls apply. Finora cannot control a file after another app receives it.

## Encrypted backups

Finora creates an encrypted backup only when requested by the user. Current backup protection uses password-derived key material and AES-GCM authenticated encryption. Receipt bytes are included after local integrity validation.

Finora does not automatically upload backups. The user chooses a destination through system share/save UI.

Finora cannot recover a forgotten backup password. Users should choose a strong password and store it safely outside Finora.

## Restore

Restore is initiated by the user through a system file picker. Finora validates/decrypts the selected backup, presents a preview, validates supported schema/attachment integrity, and replaces current local finance data only through the restore workflow.

## App lock and secure storage

If enabled, Finora stores small app-lock verifier/security values through operating-system secure storage. Secure storage is **not** used as a database for the user's financial records or receipt files.

Biometric/Windows Hello unlock is optional and requires PIN fallback in the current design. Biometric templates are controlled by the operating system; Finora does not receive/store fingerprint/face templates.

## Local notifications

Notifications are optional and require platform permission where applicable. Finora intentionally uses generic/privacy-safe reminder text because notifications may appear on a lock screen outside the Finora app lock.

Notification scheduling state is stored locally. Users can disable reminders.

## Diagnostics

Finora's local diagnostic logger stores event/type tokens only. It is designed not to serialize caller-supplied financial properties, exception messages, or stack traces.

Sanitized diagnostic logs and integrity reports must not contain:

- account names;
- merchant/payee names;
- transaction notes;
- transaction amounts;
- manually entered locations;
- receipt names/contents;
- PINs;
- backup passwords;
- encryption/signing secrets.

The hidden developer data-integrity check reports health codes/counts for SQLite, relationships, transfer/split/recurrence state, and receipt-file integrity without exporting private finance contents.

## Privacy mode and screen protection

Privacy mode can hide displayed amounts. Finora can request platform-specific sensitive-screen protection where supported. Such protection is not universal: operating systems/devices can have capture limitations, and an external camera can always photograph a screen.

## Local premium demo state

The current local premium flag is a development/demo capability. It is not a remote entitlement record and is not represented as tamper-proof commercial licensing.

## Data deletion

Finora provides an explicit action to delete local finance data. Receipt/attachment cleanup is included in the deletion workflow.

App preferences and app-lock configuration may be handled separately from finance-record deletion so users do not accidentally weaken security settings while clearing financial records; UI messaging explains the scope of the deletion action.

## Uninstalling the app

Operating systems can remove app-private data when Finora is uninstalled/reset. **Uninstalling without first saving an external encrypted backup may permanently remove local finance records and receipts.**

## Cloud sync / account system

Cloud synchronization, collaboration, Finora remote accounts, mobile-number authentication, and server-backed entitlement validation are later-version possibilities only. They are not part of the current local-first privacy model. If introduced, this document and the threat model must be updated before release.

## Permissions

Permissions should be requested only when a feature needs them. Current source may use platform capabilities for notifications, biometrics/Windows Hello, file selection/sharing, and platform screen-protection behavior. Finora must continue to work for core local finance recording when optional notification/biometric permissions are denied.

## Support and security contact

Business/security: `sanskarin@outlook.in`

Support: `supportramsandesh@gmail.com`

Repository: https://github.com/sanskarIN/Finora

Do not send real finance databases, real receipts, PINs, or backup passwords in support/security correspondence. Use synthetic data to reproduce problems whenever possible.
