# Finora Threat Model

## Scope

This threat model covers the current local-first release. Finora does not require an account or cloud service and does not automatically upload financial data or backups. Future cloud/account features require a new threat-model section before implementation.

## Assets

High-value assets include:

- account metadata and balances;
- transaction amounts, categories, merchants/payees, notes, tags, payment methods, and manually entered locations;
- receipt/attachment files;
- budgets, goals, recurring obligations, and reconciliation history;
- local SQLite database and WAL files;
- encrypted backup files;
- backup passwords while transiently entered;
- app-lock PIN verifier/salt and biometric preference;
- platform secure-storage values;
- sanitized diagnostic/integrity reports;
- release signing credentials (outside repository/application).

## Trust boundaries

1. **Finora process ↔ app-private storage** — SQLite and receipt files are trusted only after path/schema/integrity validation.
2. **Finora ↔ OS secure storage** — used for small verifier/security values, never large finance datasets.
3. **Finora ↔ system picker/share sheet** — data leaves app-private storage only after explicit user action; destination security is controlled by OS/selected app.
4. **Finora ↔ notification subsystem** — notifications are visible outside app lock, therefore contents must remain generic/privacy-safe.
5. **Finora ↔ biometric/Windows Hello APIs** — platform authentication result is trusted only as an unlock factor with PIN fallback; provider diagnostic text is not trusted for direct user display.
6. **Finora ↔ GitHub/NuGet/build infrastructure** — supply-chain and CI systems can alter build inputs; repository policies, dependency review, CodeQL, version control, and release review reduce risk.
7. **Release engineer ↔ signing infrastructure** — signing keys/certificates/profiles must remain outside source control.
8. **Authenticated backup bytes ↔ restored finance graph** — cryptographic authentication proves integrity/authenticity under password-derived key; it does not by itself prove decrypted object graph obeys current Finora domain/relationship rules.
9. **Local calendar UI ↔ UTC persistence** — user-picked dates are local calendar concepts while persisted transaction timestamps are UTC; conversion must use one reviewed boundary policy.
10. **Privacy display mode ↔ persisted finance values** — hiding an amount is presentation behavior only; persisted values remain intact and must not leak through a secondary passive UI surface or quantitative visualization.

## Threats and controls

### Lost or casually accessed device

Threat: another person opens Finora and sees finance data.

Controls:

- optional 4–12 digit app PIN;
- PBKDF2-SHA256 PIN verifier with random salt rather than plaintext PIN storage;
- OS secure storage for verifier material;
- explicit PIN-enabled marker differentiates temporary secure-storage failure from readable missing/corrupt verifier;
- temporary provider failure fails closed;
- readable missing/corrupt verifier can clear stale enabled marker instead of permanently trapping app behind nonexistent verifier;
- escalating local lockout after failed PIN attempts;
- bounded lockout arithmetic;
- configurable inactivity auto-lock;
- biometric/Windows Hello only with PIN fallback;
- privacy mode / hide amounts on launch;
- sensitive-screen protection where platform provides supported mechanism.

Residual risk: rooted/jailbroken/compromised device or attacker with OS-level privileges may bypass app-level controls.

### Passive amount leakage across UI surfaces

Threat: privacy mode hides Dashboard values but another passive finance surface still exposes raw balance/transaction/budget/goal/recurring/reconciliation/report money or raw stored minor units.

Controls:

- shared currency-aware `PrivacyMoneyConverter` for XAML money rows where appropriate;
- equivalent ViewModel-level privacy-aware formatting for generated summaries/forecasts;
- account balances/history, transaction history/tools/detail splits, budget cards, savings cards/forecast, recurring rows, reconciliation preview/history and Reports are covered by current source contracts;
- quantitative report charts are hidden while amounts are hidden because chart geometry itself reveals relative magnitude;
- structural preflight rejects raw `*Minor` values labelled as user-facing minor units;
- UI source-contract tests cover passive amount surfaces.

Residual risk: explicit edit/input fields intentionally remain visible while user is editing them; OS accessibility/screenshot behavior is governed separately.

### Screenshot/screen-recording leakage

Threat: sensitive finance UI appears in screenshots or capture streams.

Controls:

- Android uses platform secure-window behavior where implemented;
- supported Windows paths use display-affinity protection;
- unsupported platforms report limitations rather than claiming universal protection;
- privacy mode can hide amounts across passive finance surfaces.

Residual risk: camera photographs, OS/platform capture gaps, accessibility/system processes, or compromised devices may still expose screen.

### Database corruption or partial writes

Threat: force-close, disk pressure, coding defect, or interrupted operation corrupts relationships or creates inconsistent financial records.

