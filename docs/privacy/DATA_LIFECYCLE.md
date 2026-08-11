# Finora Data Lifecycle

This document describes how current-release data enters, moves through, leaves, and is deleted from Finora. The current product is local-first and does not require a Finora cloud account.

## 1. Creation

Data can enter Finora through:

- onboarding preferences and optional opening balance;
- user-created accounts/categories/tags/budgets/goals/recurring rules;
- transaction quick-add/detail/edit workflows;
- linked transfers;
- reconciliation workflow;
- receipt/document selection;
- CSV import selected through system UI;
- encrypted backup restore selected through system UI;
- local platform permission/settings choices.

Location data is not collected automatically. The transaction location field exists only when the user manually enters text.

## 2. Primary local storage

### SQLite

The local SQLite database stores structured finance data including accounts, transactions, categories/tags, budgets, goals, recurrence state, transaction revisions, reconciliation history, reminder scheduling metadata, settings, audit metadata, and backup metadata.

Money is stored as signed integer minor units plus currency code. Major-unit input is converted with `decimal`; known currency precision is not universally assumed to be two decimals.

Added/modified schema-v2 records pass Domain validation at the EF persistence boundary before SQLite commit. This applies beyond account/transaction money to splits, categories/tags, budget periods, goals/contributions, recurrence occurrences, attachment metadata, revisions, reconciliations, notification schedules, app settings, audit records, and backup metadata.

### App-private receipt storage

Receipt/document bytes are stored as files under Finora's app-private `attachments` data area rather than arbitrary external paths. SQLite stores metadata such as relative path, original filename, content type, byte size, and SHA-256 checksum.

Path resolution is confined to that attachment root using platform-correct case sensitivity. Physical path traversal through symbolic links/reparse points is rejected by attachment, backup, restore/recovery, integrity, and related private-storage paths.

### OS secure storage

Small app-lock verifier/security values can be stored using platform secure storage. Large financial datasets, the SQLite database, receipts, and backups are not placed in secure storage.

A separate local PIN-enabled preference distinguishes a temporary secure-storage provider failure from a readable missing/corrupt verifier. Provider failure fails closed. If readable verifier material is actually absent/corrupt, the stale enabled marker is cleared rather than trapping the app permanently behind a verifier that no longer exists.

### Cache storage

Temporary export/diagnostic/integrity files can be written to Finora's cache directory before the user explicitly shares/saves them through system UI. Cache files are not the system of record and may be removed by the operating system.

Startup best-effort cleanup targets only known Finora CSV/PDF/backup/integrity-report share-copy filename patterns older than a 24-hour grace period. Fresh managed copies, unrelated cache files, and diagnostic logs are not deliberately removed by that cleaner. File links are deleted as links rather than traversed to targets.

## 3. Updates

Ordinary writes use asynchronous SQLite/file operations. Multi-record workflows use relational/database transactions where atomicity matters.

Examples:

- transfers update/create both linked rows together;
- critical transaction edits create local revision history;
- reconciliation can create an explicit adjustment and history entry;
- recurrence due processing persists unique occurrence state;
- recurring-rule pause/resume/archive changes persisted lifecycle state;
- custom-budget explicit-period replacement is treated as one logical database operation;
- mapped CSV import commits validated rows transactionally;
- database migrations update supported schemas transactionally.

Account currency is not allowed to change after transaction/recurrence dependencies exist. Active recurrence must be paused/completed/archived before its account can be archived.

## 4. Currency-scoped aggregation

Finora does not silently convert currencies or invent exchange rates.

- Dashboard aggregate totals use the configured reporting currency.
- Other-currency transaction/goal/recurrence rows retain their own currency.
- Category/merchant/monthly/yearly/tag report aggregates are currency-scoped.
- Recurring-obligation and savings-progress reports retain each row's actual currency.
- Same-currency transfer remains the only current transfer model.
- CSV major-unit conversion respects currency-specific minor-unit precision.

This prevents unrelated minor-unit values such as INR and USD from being added and presented as one valid total.

