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

## 29. User-selected calendar dates are resolved through one local-time policy

A date picked in the UI represents the user's **local calendar day**, not UTC midnight. Use `LocalDateRange` to convert inclusive `DateOnly` ranges into UTC `[from, toExclusive)` boundaries before querying persisted UTC timestamps.

The helper handles invalid and ambiguous local midnight transitions rather than duplicating `DateTimeKind.Local`/23:59:59 calculations in each ViewModel or report.

Reason: users outside UTC must not lose or misclassify transactions near day boundaries, and daylight-saving transitions must not create hidden gaps/overlap mistakes.

## 30. Dashboard periods are explicit domain policy

`DashboardPeriodPolicy` is the source for current financial month, previous financial month, trailing 30/90 days, and year-to-date ranges. The financial-month start remains constrained to 1–28.

Dashboard balance is a current account-state value and is not re-derived from an arbitrary selected activity period. Income, spending, net change, categories, recent history and date-sensitive budget context use the selected period.

## 31. Privacy-mode money hiding applies to passive finance surfaces

When `PrivacyMode` or `HideAmountsOnLaunch` is active, passive balance/history/report/budget/goal/recurring/reconciliation values must not reveal monetary magnitude.

`PrivacyMoneyConverter` is the shared XAML currency-aware display path for entity rows. ViewModels that generate textual summaries use equivalent privacy-aware formatting. Quantitative report charts are hidden while amounts are hidden because bar height itself would reveal magnitude.

Explicit amount-entry/edit controls may remain visible while the user is actively editing the value; the hide-amount policy is not implemented by corrupting or replacing editable finance input.

## 32. Signed charts use a true zero baseline

Report bar charts that can represent signed values (for example monthly/yearly net change) must draw positive values above zero and negative values below zero. Never render `abs(value)` as a positive bar for a negative net result.

Text/tabular equivalents remain required independently of the chart.

## 33. Report matrix is explicit and currency-aware

The current report contract includes category spending, income/expense, account balance trend, budget performance, merchant/payee, monthly comparison, yearly comparison, recurring obligations and savings progress. Tag reporting remains available through the category/tag service with explicit currency scope.

Current monthly/yearly comparison windows stop at the current local date rather than including future-dated imported rows. Recurring/savings/account/budget rows retain their own currencies; aggregate comparisons use the selected reporting currency.
