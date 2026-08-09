# Finora Engineering Decisions

This file records architectural choices that should not be silently changed by later feature work.

## 1. Money uses integer minor units

Persist monetary amounts as signed 64-bit integer minor units plus currency code. Parse/format user-entered major units with `decimal`. Do not introduce `float`/`double` money arithmetic.

Reason: binary floating point is inappropriate for financial correctness and can create silent rounding drift.

## 2. SQLite is the current local system of record

Use SQLite through EF Core for relational integrity, transactions, async access, indices, and testability. The current product is not a remote-database client.

Reason: Finora is intentionally local-first and must work without login or internet.

## 3. Current release has no required account/cloud service

Do not add mandatory account creation, analytics, telemetry upload, automatic backup upload, or cloud sync to current-release flows.

Future cloud/account work requires explicit product approval plus new architecture/privacy/security decisions.

## 4. Transfers are linked double-entry-style rows

Represent a same-currency transfer as exactly two rows sharing `TransferGroupId`, with equal/opposite amounts and reciprocal counterparty accounts. Edit/delete/restore must preserve the pair.

Cross-currency movement is **not** emulated by this model and requires a future explicit exchange workflow.

## 5. Recurrence is occurrence-first and idempotent

Persist a unique occurrence for `(RecurrenceRuleId, DueOn)`. Due processing must not blindly create finance transactions. Paid/partial-paid workflow creates the transaction once.

Reason: repeated startup/scheduler runs must not duplicate obligations or money movement.

## 6. Backups use established authenticated cryptography

Encrypted backups use password-derived keys with PBKDF2-SHA256 and AES-GCM authenticated encryption. Never invent a Finora encryption primitive.

Receipt bytes are validated/staged during backup/restore and restore uses transactional/rollback controls.

## 7. Backup upload is explicit-user-only

Finora may create an encrypted backup locally and invoke the system share/save surface, but it must not automatically upload a backup in the current release.

## 8. Receipts live in app-private files, not database BLOB columns

Store receipt/document bytes under app-private attachment storage and keep path/type/size/checksum metadata in SQLite. Confine paths to the attachment root and verify SHA-256 where integrity matters.

Reason: large file lifecycle/storage is easier to control without bloating routine relational queries/backups of the raw DB file.

## 9. Critical transaction edits create local revision records

Persist privacy-sensitive local revision snapshots for audit/history, but do not expose raw snapshot JSON through logs or sanitized diagnostics.

## 10. Database schema changes are versioned migrations

`schema.version` is explicit. Add a one-step migration for each next released schema and retain older migration paths. Advance the marker only after the step succeeds transactionally.

Current declared schema: v2, with v1 → v2 migration coverage.

## 11. Native notification schedules are backed by persisted dedupe state

Persist reminder schedules/dedupe keys separately from OS scheduling APIs. Platform scheduling must be permission-gated and restart-safe.

Notification content stays generic because it can appear outside Finora's app lock.

## 12. Biometrics/Windows Hello do not replace PIN fallback

Biometric unlock can be enabled only with a configured Finora PIN. Cancellation/unavailability/lockout returns to PIN rather than bypassing the app lock.

## 13. Capture protection is platform-capability-based

Use supported native APIs (for example Android secure-window behavior and supported Windows display-affinity behavior) and document unsupported/partial platform behavior. Do not claim universal screenshot blocking.

## 14. Secure storage holds small security values only

Use platform secure storage for small verifier/security material. Do not place the SQLite database, receipt bytes, large backups, or entire financial datasets in secure storage.

## 15. No third-party package is added merely by assumption

Prefer framework/platform capabilities when practical. New dependencies require compatibility, maintenance, license, security, and release-toolchain review.

Current charts use MAUI drawing + textual equivalents; notification/biometric platform integrations do not require speculative third-party packages.

## 16. Reports must have text equivalents

A visual chart cannot be the only representation of financial meaning. Expose equivalent labels/tables/summaries and avoid misleading scales.

## 17. Local premium is not secure licensing

The current local premium flag is a development/demo capability. It is explicitly not tamper-proof. Commercial entitlement needs future store/server validation and must not be faked by obfuscating a local boolean.

## 18. Diagnostics are privacy-safe by design

Diagnostic logs and integrity reports must exclude account names, merchant/payee names, notes, amounts, manually entered locations, receipt names/contents, PINs, backup passwords, and cryptographic/signing secrets.

## 19. Data integrity is independently checkable

Provide an on-device privacy-safe diagnostic for SQLite integrity, foreign keys, transfer pairs, split totals, category cycles, recurrence references, and receipt path/size/hash state.

Reason: source-level validation is not enough for a long-lived local financial database.

## 20. System pickers/share sheets are explicit trust-boundary transitions

Import/export/backup/receipt operations use system selection/share surfaces after user action. Once exported/shared to another destination, protection depends on the user-selected app/location.

## 21. Warnings and analyzers are quality gates

Nullable reference types, warnings-as-errors, deterministic builds, and latest-recommended analysis are repository defaults. Broad analyzer disabling is not an acceptable shortcut.

## 22. Structural preflight does not replace compilation

`build/scripts/verify_structure.py` exists so malformed XAML/project wiring can be caught without a .NET SDK. A passing preflight is not evidence that C# compiles or a MAUI platform works.

## 23. Platform behavior requires platform validation

Notification scheduling, biometric APIs, screen-capture controls, file picker/share behavior, packaging, signing, accessibility, and store behavior require builds/tests on the matching platform.

## 24. Signing secrets stay outside the repository

Keystores, private keys, certificates, provisioning profiles containing secrets, and passwords belong in secure release infrastructure—not Git history.

## 25. Open source license remains Apache-2.0

Finora source is Apache-2.0 licensed. Third-party dependencies retain their own licenses and require exact release-time dependency/license review.