## 5. Local calendar dates and UTC persistence

Transactions are persisted with UTC timestamps, but a date selected by the user represents the user's local calendar day.

`LocalDateRange` converts inclusive local `DateOnly` ranges into UTC `[fromUtc, toExclusiveUtc)` boundaries. It handles invalid/ambiguous local midnight cases rather than duplicating UTC-midnight or `23:59:59` calculations in each screen.

This policy is used by Dashboard periods, Reports, transaction filters/tools, reconciliation statement-date boundaries, budget report windows, account trends, and monthly/yearly comparisons where local calendar meaning matters.

Monthly/yearly current-period comparisons stop at the current local date. Future-dated imported rows are retained in the local database if otherwise valid but are not included in current monthly/yearly comparisons before their local date arrives.

## 6. Display privacy and amount visibility

`PrivacyMode` and `HideAmountsOnLaunch` are display controls; they do not mutate persisted monetary values.

Passive finance surfaces—including account balances/history, transaction history/tools, split display, budget cards, savings cards/forecast, recurring rule/occurrence rows, reconciliation preview/history, Dashboard, and Reports—mask monetary values while those controls are active.

Currency-aware XAML display uses the shared `PrivacyMoneyConverter` where suitable. Text-generating ViewModels use equivalent privacy-aware money formatting.

Quantitative report charts are hidden while amounts are hidden because bar height would otherwise reveal relative monetary magnitude even if labels were replaced with bullets. Non-monetary labels/statuses may remain visible.

Explicit monetary input/edit controls may remain visible while the user is actively editing a value. The privacy display setting is not implemented by corrupting or replacing editable finance input.

## 7. Integrity metadata and diagnostics

Finora keeps local controls to detect inconsistency:

- SQLite foreign keys/indexes;
- account/transaction currency relationships;
- transfer group/counterparty relationships;
- split signs/totals/category links;
- category hierarchy;
- unique recurrence occurrence key;
- budget/custom-period relationships;
- savings contribution/link/completion state;
- reconciliation arithmetic/adjustment link;
- receipt byte size and SHA-256 checksum;
- schema-version setting.

The hidden developer data-integrity check can inspect SQLite integrity, foreign keys, transaction values, transfer pairs, split totals, category cycles, budgets/periods, savings contributions, recurrence dependencies/payment state, reconciliation links, and receipt path/size/checksum state.

Its exported report contains only health codes/counts rather than private finance contents.

## 8. Notifications

Local reminder schedules are stored locally and mapped to platform notification APIs after permission is granted. Notification title/body is intentionally generic because it can be visible outside the Finora app lock.

Reminder synchronization is stateful:

- disabling backup reminders cancels stale backup schedule;
- budget schedules are removed when no current threshold condition needs them;
- paused/completed/archived recurring rules have stale recurrence schedules cancelled;
- duplicate dedupe keys are not intentionally accumulated;
- replacement first asks the OS to accept the new reminder, then commits replacement/old-disabled database state, then cancels stale OS ID;
- failed replacement scheduling leaves prior enabled reminder intact;
- disabled/expired rows are retried for best-effort native cancellation during reconciliation.

On Android, cancellation looks up an existing immutable `PendingIntent` with `NoCreate`; cancellation does not create a new pending broadcast merely so it can be cancelled.

No notification workflow uploads the user's finance database.

## 9. Recurring lifecycle

A recurring rule may be Active, Paused, Completed, or Archived.

- Active rules can prepare due occurrences.
- Paused rules stop generation without deleting history.
- Resume revalidates end date/account/category/currency dependencies.
- Archived rules are removed from active rule lists but occurrence history remains.
- A due occurrence persists independently and can be Pending, Paid, PartiallyPaid, Skipped, or Postponed; skipped occurrences can be explicitly reopened.
- Paid/partial state must have a valid generated transaction; unpaid/skipped/postponed state must not silently carry generated payment data.
- A paid occurrence may retain a valid historical postponed date so the previous scheduling history is not erased after payment.

