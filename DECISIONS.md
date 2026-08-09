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

## 26. Currency precision is currency-aware

Stored values remain integer minor units, but major/minor conversion and formatting use currency-specific decimal precision where Finora has built-in metadata. Zero-decimal and three-decimal currencies are not forced through a two-decimal assumption.

Release QA must verify the built-in currency precision table against the currency metadata required by the targeted release markets. No exchange-rate conversion is implied by this metadata.

## 27. Unlike currencies are never silently aggregated

Dashboards and aggregate reports use an explicit reporting currency. Accounts, budgets, goals, transactions, and recurrence rows with another currency retain that currency and are displayed separately rather than converted or added together.

Finora does not invent exchange rates. A future cross-currency aggregate requires an explicit exchange-rate source, timestamp semantics, user disclosure, and a new architecture decision.

## 28. Restore spans SQLite and receipt files through a recovery protocol

Encrypted restore touches both the relational database and app-private attachment files, so a SQLite transaction alone is insufficient for crash safety. Production restore uses a durable app-private recovery journal, a transient `internal.restore.commit` marker, a verified pre-restore receipt copy, and startup recovery.

If the pending marker remains, the database replacement did not commit and receipt files roll back. If the marker was removed by the committed restore transaction, startup finalizes the new receipt tree. Recovery metadata contains no backup password or financial contents.

## 29. Primary navigation adapts by device class and width

Phones use bottom primary tabs. Tablet/desktop layouts expose the equivalent primary hierarchy through a flyout/sidebar. Route helpers keep onboarding, unlock, startup, and resize transitions on the correct root without changing finance state.

Native resize, keyboard, focus, and accessibility behavior remain platform-release validation requirements.

## 30. Locale preference controls runtime formatting

The saved locale is validated and applied to process/thread culture before normal UI navigation. Date and number formatting follows the active culture while stored money remains integer minor units and stored timestamps remain explicit UTC/date values as designed.

Localization readiness does not mean every literal UI string has already been translated; translation completeness is a separate release concern.

## 31. Destructive reset and sample reset have distinct guarantees

“Delete all local finance data” removes finance-domain records, audit/backup metadata, reminder records, and receipt metadata/files while preserving schema metadata, app preferences, and app-lock configuration.

The hidden developer “Reset to synthetic sample data” action first performs the same safe finance reset, then creates a deterministic synthetic dataset. It requires an explicit typed confirmation and must never silently overwrite real local finance data.
