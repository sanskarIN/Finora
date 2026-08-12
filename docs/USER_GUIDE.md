# Finora User Guide

This guide describes the current Finora 0.2.0 (build 2) local-first source line. Exact appearance can vary by platform and screen size. Native platform behavior still requires the corresponding release/device validation described elsewhere in this repository.

## 1. What Finora is

Finora is a personal-finance application for Android, Windows, iOS, and Mac Catalyst built with .NET MAUI. The current product is local-first:

- no Finora account or login is required;
- core finance workflows are designed to work without internet access;
- financial records are stored locally in app-private storage;
- backups are created only after explicit user action;
- no automatic Finora cloud synchronization is part of the current release;
- no background location collection is part of the current release;
- transaction location is manually entered text only;
- unlike currencies are not silently added together or converted with invented exchange rates.

## 2. First launch and onboarding

On first launch, onboarding explains the local-first privacy model and asks for initial preferences.

Current onboarding choices include:

- default currency;
- locale;
- financial-month start day from 1 through 28;
- optional opening balance;
- optional synthetic sample-data opt-in.

Onboarding also links to Privacy and Terms and explains that Finora can be revisited from Settings. If onboarding is opened again later, it does not intentionally duplicate an opening account or sample dataset when finance data already exists.

Before relying on the app for important records, configure a backup routine and verify at least one encrypted backup/restore using synthetic data on the target platform.

## 3. Navigation

Finora adapts its primary navigation to the device:

- phones use a bottom-tab hierarchy;
- tablets/desktops and wide windows use a flyout hierarchy;
- the adaptive boundary is implemented around a 900-pixel window width in addition to device idiom checks.

The application preserves equivalent primary sections when switching between mobile and desktop navigation roots.

## 4. Dashboard

The Dashboard is a summary surface. It can include configurable cards for:

- current balance;
- income, spending, and net activity;
- remaining budget;
- upcoming recurring items;
- top spending categories;
- savings goals;
- recent transactions;
- six-month cash flow.

Dashboard activity periods include:

- Current financial month;
- Previous financial month;
- Last 30 days;
- Last 90 days;
- Year to date.

Current account balance is not recalculated from the selected activity period. Period selection affects period-based activity while account summaries remain current-state values.

Aggregate Dashboard values are scoped to the configured reporting/default currency. Other-currency rows remain separately labeled. Finora does not silently perform FX conversion.

## 5. Accounts

Use Accounts to create and manage local account records.

Supported account types include cash, bank, credit card, wallet, savings, investment placeholder, and custom types present in the domain model.

Account capabilities include:

- account name and type;
- opening balance;
- current balance;
- currency;
- icon/color metadata where exposed;
- credit-card limit and billing day;
- account state;
- archive/restore;
- transaction history;
- reconciliation.

Credit-card billing day supports 1 through 31 in the current domain/UI alignment.

### Account currency

Account currency is a financial invariant. After transactions or recurrence depend on an account, its currency cannot be silently changed into another currency.

### Archive behavior

An active recurring rule that depends on an account blocks archival until the rule is paused, completed, or archived. Historical inactive recurrence can remain linked without continuing generation.

## 6. Transfers

The current transfer workflow supports same-currency account-to-account transfers.

A transfer is represented as two linked transaction rows:

- one negative source row;
- one positive destination row;
- equal magnitude;
- same currency;
- shared transfer group;
- reciprocal counterparty accounts.

Transfer edit/delete/restore workflows preserve both halves. Cross-currency transfers are intentionally not approximated by this model; an explicit future FX workflow is required before those can be supported correctly.

## 7. Transactions

Current transaction types include:

- Expense;
- Income;
- Refund;
- Adjustment;
- Transfer through the dedicated transfer workflow.

Quick-add supports account, category, date/time, amount, merchant/payee, payment method, manually entered location, and note fields where available.

The amount calculator uses decimal arithmetic rather than binary floating point.

### Transaction history