## 10. Budget period lifecycle

Budget periods use one shared interpretation policy:

- explicit periods cannot overlap;
- weekly generated windows are Monday–Sunday;
- monthly generated windows are calendar months;
- custom budgets require explicit periods and are inactive outside them;
- rollover applies only when enabled;
- checked effective planned amount must remain positive.

Replacing explicit periods is intended to be atomic so a failed replacement does not leave the budget without its prior valid period set.

## 11. Dashboard/report lifecycle

Dashboard date-sensitive cards use explicit period policy for current financial month, previous financial month, trailing 30 days, trailing 90 days, or year-to-date. Current account balance remains a current account-state value rather than being redefined by an activity date filter.

Reports currently cover category spending, income/expense, account balance trends, budget performance, merchant/payee, monthly comparison, yearly comparison, recurring obligations, savings progress, plus currency-scoped tag reporting through category/tag services.

Signed report charts use a true zero baseline so negative net values render below zero rather than being converted to positive-height bars.

## 12. Import trust boundary

CSV import begins after explicit file selection. Finora reads selected file, validates/normalizes mapping/rows locally, shows preview/validation information, and then writes accepted records to SQLite.

Import controls include UTF-8/file/row limits, currency-specific decimal conversion, account/category/tag resolution, duplicate protection, transfer pair/counterparty checks, and `long.MinValue` rejection before sign normalization.

The selected source file remains controlled by its original storage/provider; Finora does not delete it.

## 13. Export trust boundary

CSV/PDF exports are generated locally. The user explicitly invokes system share/save UI.

Once another application or location receives an export, the destination's privacy/security/storage lifecycle applies. Finora cannot automatically revoke the exported copy.

A temporary app-owned share copy can remain in Finora cache long enough for system share/save handling and is eligible for later bounded cleanup; it is not the durable system of record.

## 14. Encrypted backup creation

When requested by the user:

1. Finora reads supported local finance graph.
2. Receipt paths/files/size/checksums and physical link safety are validated.
3. Financial graph relationships/invariants are validated before encryption.
4. Snapshot plus receipt bytes is serialized.
5. A key is derived from the user-entered backup password using PBKDF2-SHA256 with random salt.
6. Payload is encrypted/authenticated with AES-GCM using random nonce/tag.
7. Finora records privacy-safe local backup metadata/audit state.
8. Sensitive plaintext/receipt buffers are cleared as early as practical after use/failure, including already accumulated receipt buffers if a later file/query/validation step fails.
9. Encrypted bytes are offered to system share/save UI.

Finora does not automatically upload backup and does not persist backup password/derived key.

The Settings password field is masked and its string is cleared from the UI field after the operation. Managed runtimes cannot guarantee immediate erasure of every immutable string copy, so documentation does not claim impossible perfect memory erasure.

## 15. Encrypted backup preview/restore

When requested by user:

1. System file picker supplies selected backup stream.
2. Finora validates basic format/size and authenticates/decrypts it with entered password.
3. Schema, unique identifiers, attachment metadata/bytes, and complete supported financial graph are validated.
4. Invalid account/currency, transfer, split, category/tag, budget/period, goal/contribution, recurrence, reconciliation, notification, setting, revision, or attachment metadata fails before destructive replacement.
5. Internal restore markers/settings are not accepted from snapshot.
6. A preview is displayed before replacement.
7. Receipt files are staged in a private temporary directory.
8. Crash-safe wrapper snapshots prior receipt tree and records recovery state.
9. Supported local database records are replaced inside a database transaction.
10. A local DB commit marker distinguishes pre-commit from post-commit recovery.
11. Attachment directories are swapped/finalized with rollback handling.
12. On next startup, recovery runs before finance navigation: pending marker restores previous tree; committed marker absence finalizes new tree.
13. Stale staging/rollback directories are cleaned only after recovery decision.
14. Invalid/tampered/incompatible backups fail instead of being silently accepted.

Restore journal/rollback/staging paths also reject symbolic-link/reparse traversal.

