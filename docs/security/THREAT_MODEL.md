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

## Threats and controls

### Lost or casually accessed device

Threat: another person opens Finora and sees finance data.

Controls:

- optional 4–12 digit app PIN;
- PBKDF2-SHA256 PIN verifier with random salt rather than plaintext PIN storage;
- OS secure storage for verifier material;
- escalating local lockout after failed PIN attempts;
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
- transactional restore;
- data-integrity diagnostic covering SQLite integrity, foreign keys, transfers, splits, category cycles, recurrence links, and attachment integrity;
- automated integration/migration tests.

Residual risk: filesystem/hardware corruption or untested platform/runtime defects require external backups and recovery testing.

### Transfer inconsistency

Threat: one transfer half changes/deletes independently or no longer balances.

Controls:

- shared `TransferGroupId`;
- equal/opposite amount rule;
- reciprocal counterparty accounts;
- paired edit/delete/restore workflow;
- integrity-report detection;
- integration tests.

### Duplicate recurring transactions

Threat: restart or repeated recurrence processing creates duplicate financial records.

Controls:

- persisted `RecurrenceOccurrence`;
- unique `(RecurrenceRuleId, DueOn)` index;
- idempotent due-occurrence processing;
- financial transaction is created from explicit paid/partial-paid workflow rather than every scheduler run;
- tests repeat processing across the same due date.

### Receipt/path traversal or file tampering

Threat: attachment metadata escapes app storage, points to arbitrary files, or receipt bytes are altered.

Controls:

- sanitized generated internal file paths;
- app-private attachment root;
- canonical full-path confinement check;
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
- backup is created only after explicit user action;
- no automatic upload destination.

Residual risk: weak/reused backup passwords can be guessed offline. Finora cannot recover a forgotten backup password.

### Backup tampering/truncation

Threat: corrupted or modified backup causes silent partial restore.

Controls:

- AES-GCM authentication;
- strict file length/magic validation;
- schema validation;
- attachment count/path/size/hash validation;
- staged receipt restore;
- database transaction and attachment rollback path;
- preview before destructive replacement;
- failure is reported instead of silently accepting partial data.

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
- decimal-safe money conversion;
- account/category/type/date/currency validation;
- transfer-group validation;
- duplicate protection option;
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

Threat: lock-screen notification exposes a bill/merchant/amount.

Controls:

- generic privacy-safe titles/bodies;
- local scheduling only after permission;
- no background location;
- reminder deduplication;
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
- code ownership for sensitive files;
- release-time exact dependency/license review.

Residual risk: tags such as `@v4` are mutable references. High-assurance release processes may pin actions/dependencies to reviewed immutable commits after validating compatibility.

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
- wrong/tampered backup rejection;
- migration and integrity checks;
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
- remote key escrow/recovery.

Adding any of these requires updating this threat model before implementation.