The current transaction list supports:

- text search;
- account filter;
- category filter;
- transaction-type filter;
- local date-range filter;
- explicit sorting;
- incremental display in 50-row pages with a Load more action when additional rows exist.

Local date filters are converted to UTC with shared inclusive-local-day/exclusive-end boundaries so local calendar dates are not treated as UTC dates.

### Transaction detail

Transaction detail supports:

- editing normal transactions;
- linked-transfer-safe editing;
- category changes;
- splits;
- tags;
- receipt attachments;
- revision history;
- soft delete and restore.

Critical edits retain a local revision record. Raw revision snapshot JSON is not intended to be exposed through diagnostics.

## 8. Categories and tags

Categories support hierarchy and maintenance workflows including:

- parent/subcategory structure;
- cycle prevention;
- reorder;
- archive/restore;
- merge/reassign.

Tag management supports create/update/archive/restore and currency-scoped reporting.

Category operations protect budget semantics so subcategory budgets cannot be silently converted into invalid root-category relationships during merge/archive/reassignment.

## 9. Splits

A transaction can be split among categories when supported by the detail workflow.

Split invariants include:

- split values are nonzero;
- split sign follows the parent transaction;
- split total equals the parent transaction amount;
- referenced categories must be valid/available.

Category and budget reporting uses split allocations when a transaction has splits instead of double-counting the full parent amount.

## 10. Receipts and attachments

Receipt/document attachments are stored in Finora app-private storage rather than arbitrary external paths.

Current controls include:

- generated internal names;
- content-type/size validation;
- byte-size metadata;
- SHA-256 checksum metadata;
- path confinement to the attachment root;
- symbolic-link/reparse-point rejection where applicable;
- open/delete/storage-usage/orphan cleanup;
- backup inclusion and restore verification.

Opening a receipt or sharing/exporting data crosses into OS/provider behavior; the receiving application/location controls its own retention and privacy.

## 11. Reconciliation

Reconciliation compares Finora's book balance with a statement ending balance.

Workflow:

1. choose an account;
2. enter the statement ending balance;
3. choose statement date;
4. optionally add a note;
5. preview book balance, statement balance, and difference;
6. optionally create an explicit adjustment if the difference should be resolved that way;
7. complete reconciliation;
8. review local reconciliation history.

A difference is not silently hidden. Reconciliation arithmetic is checked for overflow, and statement-day boundaries use the shared local-date policy.

## 12. Budgets

Budgets support:

- overall budgets;
- category budgets;
- subcategory budgets;
- weekly cadence;
- monthly cadence;
- custom cadence;
- warning threshold;
- optional rollover for supported cadence;
- explicit budget periods.

Budget actuals account for category descendants and split transactions.

Custom budgets are active only inside explicit configured periods. Explicit periods cannot overlap. Rollover affects the effective planned amount only when enabled and must remain positive.

## 13. Savings goals

Savings goals support:

- target amount;
- starting amount;
- target date;
- icon/note metadata;
- contributions;
- withdrawals;
- optional linked transaction;
- progress;
- milestones;
- contribution forecast;
- completion state.

A linked transaction must use the goal currency. Running savings history cannot validly fall below zero.

Privacy mode also hides passive savings-card and forecast amounts.

## 14. Recurring items

Recurring rules can model expense, income, transfer, and refund obligations.

Rule fields can include:

- name;
- type;
- frequency;
- custom interval;
- account;
- transfer destination when applicable;
- category when applicable;
- amount/currency;
- merchant/payee;
- note;
- start/end dates;
- grace period;
- reminder lead time.

Recurring processing is occurrence-first: preparing a due occurrence does not automatically create a finance transaction. Money is recorded when the occurrence is explicitly marked paid or partially paid.

Occurrence states include pending, paid, partially paid, skipped, and postponed. Skipped occurrences can be reopened when valid.

Rule lifecycle includes Active, Paused, Completed, and Archived behavior. Paused/archived rules stop future generation while preserving history.