## 16. Diagnostics

The privacy logger stores sanitized event/type tokens only. Caller-supplied private properties, exception messages, and stack traces are intentionally not serialized by this logger.

Unobserved task exceptions are captured in sanitized path and marked observed after handling to prevent duplicate runtime escalation. Unexpected `AsyncCommand` failures are contained and routed through the same privacy-safe reporting hook.

Bound ViewModel infrastructure failures and primary user alerts use generic text rather than raw filesystem/database/cryptography/provider exception details.

Privacy logger's own current/previous file paths are rejected when they traverse symbolic links/reparse points.

Sanitized diagnostic and integrity exports are user-initiated. They must not contain account names, merchant/payee names, notes, amounts, manually entered locations, receipt names/contents, PINs, backup passwords, or signing/encryption secrets.

## 17. App lock/security values

PIN setup stores verifier material rather than plaintext PIN. PIN input is validated as 4–12 ASCII digits before PBKDF2 work. Failed attempts can trigger bounded escalating local lockout. Biometric/Windows Hello uses platform authentication and requires PIN fallback in current design.

Settings new/confirm PIN entries and lock-screen PIN entry are masked. The fields are cleared after operations/attempts.

Android biometric provider error strings are not passed directly into user-visible Result text. Stable Finora messages preserve PIN fallback without exposing OS/provider details.

Removing local finance data does not necessarily remove security/preferences automatically; UI explains which data categories are being deleted.

## 18. Full local finance-data deletion

Explicit Settings deletion flow removes supported local finance records, including user-created categories and schema-v2 finance tables, and invokes receipt-file cleanup. Schema metadata/preferences needed to keep application operable are not silently reinterpreted as finance records.

The confirmation is intentionally destructive/explicit and uses a typed confirmation phrase. Settings XAML is wired to dedicated complete finance-reset service handler rather than an older partial-delete path.

Deletion from Finora cannot delete copies user previously exported/shared/saved elsewhere.

## 19. Deterministic sample-data reset

Hidden developer reset uses synthetic local data only and requires a separate typed destructive confirmation. It is intended for development/testing, not for preserving real user finance history.

## 20. Onboarding/About metadata

Onboarding states local-first/no-account/no-automatic-upload behavior, allows explicit sample-data opt-in only when appropriate, and exposes Privacy and Terms access. It can be revisited from Settings without duplicating opening/sample data when accounts already exist.

About version/build is derived from packaged `AppInfo` metadata instead of a duplicate version literal. Repository/profile, business/support contacts, Apache-2.0 license/notices, contributing, security, and support documentation links are exposed from Settings.

## 21. Android automatic backup/device transfer

Android manifest keeps `allowBackup=false` and wires explicit legacy full-backup and Android 12+ data-extraction rule resources.

Those resources exclude root/file/database/shared preferences/external app domains from ordinary Android automatic backup/cloud backup/device transfer.

This source configuration still requires merged-manifest/package/device validation on Android before store release; source inspection alone does not prove OEM/platform behavior.

## 22. Uninstall/reset

Operating system can remove Finora app-private SQLite data, receipts, preferences, secure-storage values, and/or cache data during uninstall/reset depending on platform behavior.

Users who need to preserve finance records should save and verify an external encrypted backup before uninstall/reset. Finora has no automatic cloud recovery service in current release.

## 23. Logs/cache retention

Finora privacy diagnostic log is bounded/rotated. Managed share artifacts older than configured cleanup grace can be removed by Finora startup cleaner; other cache files may be removed independently by OS. Release/device QA should verify cache cleanup behavior but must not treat cache artifacts as durable records.

## 24. Future cloud/account/exchange features

Cloud synchronization, remote account authentication, collaboration, mobile-number authentication, server-backed entitlement validation, automatic exchange-rate conversion, and analytics/advertising telemetry by default are outside current lifecycle. Adding any requires a new server/network data-flow design, privacy update, threat-model update, retention/deletion policy, user-consent treatment, and migration strategy before release.
