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
- release signing credentials (outside the repository/application).

## Trust boundaries

1. **Finora process ↔ app-private storage** — SQLite and receipt files are trusted only after path/schema/integrity validation.
2. **Finora ↔ OS secure storage** — used for small verifier/security values, never large finance datasets.
3. **Finora ↔ system picker/share sheet** — data leaves app-private storage only after explicit user action; destination security is controlled by the OS/selected app.
4. **Finora ↔ notification subsystem** — notifications are visible outside the app lock, therefore contents must remain generic/privacy-safe.
5. **Finora ↔ biometric/Windows Hello APIs** — platform authentication result is trusted only as an unlock factor with PIN fallback.
6. **Finora ↔ GitHub/NuGet/build infrastructure** — supply-chain and CI systems can alter build inputs; repository policies, dependency review, CodeQL, version control, and release review reduce risk.
7. **Release engineer ↔ signing infrastructure** — signing keys/certificates/profiles must remain outside source control.
8. **Authenticated backup bytes ↔ restored finance graph** — cryptographic authentication proves integrity/authenticity under the password-derived key; it does not by itself prove that the decrypted object graph obeys current Finora domain/relationship rules.

## Threats and controls

### Lost or casually accessed device

Threat: another person opens Finora and sees finance data.

Controls:

- optional 4–12 digit app PIN;
- PBKDF2-SHA256 PIN verifier with random salt rather than plaintext PIN storage;
- OS secure storage for verifier material;
- persistent PIN-enabled marker so missing/corrupt secure-storage material fails closed;
- escalating local lockout after failed PIN attempts;
- bounded lockout arithmetic;
- configurable inactivity auto-lock;
- biometric/Windows Hello only with PIN fallback;
- privacy mode / hide amounts on launch;
- sensitive-screen protection where the platform provides a supported mechanism.

Residual risk: a rooted/jailbroken/compromised device or attacker with OS-level privileges may bypass app-level controls.

### Screenshot/screen-recording leakage

Threat: sensitive finance UI appears in screenshots or capture streams.

Controls:

- Android uses platform secure-window behavior where implemented;
- supported Windows paths use display-affinity protection;
- unsupported platforms report limitations rather than claiming universal protection;
- privacy mode can hide amounts.

Residual risk: camera photographs, OS/platform capture gaps, accessibility/system processes, or compromised devices may still expose the screen.

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
- data-integrity diagnostic covering SQLite integrity, foreign keys, transactions, transfers, splits, category hierarchy, budgets, goals/contributions, recurrence, reconciliation, and attachment integrity;
- automated integration/migration/failure-path tests.

Residual risk: filesystem/hardware corruption or untested platform/runtime defects require external backups and recovery testing.

### Mixed-currency aggregation

Threat: values from unrelated currencies are added together and displayed as if they were one currency, producing materially false finance information.

Controls:

- transaction currency must match its account;
- account currency change is blocked after transactions/recurrence depend on the account;
- same-currency transfer only in the current transfer model;
- Dashboard aggregate cards are scoped to the selected/default reporting currency;
- report and tag totals require explicit currency scope;
- recent/upcoming/goal rows retain their native currency;
- no implicit exchange-rate conversion.

Residual risk: users must choose/use an appropriate reporting currency; Finora currently does not provide exchange-rate conversion.

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

Threat: a split transaction is double-counted or fully attributed to the parent category instead of split allocations, or a split references an archived/missing category.

Controls:

- checked split total equals parent;
- split sign matches parent;
- split categories are validated on writes;
- category spending/budget reporting uses split allocations when present;
- category-budget descendants are resolved recursively;
- category reassignment protects subcategory-budget semantics.

### Budget period ambiguity

Threat: overlapping/custom periods or a disabled rollover are interpreted inconsistently across dashboard/report/store paths.

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
- stale recurring reminder dedupe keys are cancelled during synchronization;
- repeated full payment is idempotent while incompatible completed mutations are rejected.

### Receipt/path traversal or file tampering

Threat: attachment metadata escapes app storage, points to arbitrary files, or receipt bytes are altered.

Controls:

- sanitized generated internal file paths;
- app-private attachment root;
- canonical full-path confinement check;
- platform-correct path comparison (case-insensitive on Windows, case-sensitive on Unix-style targets);
- per-file size limit;
- allowed receipt/document content types;
- stored SHA-256 checksum and byte count;
- backup verifies path/size/hash before encryption;
- restore stages and revalidates attachments;
- integrity checker detects unsafe/missing/changed attachment files.