Controls:

- SQLite relational persistence;
- foreign keys;
- WAL and busy timeout;
- database transactions for multi-record workflows;
- linked transfer pair invariants;
- unique recurrence occurrence index;
- transactional schema migration;
- transactional budget period replacement;
- crash-safe restore journal plus database commit marker;
- EF persistence-boundary validation for schema-v2 aggregate/metadata records;
- data-integrity diagnostic covering SQLite integrity, foreign keys, transactions, transfers, splits, category hierarchy, budgets, goals/contributions, recurrence, reconciliation, and attachment integrity;
- automated integration/migration/failure-path tests.

Residual risk: filesystem/hardware corruption or untested platform/runtime defects require external backups and recovery testing.

### Local-date/UTC boundary misclassification

Threat: a local date filter/report silently excludes or includes transactions near midnight because UI date is interpreted as UTC midnight, DST creates an invalid/ambiguous boundary, or `23:59:59` misses fractional timestamps.

Controls:

- `LocalDateRange` converts inclusive local `DateOnly` ranges into UTC `[fromUtc,toExclusiveUtc)` boundaries;
- invalid and ambiguous local boundary handling is centralized;
- Dashboard, Reports, transaction filters/tools, reconciliation statement date, budget report windows and account trends use shared policy where local calendar semantics apply;
- unit tests cover non-UTC fixed offsets and invalid/reversed ranges;
- monthly/yearly comparison groups by local calendar date and stops at current local day.

Residual risk: OS timezone can change; release QA must exercise timezone/DST transitions on native targets.

### Misleading signed charts

Threat: a negative financial value is drawn as a positive-height bar, visually reversing meaning or hiding a loss.

Controls:

- signed bar chart scale includes zero;
- positive values render above zero and negative values below zero;
- renderer no longer applies absolute magnitude to signed report values;
- text/tabular equivalents remain present;
- quantitative charts are hidden when privacy mode hides amounts.

### Mixed-currency aggregation

Threat: values from unrelated currencies are added together and displayed as if they were one currency, producing materially false finance information.

Controls:

- transaction currency must match account;
- account currency change blocked after transactions/recurrence depend on account;
- same-currency transfer only in current transfer model;
- Dashboard aggregate cards scoped to selected/default reporting currency;
- report/tag totals require explicit currency scope;
- recent/upcoming/goal/recurring/savings rows retain native currency;
- no implicit exchange-rate conversion.

Residual risk: users must choose/use appropriate reporting currency; Finora currently does not provide exchange-rate conversion.

### Transfer inconsistency

Threat: one transfer half changes/deletes independently or no longer balances.

Controls:

- shared `TransferGroupId`;
- equal/opposite amount rule;
- reciprocal counterparty accounts;
- paired edit/delete/restore workflow;
- integrity-report detection;
- backup graph validation;
- integration tests.

### Split/category misallocation

Threat: split transaction is double-counted or fully attributed to parent category instead of split allocations, or split references archived/missing category.

Controls:

- checked split total equals parent;
- split sign matches parent;
- split categories validated on writes;
- category spending/budget reporting uses split allocations when present;
- category-budget descendants resolved recursively;
- category reassignment protects subcategory-budget semantics.

### Budget period ambiguity

Threat: overlapping/custom periods or disabled rollover are interpreted inconsistently across dashboard/report/store paths.

Controls:

- centralized `BudgetPeriodPolicy`;
- non-overlapping explicit periods;
- custom cadence only active inside explicit periods;
- rollover included only when enabled;
- effective plan must remain positive;
- checked arithmetic;
- transactional explicit-period replacement;
- backup/integrity validation of periods.

### Duplicate or stale recurring behavior

Threat: restart/repeated processing creates duplicates, or paused/archived rules still generate money/reminders.

Controls:

- persisted `RecurrenceOccurrence`;
- unique `(RecurrenceRuleId, DueOn)` index;
- idempotent due-occurrence processing;
- transaction created from explicit paid/partial-paid workflow rather than every scheduler run;
- Pause/Resume/Archive rule lifecycle;
- resume revalidates end date/account/category/currency dependencies;
- account archival blocked while active recurrence depends on it;
- stale recurring reminder dedupe keys cancelled during synchronization;
- repeated full payment idempotent while incompatible completed mutations rejected.

### Receipt/path traversal or file tampering

Threat: attachment metadata escapes app storage, points to arbitrary files, traverses a symbolic link/reparse point, or receipt bytes are altered.

Controls:

