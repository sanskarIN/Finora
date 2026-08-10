# Finora Engineering Decisions

This file records architectural choices that should not be silently changed by later feature work.

## 1. Money uses integer minor units

Persist monetary amounts as signed 64-bit integer minor units plus currency code. Parse/format user-entered major units with `decimal`. Do not introduce `float`/`double` money arithmetic.

Currency precision is resolved from the currency code. Do not assume every currency has two decimal places.

Reason: binary floating point and a universal two-decimal assumption are both unsafe for financial correctness.

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

Recurring rules have explicit lifecycle state. Active rules can be paused, paused rules can be resumed only after their current account/category/currency dependencies are revalidated, and archived rules preserve occurrence history without continuing generation.

Reason: repeated startup/scheduler runs and lifecycle changes must not duplicate obligations or silently target unavailable accounts.

## 6. Backups use established authenticated cryptography

Encrypted backups use password-derived keys with PBKDF2-SHA256 and AES-GCM authenticated encryption. Never invent a Finora encryption primitive.

Receipt bytes are validated/staged during backup/restore and restore uses transactional/rollback controls.

Authenticated encryption proves that a backup has not been modified without the password, but it does not prove that the authenticated snapshot contains a valid Finora financial graph. Therefore authenticated snapshots must pass domain/relation validation before preview/restore.

Sensitive plaintext and receipt byte buffers must be cleared as early as practical after use, including validation-failure paths.

## 7. Backup upload is explicit-user-only

Finora may create an encrypted backup locally and invoke the system share/save surface, but it must not automatically upload a backup in the current release.

## 8. Receipts live in app-private files, not database BLOB columns

Store receipt/document bytes under app-private attachment storage and keep path/type/size/checksum metadata in SQLite. Confine paths to the attachment root and verify SHA-256 where integrity matters.

Path confinement must use platform-correct path comparison semantics. Windows paths are case-insensitive; Unix-style Android/Apple paths are case-sensitive.

Reason: large file lifecycle/storage is easier to control without bloating routine relational queries/backups of the raw DB file.

## 9. Critical transaction edits create local revision records

Persist privacy-sensitive local revision snapshots for audit/history, but do not expose raw snapshot JSON through logs or sanitized diagnostics.

## 10. Database schema changes are versioned migrations

`schema.version` is explicit. Add a one-step migration for each next released schema and retain older migration paths. Advance the marker only after the step succeeds transactionally.

Current declared schema: v2, with v1 → v2 migration coverage.

## 11. Native notification schedules are backed by persisted dedupe state

Persist reminder schedules/dedupe keys separately from OS scheduling APIs. Platform scheduling must be permission-gated and restart-safe.

Notification content stays generic because it can appear outside Finora's app lock.

Pausing/archiving/completing a recurring rule, disabling a reminder, or removing an active budget condition must remove stale native schedules instead of leaving an obsolete reminder registered with the OS.

## 12. Biometrics/Windows Hello do not replace PIN fallback

Biometric unlock can be enabled only with a configured Finora PIN. Cancellation/unavailability/lockout returns to PIN rather than bypassing the app lock.

PIN verification fails closed when secure-storage verifier material is missing or corrupt while PIN-enabled state is present.

## 13. Capture protection is platform-capability-based

Use supported native APIs (for example Android secure-window behavior and supported Windows display-affinity behavior) and document unsupported/partial platform behavior. Do not claim universal screenshot blocking.

## 14. Secure storage holds small security values only

Use platform secure storage for small verifier/security material. Do not place the SQLite database, receipt bytes, large backups, or entire financial datasets in secure storage.

## 15. No third-party package is added merely by assumption

Prefer framework/platform capabilities when practical. New dependencies require compatibility, maintenance, license, security, and release-toolchain review.

Current charts use MAUI drawing + textual equivalents; notification/biometric platform integrations do not require speculative third-party packages.

## 16. Reports must have text equivalents

A visual chart cannot be the only representation of financial meaning. Expose equivalent labels/tables/summaries and avoid misleading scales.

## 17. Financial aggregation is currency-scoped

Never add monetary values from different currencies and then label the total as one currency unless a reviewed exchange-rate workflow explicitly performed that conversion.

Dashboard/report/tag totals are scoped to a chosen reporting currency. Rows that naturally belong to another currency retain their own currency display. Finora currently does not invent or silently fetch exchange rates.

## 18. Category reporting follows split allocations

If a transaction has category splits, category/budget reporting uses the split allocations rather than double-counting or attributing the full parent amount to its top-level category.

Category-budget descendants are resolved recursively, not only one level deep.

## 19. Budget period semantics are centralized

`BudgetPeriodPolicy` is the shared source for resolving effective budget windows.

- weekly generated windows run Monday through Sunday;
- monthly generated windows use calendar months;
- explicit periods take precedence;
- rollover affects planned amount only when enabled;
- effective planned amount must remain positive;
- explicit periods cannot overlap;
- custom-cadence budgets are active only inside an explicit period and must not invent fallback one-day periods.

Replacing persisted explicit periods must be transactional so a failed replacement does not erase the previously valid period set.

## 20. Local premium is not secure licensing

The current local premium flag is a development/demo capability. It is explicitly not tamper-proof. Commercial entitlement needs future store/server validation and must not be faked by obfuscating a local boolean.

## 21. Diagnostics are privacy-safe by design

Diagnostic logs and integrity reports must exclude account names, merchant/payee names, notes, amounts, manually entered locations, receipt names/contents, PINs, backup passwords, and cryptographic/signing secrets.

## 22. Data integrity is independently checkable

Provide an on-device privacy-safe diagnostic for SQLite integrity, foreign keys, transaction/account currency, transfer pairs, split totals, category cycles, budget configuration/category relationships, savings contribution histories, recurrence rule/payment state, reconciliation links, and receipt path/size/hash state.

Reason: source-level validation is not enough for a long-lived local financial database.

## 23. System pickers/share sheets are explicit trust-boundary transitions

Import/export/backup/receipt operations use system selection/share surfaces after user action. Once exported/shared to another destination, protection depends on the user-selected app/location.

## 24. Warnings and analyzers are quality gates

Nullable reference types, warnings-as-errors, deterministic builds, and latest-recommended analysis are repository defaults. Broad analyzer disabling is not an acceptable shortcut.

## 25. Structural preflight does not replace compilation

`build/scripts/verify_structure.py` exists so malformed XAML/project wiring, project references, version/schema drift, required repository files, money-representation violations, and selected privacy/platform contract errors can be caught without a .NET SDK. A passing preflight is not evidence that C# compiles or a MAUI platform works.

## 26. Platform behavior requires platform validation

Notification scheduling, biometric APIs, screen-capture controls, file picker/share behavior, packaging, signing, accessibility, and store behavior require builds/tests on the matching platform.

## 27. Signing secrets stay outside the repository

Keystores, private keys, certificates, provisioning profiles containing secrets, and passwords belong in secure release infrastructure—not Git history.

## 28. Open source license remains Apache-2.0

Finora source is Apache-2.0 licensed. Third-party dependencies retain their own licenses and require exact release-time dependency/license review.