Residual risk: checksum detects changes but does not prevent a privileged attacker from changing both database metadata and local file contents.

### Backup theft

Threat: copied `.finora-backup` reveals financial contents.

Controls:

- password-derived key using PBKDF2-SHA256 with random salt and high iteration count;
- AES-GCM authenticated encryption with random nonce/tag;
- format magic used as authenticated associated data;
- derived key zeroed after cryptographic operation;
- serialized plaintext and receipt byte buffers cleared as early as practical;
- backup is created only after explicit user action;
- no automatic upload destination.

Residual risk: weak/reused backup passwords can be guessed offline. Finora cannot recover a forgotten backup password.

### Validly encrypted but semantically invalid backup

Threat: a backup can be cryptographically authentic yet contain broken relationships, invalid signs/currencies, inconsistent transfers, overlapping budget periods, impossible recurrence state, or invalid reconciliation links.

Controls:

- backup creation validates the graph before encryption;
- preview/restore validates decrypted unique IDs and complete financial graph before database deletion/staging commit;
- graph validation covers account/currency references, transfers, splits, category hierarchy, transaction-tag links, budgets/periods, goals/contributions, recurrence, attachments, revisions, reconciliations, notification metadata, and settings boundaries;
- internal restore markers/settings are not imported from snapshot data.

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
- failure is reported instead of silently accepting partial data.

### Restore interruption

Threat: the process exits between database commit and receipt-directory replacement/finalization, exposing mismatched DB/file state.

Controls:

- restore operation gate prevents simultaneous backup/restore races;
- pre-restore attachment snapshot;
- durable journal;
- pending database marker;
- startup recovery executes before finance UI navigation;
- pending marker restores the prior receipt tree; missing marker after commit finalizes the new tree;
- orphan staging/rollback directories are cleaned after the recovery decision.

### Restore from incompatible schema

Threat: old/new backup silently maps incorrectly.

Controls:

- schema version stored in backup;
- future schema rejected;
- current restore path requires explicitly supported schema;
- database migrations are versioned separately;
- migration/backup compatibility is tested/documented.

### CSV import abuse/corruption

Threat: malformed or huge CSV causes memory/resource exhaustion or incorrect financial entries.

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
- explicit errors rather than partial silent coercion.

Residual risk: CSV is user-provided data; semantic correctness still requires user review.

### Logs/diagnostics leakage

Threat: logs expose finance data or credentials.

Controls:

- privacy-aware logger;
- no private transaction payload logging by default;
- integrity report contains counts/codes only;
- explicit sanitized export action;
- developer diagnostics avoid private finance contents.

Forbidden log content includes amounts, account names, merchant/payee names, notes, locations, receipt names/contents, PINs, backup passwords, encryption keys, signing material, and private finance identifiers unless an identifier is explicitly sanitized/non-sensitive.

### Notification leakage

Threat: lock-screen notification exposes a bill/merchant/amount or an obsolete reminder remains after state changes.

Controls:

- generic privacy-safe titles/bodies;
- local scheduling only after permission;
- no background location;
- reminder deduplication;
- synchronization cancels stale backup/budget/recurrence schedules;
- user can disable reminders.

### App-lock bypass through biometrics

Threat: biometric integration unlocks without a valid platform authentication or removes PIN fallback.

Controls:

- biometric unlock requires existing Finora PIN configuration;
- platform availability checked;
- cancellation/unavailable/error returns to locked/PIN path;
- PIN removal disables biometric preference.

Residual risk: security ultimately depends on OS biometric/Hello implementation and device integrity.

### Local premium tampering

Threat: local flag is modified to unlock premium-ready features.

Control: explicitly document the flag as a development/demo capability and never represent it as secure paid entitlement.

Commercial licensing requires future store/server-backed validation and a new threat assessment.

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
- No background location collection; location is manually entered only.
- No automatic backup upload.
- System pickers/share sheets are invoked only after explicit user action.
- Uninstalling without a separately saved backup may remove local data.

## Security regression gates

Before release, verify:

- app-lock/PIN/biometric/capture paths on supported devices;
- wrong/tampered/semantically invalid backup rejection;
- interrupted-restore startup recovery;
- migration and expanded integrity checks;
- mixed-currency isolation;
- split/category and custom-budget period behavior;
- recurrence pause/resume/archive plus stale-reminder cleanup;
- notification privacy;
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
- automatic exchange-rate conversion.

Adding any of these requires updating this threat model before implementation.