- sanitized generated internal file paths;
- app-private attachment root;
- canonical full-path confinement check;
- platform-correct path comparison;
- symbolic-link/reparse-point rejection for private attachment traversal;
- same physical-link policy used by backup validation/staging, restore journal/recovery/rollback copy, integrity checker and privacy-log storage where applicable;
- per-file size limit;
- allowed receipt/document content types;
- stored SHA-256 checksum and byte count;
- backup verifies path/size/hash before encryption;
- restore stages/revalidates attachments;
- integrity checker detects unsafe/missing/changed attachment files.

Residual risk: checksum detects changes but does not prevent privileged attacker from changing both database metadata and local file contents.

### Backup theft

Threat: copied `.finora-backup` reveals financial contents.

Controls:

- password-derived key using PBKDF2-SHA256 with random salt and high iteration count;
- AES-GCM authenticated encryption with random nonce/tag;
- format magic used as authenticated associated data;
- derived key zeroed after cryptographic operation;
- serialized plaintext and receipt byte buffers cleared as early as practical, including accumulated buffers on later failure paths;
- backup created only after explicit user action;
- no automatic upload destination;
- Settings backup-password input is masked and cleared from UI field after operation.

Residual risk: weak/reused backup passwords can be guessed offline. Finora cannot recover forgotten backup password. Managed immutable strings cannot be guaranteed to be physically erased immediately.

### Validly encrypted but semantically invalid backup

Threat: backup can be cryptographically authentic yet contain broken relationships, invalid signs/currencies, inconsistent transfers, overlapping budget periods, impossible recurrence state, invalid metadata, or invalid reconciliation links.

Controls:

- backup creation validates graph before encryption;
- preview/restore validates decrypted unique IDs and complete financial graph before database deletion/staging commit;
- graph validation reuses Domain metadata rules for schema-v2 rows;
- graph validation covers account/currency references, transfers, splits, category hierarchy, transaction-tag links, budgets/periods, goals/contributions, recurrence, attachments, revisions, reconciliations, notification metadata, and settings boundaries;
- internal restore markers/settings not imported from snapshot data.

### Backup tampering/truncation

Threat: corrupted or modified backup causes silent partial restore.

Controls:

- AES-GCM authentication;
- strict file length/magic validation;
- schema validation;
- attachment count/path/size/hash validation;
- staged receipt restore;
- durable restore journal/commit marker;
- transactional database replacement;
- attachment rollback/finalization recovery on next startup;
- failure reported instead of silently accepting partial data.

### Restore interruption

Threat: process exits between database commit and receipt-directory replacement/finalization, exposing mismatched DB/file state.

Controls:

- restore operation gate prevents simultaneous backup/restore races;
- pre-restore attachment snapshot;
- durable journal;
- pending database marker;
- startup recovery executes before finance UI navigation;
- pending marker restores prior receipt tree; missing marker after commit finalizes new tree;
- linked recovery paths/entries rejected;
- orphan staging/rollback directories cleaned after recovery decision.

### Restore from incompatible schema

Threat: old/new backup silently maps incorrectly.

Controls:

- schema version stored in backup;
- future schema rejected;
- current restore path requires explicitly supported schema;
- database migrations versioned separately;
- migration/backup compatibility tested/documented.

### CSV import abuse/corruption

Threat: malformed or huge CSV causes memory/resource exhaustion or incorrect/future financial entries.

Controls:

- size/row limits;
- UTF-8 validation;
- explicit column mapping/preview;
- currency-specific decimal-safe money conversion;
- account/category/type/date/currency validation;
- `long.MinValue` rejection;
- transfer-group/counterparty validation;
- duplicate protection including same-batch protection;
- transactional import;
- explicit errors rather than partial silent coercion;
- current monthly/yearly comparison does not include future-dated imported rows before their date arrives.

Residual risk: CSV is user-provided data; semantic correctness still requires user review.

### Logs/diagnostics leakage

Threat: logs expose finance data or credentials.

Controls:

- privacy-aware logger;
- no private transaction payload logging by default;
- caller property dictionaries ignored by privacy logger;
- exception type/event token recorded instead of exception message/stack;
- bound infrastructure errors and primary alerts use generic user text;
- privacy-log file paths reject link traversal;
- integrity report contains counts/codes only;
- explicit sanitized export action;
- developer diagnostics avoid private finance contents.

Forbidden log content includes amounts, account names, merchant/payee names, notes, locations, receipt names/contents, PINs, backup passwords, encryption keys, signing material, and private finance identifiers unless identifier explicitly sanitized/non-sensitive.

### Notification leakage or lifecycle drift

Threat: lock-screen notification exposes bill/merchant/amount, replacement failure deletes prior reminder, or obsolete reminder remains after state changes.