## 15. Reports

Current report areas include:

- spending by category;
- income versus expense;
- account balance trends;
- budget performance;
- merchant/payee report;
- monthly comparison;
- yearly comparison;
- recurring obligations;
- savings progress;
- tag reporting through the category/tag reporting workflow.

Aggregated reports require a currency scope and do not silently convert unlike currencies.

Monthly/yearly reports use local calendar grouping and stop current-period comparison at today, so future-dated imported transactions do not appear before their local date arrives.

Charts include textual/tabular equivalents. Signed net charts use a true zero baseline so negative values render below zero instead of being shown as positive magnitude.

## 16. CSV import

CSV import is user-triggered through a system file picker and exposes a mapping/preview process before persistence.

Current mapping supports required and optional fields such as:

- Date;
- Type;
- Amount;
- Account;
- Currency;
- Category;
- Merchant/payee;
- Note;
- Payment method;
- Manual location;
- Transfer group;
- Counterparty account;
- Tags.

Import features include:

- UTF-8 validation;
- file/row limits;
- major-unit or minor-unit amount mode;
- currency-specific decimal conversion;
- fallback account;
- optional category creation;
- duplicate protection including same-batch duplicates;
- transfer pair/counterparty validation;
- preview/errors before import;
- transactional persistence.

## 17. CSV/PDF export

Finora can generate local CSV and dependency-free multipage PDF exports from supported transaction selections/workflows.

Generated share copies can temporarily exist in cache before the user invokes system share/save UI. Managed Finora share-copy files older than the configured grace period are eligible for best-effort startup cleanup; destination copies saved elsewhere are outside Finora's control.

## 18. Encrypted backup and restore

Backups are explicit user actions. Current backup cryptography uses:

- PBKDF2-SHA256 password-derived key;
- random salt;
- 210,000 iterations in the current backup implementation;
- AES-GCM authenticated encryption;
- random nonce/tag;
- authenticated Finora format magic.

Backup content includes supported finance graph data and receipt bytes. Creation validates data and attachment integrity before encryption.

Restore provides preview/validation and rejects wrong-password, tampered, malformed, incompatible, or semantically invalid snapshots.

Restore is crash-aware: the current implementation combines database transaction handling, staged attachment files, rollback/finalization directories, a durable recovery journal, and an internal commit marker. Startup recovery runs before normal finance navigation.

Finora does not recover forgotten backup passwords.

## 19. Privacy mode and hidden amounts

Privacy mode/hide-on-launch affects passive monetary display surfaces across Dashboard, accounts, transactions, tools, budgets, savings, recurring, reconciliation, reports, and transaction-detail split rows.

The shared display behavior uses a masked value such as `••••` instead of passive monetary values when hiding is active. Quantitative report charts are also suppressed while amounts are hidden so bar magnitude cannot reveal values indirectly.

Editable amount inputs are not the same as passive summary displays; users intentionally editing a transaction/account may still interact with the entered value.

## 20. App PIN and biometrics

The optional local app lock uses:

- 4–12 ASCII-digit PIN input;
- PBKDF2-SHA256 verifier;
- random salt;
- OS secure storage for small verifier material;
- fixed-time comparison;
- escalating bounded lockout;
- configurable inactivity auto-lock;
- optional biometrics/Windows Hello where supported;
- PIN fallback.

Biometric provider-specific error text is normalized before it reaches ordinary user-facing alerts. Native biometric/Hello behavior must still be tested on the target device/platform.

## 21. Notifications

Finora can use local platform notification APIs for supported reminder workflows after permission/need.

Notification payloads are intentionally generic because they may appear outside the Finora app lock.

Reminder persistence/deduplication is separate from OS scheduling. Replacement behavior schedules the new reminder before database replacement and attempts stale OS cancellation after commit. Android cancellation queries an existing PendingIntent with `NoCreate` instead of creating a new one merely to cancel it.