Controls:

- generic privacy-safe titles/bodies;
- local scheduling only after permission;
- no background location;
- reminder deduplication;
- replacement schedules new native reminder before DB replacement, disables old row in transaction, then best-effort cancels stale native ID after commit;
- failed new scheduling leaves prior enabled reminder;
- reconciliation retries cancellation of disabled/expired rows;
- Android cancellation uses existing `PendingIntent` lookup with `NoCreate`;
- user can disable reminders.

### App-lock bypass through biometrics

Threat: biometric integration unlocks without valid platform authentication, raw provider details leak into UI, or PIN fallback is removed.

Controls:

- biometric unlock requires existing Finora PIN configuration;
- platform availability checked;
- cancellation/unavailable/error returns to locked/PIN path;
- PIN removal disables biometric preference;
- Android provider `errString` is not forwarded to Result/user text;
- structural preflight guards provider-text regression.

Residual risk: security ultimately depends on OS biometric/Hello implementation and device integrity.

### Android automatic backup/device transfer leakage

Threat: app-private finance DB/preferences/receipts are copied by ordinary Android cloud backup/device transfer despite Finora's explicit encrypted-backup design.

Controls:

- `android:allowBackup="false"`;
- legacy full-backup exclusion resource;
- Android 12+ data-extraction/cloud/device-transfer exclusion resource;
- root/file/database/sharedpref/external domains explicitly excluded;
- structural preflight guards resource presence/wiring.

Residual risk: final merged manifest and OEM/device behavior require native release validation.

### Temporary share-copy retention

Threat: user-generated CSV/PDF/backup/integrity share copies remain indefinitely in app cache after share-sheet use.

Controls:

- only known Finora share-copy patterns are eligible;
- 24-hour grace avoids deleting files while share flow may still use them;
- fresh/unrelated/diagnostic cache files preserved;
- link deletion does not traverse target;
- cleanup best-effort during serialized startup.

Residual risk: copy already shared/saved outside Finora follows destination lifecycle and cannot be revoked by Finora.

### Local premium tampering

Threat: local flag is modified to unlock premium-ready features.

Control: explicitly document flag as development/demo capability and never represent it as secure paid entitlement.

Commercial licensing requires future store/server-backed validation and new threat assessment.

### Supply-chain compromise

Threat: malicious/vulnerable dependency or CI action alters build behavior.

Controls:

- small dependency surface;
- central package versions;
- Dependabot update proposals;
- pull-request dependency review;
- CodeQL analysis;
- warnings-as-errors/latest-recommended analyzers;
- structural preflight;
- code ownership for sensitive files;
- release-time exact dependency/license review.

Residual risk: version tags for GitHub Actions are mutable references. High-assurance release processes may pin actions/dependencies to reviewed immutable commits after validating compatibility.

### Secret/signing-material exposure

Threat: repository or logs contain release credentials.

Controls:

- no production secrets in source;
- `.gitignore`/review/checklists;
- signing performed through external secure configuration;
- security issue guidance prohibits attaching credentials/private data.

## Privacy principles

- No account/login required in current release.
- No analytics/advertising telemetry by default.
- No background location collection; location manually entered only.
- No automatic backup upload.
- Android ordinary automatic backup/device transfer is explicitly excluded by source policy.
- System pickers/share sheets invoked only after explicit user action.
- Privacy-mode amount hiding applies across passive finance surfaces, not only Dashboard.
- Uninstalling without separately saved backup may remove local data.

## Security regression gates

Before release, verify:

- app-lock/PIN/biometric/capture paths on supported devices;
- provider biometric errors do not leak raw OS text;
- wrong/tampered/semantically invalid backup rejection;
- interrupted-restore startup recovery;
- migration and expanded integrity checks;
- mixed-currency isolation;
- local-calendar/day-boundary behavior around non-UTC/DST zones;
- split/category and custom-budget period behavior;
- recurrence pause/resume/archive plus stale-reminder cleanup;
- notification replacement/cancellation failure paths;
- privacy-mode amount masking and chart suppression across passive finance surfaces;
- signed charts preserve negative direction around zero;
- Android merged backup/data-transfer exclusions and device behavior;
- sanitized logs/reports;
- no new network/analytics/account dependency;
- exact package vulnerability/license review;
- platform signing credentials stay outside repository artifacts.

## Out of scope for current release

- cloud synchronization;
- remote account authentication;
- collaboration/sharing service;
- server-side entitlement validation;
- remote key escrow/recovery;
- automatic exchange-rate conversion;
- analytics/advertising telemetry by default.

Adding any of these requires updating this threat model before implementation.