Platform scheduling behavior, reboot/force-stop/doze limitations, and packaged-identity requirements remain native validation topics.

## 22. Settings

Settings currently cover areas such as:

- default currency;
- locale and number/date preview;
- financial month start;
- theme;
- privacy mode/hide amounts;
- reduced motion;
- larger interface;
- default account/transaction type;
- notifications and backup reminders;
- receipt image quality;
- auto-lock;
- PIN/biometric behavior;
- sensitive-screen protection;
- Dashboard card preferences;
- local premium demo capability;
- backup/restore controls;
- revisit onboarding;
- About/legal/repository/support links;
- hidden developer tools.

About includes an optional **Support development · Buy Me a Coffee** action that opens:

https://buymeacoffee.com/sanskarIN

This external support link does not unlock Finora features, create premium entitlement, change support priority, or replace store/server-backed commercial licensing. Availability in a packaged store build is subject to the target store's current external contribution/payment-link policy.

Local premium is a development/demo flag, not tamper-proof commercial entitlement.

## 23. Developer tools

The hidden developer area can expose schema/diagnostic/feature/sample tools intended for development and support validation. Current functions include privacy-safe integrity diagnostics and deterministic synthetic sample reset.

Do not use synthetic reset when preserving real finance history matters.

## 24. Delete local finance data

Settings includes an explicit complete finance-data deletion workflow with typed confirmation. The dedicated reset service removes supported schema-2 finance records, including user categories, and cleans orphaned attachment files.

Application preferences/schema metadata needed to keep the app operable are intentionally distinguished from finance records. Exported/shared copies outside Finora cannot be deleted by this action.

## 25. Accessibility

Current source includes adaptive navigation, scalable control targets, theme preferences, larger-interface/reduced-motion preferences, semantic descriptions on important security/recurring/settings flows, and text/tabular equivalents for charts.

Native TalkBack, VoiceOver, Narrator, keyboard-focus, Dynamic Type/large-text, high-contrast, and resize behavior still require platform validation before release.

## 26. Troubleshooting and support

For technical setup/runtime issues, see [Troubleshooting](setup/TROUBLESHOOTING.md).

Support: `supportramsandesh@gmail.com`  
Business/security: `sanskarin@outlook.in`  
Repository: https://github.com/sanskarIN/Finora  
Creator/open-source profile: https://www.github.com/sanskarIN  
Optional project support: https://buymeacoffee.com/sanskarIN

A Buy Me a Coffee contribution is optional and does not create a support service level or guaranteed response time.

## 27. Next steps and current milestone

The prioritized project roadmap is [Next Steps](NEXT_STEPS.md).

Its current order is:

1. P0 release blockers: structural verification, exact restore/build/tests, migrations, backup/recovery, integrity, privacy, currency/date correctness, notifications, app lock/biometrics, attachment confinement, accessibility, and complete data deletion;
2. P1 release-candidate completion: signing/package identities, synthetic store assets, privacy/data-safety declarations, Buy Me a Coffee store-policy review, dependency/license/security review, release evidence, tags and release notes;
3. P2 quality/product polish: true database paging if needed, performance benchmarks, localization completion, native UI automation, accessibility improvements, richer import/export/backup UX;
4. P3 later-version architecture: remote accounts, cloud sync, collaboration, secure commercial entitlement, explicit FX, optional network rates, and telemetry decisions.

The recommended next milestone is a reproducible Finora 0.2.0 release candidate with actual compiler/test/native/security/privacy/store evidence rather than simply adding more features.

## 28. Important current limitations

The current release does not claim:

- Finora remote account/login;
- cloud synchronization;
- collaboration/shared-finance server features;
- server/store-backed secure commercial entitlement;
- automatic exchange-rate conversion;
- default analytics/advertising telemetry;
- universal screenshot blocking;
- guaranteed notification delivery under every OS state;
- forgotten encrypted-backup password recovery.

These are explicit boundaries, not hidden promises.
