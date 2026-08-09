# What Changed — Finora

Last continuation: 2026-08-09

Repository: https://github.com/sanskarIN/Finora

Current source line: **Finora 0.2.0 (build 2)**  
Current database schema: **2**  
Current branch: **main**

This file is intentionally detailed because implementation/status information that would otherwise occupy the chat is recorded here.

---

## 1. Source of truth and delivery method

The uploaded Finora Personal Finance master prompt remained the implementation source of truth. The project has been continued as a real multi-project .NET MAUI repository rather than a mockup or isolated snippet set.

Work has been split into many small focused commits as requested. The implementation keeps the existing architecture and product rules:

- .NET MAUI / C# / XAML;
- Android, iOS, Mac Catalyst and Windows targets;
- SQLite/EF Core local persistence;
- current release works without Finora account/login;
- finance data remains local unless the user explicitly imports/exports/shares/backs it up;
- no automatic cloud synchronization;
- no analytics/advertising telemetry added;
- integer minor units for stored money;
- decimal arithmetic for major-unit money conversion;
- Apache-2.0 open source;
- product identity **Finora**;
- attribution **Made by the Sanskar**.

A Git compare from the repository's original LICENSE-only state to current `main` was used as a repository-wide inventory so the audit did not depend only on GitHub code-search indexing. The inventory covered root policy/configuration files, GitHub automation, build scripts, architecture/privacy/security/release docs, all Shared/Domain/Application/Infrastructure/App source areas, all MAUI pages/viewmodels/platform files/resources, and all Unit/Integration/UI-contract test paths.

---

## 2. Git commit identity limitation

Requested commit email: `sanskarin@outlook.in`.

The GitHub connector available in this ChatGPT session can create/update files/trees/commits but does **not** expose an author/committer email override for these writes. Therefore this session cannot truthfully force `sanskarin@outlook.in` into connector-created Git commit metadata.

For local Git commits, configure:

```bash
git config user.email "sanskarin@outlook.in"
```

This limitation has not been hidden or represented as completed.

---

## 3. Core architecture already implemented

The repository contains the complete solution/project structure:

- `Finora.sln`
- `src/Finora.Shared`
- `src/Finora.Domain`
- `src/Finora.Application`
- `src/Finora.Infrastructure`
- `src/Finora.App`
- `tests/Finora.UnitTests`
- `tests/Finora.IntegrationTests`
- `tests/Finora.UiTests`

Architecture direction remains:

```text
Finora.App -> Finora.Infrastructure / Finora.Application -> Finora.Domain -> Finora.Shared
```

The app project remains UI/platform composition; Domain contains financial invariants; Application contains contracts/use-case DTOs; Infrastructure owns SQLite/import/export/backup/recovery/integrity behavior; Shared contains common primitives/configuration.

---

## 4. Money correctness hardening

### Existing behavior retained

- persisted money uses signed `long` minor units;
- UI/input major-unit parsing uses `decimal`;
- binary floating point is not used for monetary storage/calculation;
- Expense amounts are negative;
- Income/Refund amounts are positive;
- transfers use paired equal/opposite rows;
- split totals must equal the parent transaction.

### Currency-aware minor-unit precision added

New:

- `src/Finora.Domain/CurrencyMinorUnits.cs`

The Money primitive now supports currency-specific default precision:

- zero-decimal mappings for currencies such as JPY/KRW/VND where included in Finora's built-in metadata;
- normal two-decimal currencies such as INR/USD;
- three-decimal mappings such as KWD/BHD/OMR where included in the built-in metadata;
- explicit precision remains available for intentionally non-standard accounting units.

`Money.cs` now exposes currency-aware:

- `DecimalPlaces`;
- `ToMajorUnits`;
- `ToMinorUnits`;
- `FromMajorUnits`;
- `Format`.

Important: this metadata controls **minor-unit precision only**. It is not an exchange-rate source. Release QA must verify the currency metadata needed for targeted markets against current authoritative currency information.

### Extreme-value protection

Transaction/import/integrity logic now treats `long.MinValue` as invalid because negating/absolute-value conversion is unsafe. Zero-value finance transactions are also rejected by current transaction invariants.

---

## 5. Domain and persistence-boundary invariants

`DomainRules.cs` was hardened to validate:

- account names;
- currency format;
- credit-card-only limit/billing metadata;
- non-negative credit limits;
- non-zero/safe transaction amounts;
- required transaction date/time;
- semantic transaction signs;
- split sign and exact checked total;
- recurrence interval/date behavior.

`FinoraDbContext` now validates tracked `Account` and `FinanceTransaction` objects on every EF `SaveChanges` / `SaveChangesAsync` path. This prevents import/restore/direct-EF code from bypassing the same fundamental account/currency/sign rules used by normal application workflows.

Integration coverage directly bypasses normal services and verifies invalid EF writes are rejected.

---

## 6. Complete finance-data reset

New application/infrastructure reset contract/service:

- `src/Finora.Application/DataResetContracts.cs`
- `src/Finora.Infrastructure/FinanceDataResetService.cs`

The Settings destructive reset now uses this complete finance reset instead of only deleting a subset through the older store method.

The reset removes schema-v2 finance-domain data in dependency-safe order, including:

- transaction revisions;
- account reconciliations;
- notification schedules;
- transaction tags;
- transaction splits;
- attachment metadata;
- recurrence occurrences;
- goal contributions;
- budget periods;
- transactions;
- recurrence rules;
- budgets;
- savings goals;
- tags;
- categories;
- accounts;
- audit entries;
- backup metadata.

Self-referencing categories are removed leaves-first. If a category cycle prevents a safe leaves-first deletion, the database reset rolls back rather than partially deleting the finance store.

After DB commit, orphaned receipt files are cleaned.

The reset intentionally keeps:

- `schema.version`;
- non-finance app preferences;
- app-lock/PIN configuration.

Integration tests verify the finance tables are empty and schema metadata remains.

---

## 7. Hidden developer synthetic sample reset

The master prompt explicitly required a hidden developer `Reset sample data` control. That gap is now closed.

New:

- `src/Finora.Application/SampleDataContracts.cs`
- `src/Finora.Infrastructure/SampleDataService.cs`
- `src/Finora.App/Pages/SettingsPage.SampleData.cs`

The hidden developer panel now includes **Reset to synthetic sample data**.

Safety behavior:

- exact typed confirmation `RESET SAMPLE` is required;
- current finance data is cleared through the complete safe finance reset;
- system categories are reseeded;
- deterministic synthetic accounts/transactions/transfer/budget/goal/recurrence data is created;
- receipt cleanup/reminder reconciliation follows as applicable;
- existing finance records are not silently retained.

Integration tests verify deterministic counts, transfer conservation, category reseeding, currency normalization and replacement of pre-existing finance data.

---

## 8. ViewModel-layer test coverage

The UnitTests project now links the actual production `ViewModelBase.cs` source into the platform-neutral unit-test assembly.

New tests cover:

- busy-state transitions;
- previous-error clearing;
- error conversion into ViewModel state;
- concurrent `RunAsync` suppression;
- `AsyncCommand` parallel execution suppression;
- `CanExecute` restoration;
- property-change notification only when values actually change.

Target-typing/TaskCompletionSource test ambiguities were corrected so the test source is compiler-friendly.

---

## 9. Adaptive phone/tablet/desktop navigation

The UI now has two equivalent primary navigation presentations:

### Phone

Bottom tabs:

- Dashboard
- Transactions
- Budgets
- Goals
- Settings

### Tablet/desktop/wide layouts

Flyout/sidebar-equivalent primary hierarchy with matching sections.

New/updated:

- `src/Finora.App/Navigation/AppRoutes.cs`
- `AppShell.xaml`
- `AppShell.xaml.cs`
- startup/onboarding/lock navigation.

Behavior:

- device idiom and width determine navigation mode;
- sufficiently wide layouts use desktop hierarchy;
- phone layout uses tabs;
- resizing between modes routes to the equivalent primary section rather than always dumping the user onto Dashboard;
- onboarding completion uses adaptive root;
- PIN unlock uses adaptive root;
- biometric/Windows Hello unlock uses adaptive root;
- startup uses adaptive root.

UI-contract tests now explicitly include mobile tabs, desktop flyout, resize route preservation, large-text, keyboard-focus and screen-reader obligations.

---

## 10. Accessibility sizing/semantics

Global styles now include dynamic:

- `FinoraBodyFontSize`;
- `FinoraControlHeight`;
- minimum touch width;
- scalable button/input/picker/date/time/search/editor sizing;
- semantic heading levels for title/section title.

The existing larger-interface setting now updates the dynamic font/control resources.

Changed flows add/retain semantic descriptions/live error regions where practical. Report charts continue to have text/tabular equivalents so visual bars are not the only representation of financial meaning.

Native TalkBack/VoiceOver/Narrator/Dynamic Type/keyboard-focus/resize validation remains an external release gate and is **not** claimed complete merely from XAML source inspection.

---

## 11. Runtime locale and formatting

New:

- `src/Finora.Shared/CultureSettings.cs`

Behavior:

- persisted locale is validated;
- invalid locale safely falls back;
- locale is applied before normal app navigation at startup;
- onboarding validates/applies locale;
- Settings applies locale live;
- Settings shows a live money/date format preview;
- changing default currency refreshes the preview.

Culture-mutating unit tests run in a non-parallel test collection and restore previous culture state.

The app remains English-first/localization-ready; this change does **not** falsely claim every literal UI string has already been translated to Hindi or other languages.

---

## 12. App-lock fail-closed security

A critical security edge case was corrected.

Previously, a missing secure-storage salt/hash path could behave as though no PIN existed. The current implementation uses a persistent non-secret `finora.pin.enabled` marker.

Now:

- PIN enabled marker persists separately from secure verifier bytes;
- missing/malformed salt/hash while enabled fails verification;
- corrupt Base64 verifier data fails closed;
- unexpected secure-storage read failure fails closed;
- successful explicit PIN removal clears enabled marker/verifier/lockout state;
- successful PIN setup writes verifier then marks enabled;
- biometric unlock still requires a PIN fallback.

New pure `PinAttemptPolicy` centralizes and tests:

- bounded failure counter;
- lockout start after configured threshold;
- escalating delay;
- 30-minute cap;
- malformed/extreme stored counter handling.

`MauiAppLockService` reuses this tested policy.

Fail-closed secure-storage loss can result in lockout/recovery needs; this is preferable to silently bypassing a finance-data lock.

---

## 13. Central reliability/exception handling

`AppExceptionCoordinator` remains the central privacy-safe app-level exception observer.

Current hardening:

- starts once;
- captures unhandled process exceptions;
- captures unobserved task exceptions;
- normalizes event names;
- passes exception **type** through privacy logger behavior without persisting exception messages/stacks/private financial context;
- marks captured unobserved task exceptions as observed after logging to avoid duplicate escalation.

App startup/activation failures are routed through this privacy-safe coordinator.

---

## 14. iOS/Mac Catalyst/Windows packaging corrections

### iOS

`Info.plist` now contains `NSFaceIDUsageDescription` explaining optional local Finora biometric unlock.

### Mac Catalyst

`Info.plist` now contains an equivalent biometric purpose string.

### Windows

`Package.appxmanifest` source version was aligned from stale 0.1 metadata to `0.2.0.0` and includes Desktop target-family metadata.

The repository publisher remains development/source metadata, **not production signing evidence**. Production package identity/signing is still an external release-infrastructure gate.

---

## 15. Crash-safe encrypted restore across DB + receipts

A major reliability hardening was added because SQLite and the receipt filesystem cannot participate in one native atomic transaction.

New:

- `src/Finora.Application/RecoveryContracts.cs`
- `src/Finora.Infrastructure/RestoreRecoveryJournal.cs`
- `src/Finora.Infrastructure/RestoreRecoveryService.cs`
- `src/Finora.Infrastructure/CrashSafeBackupService.cs`

Production DI now uses `CrashSafeBackupService` as `IBackupService` while retaining the existing validated cryptographic `BackupService` under the wrapper.

### Recovery protocol

1. resolve any earlier interrupted restore;
2. generate random restore ID;
3. write transient DB marker `internal.restore.commit` to old database;
4. write durable app-private recovery journal with safe operation/directory state;
5. copy current receipt tree to private rollback directory;
6. mark rollback copy ready in journal;
7. execute existing encrypted restore/validation logic;
8. the committed restore transaction replaces non-schema app settings and therefore removes the pending marker;
9. recovery checks marker state:
   - matching marker present → DB replacement did **not** commit → restore previous receipt tree;
   - matching marker absent → DB replacement committed → finalize new receipt tree;
10. cleanup staging/rollback/journal/marker only after the decision.

Startup invokes recovery immediately after database initialization and **before** normal finance navigation.

If safe automatic recovery cannot resolve the state, app initialization fails instead of silently exposing mismatched database/receipt contents.

### Recovery privacy

The recovery marker/journal contain operation metadata only. They do not contain:

- backup password;
- encryption/derived keys;
- account names;
- transaction notes;
- merchants/payees;
- monetary amounts;
- manual location;
- receipt bytes/content.

### Concurrency

Backup creation, backup preview and restore are serialized through an operation gate so two operations cannot race recovery metadata.

### Recovery tests

Integration tests cover:

- pending marker restores prior receipt tree;
- missing marker finalizes committed receipt tree;
- incomplete rollback-copy state preserves untouched live tree;
- successful crash-safe round trip leaves no recovery journal/marker;
- orphan recovery staging/rollback directories are cleaned after safe journal resolution.

Native process-kill testing at every phase remains a release/device gate.

---

## 16. Data-integrity diagnostic hardening

`DataIntegrityService` now checks more than relational shape.

Current privacy-safe checks include:

- SQLite `integrity_check`;
- SQLite `foreign_key_check`;
- zero/`long.MinValue` transaction values;
- Expense/Income/Refund semantic sign violations;
- invalid currency codes;
- transaction-account/currency consistency;
- transfer pair membership/type/opposite amount/currency/counterparty/delete-state consistency;
- split value/sign/exact total;
- category hierarchy cycles;
- recurrence duplicate occurrences;
- generated-transaction references;
- receipt path confinement specifically to private `attachments` root;
- receipt file existence;
- receipt byte size;
- receipt SHA-256.

Raw-SQL corruption integration tests verify the diagnostic detects invalid sign/extreme amount states that bypass EF validation, plus unsafe receipt metadata.

The integrity report exposes codes/counts rather than private financial contents.

---

## 17. Recurring workflow state-machine completion

The recurrence workflow previously told users skipped occurrences had to be “reopened” but lacked a reopen operation. That gap is closed.

New contract/action:

- `IRecurringWorkflowService.ReopenAsync`
- ViewModel `ReopenCommand`
- UI Reopen button for skipped occurrences.

State guards now include:

- skipped occurrence must reopen before payment;
- skipped occurrence must reopen before postponement;
- only skipped occurrence can reopen to pending;
- fully paid occurrence cannot be postponed;
- repeated full-payment action is idempotent rather than creating another transaction;
- generated transaction link inconsistency stops changes;
- archived/unavailable account blocks recurring payment generation;
- account/rule currency drift blocks generation;
- recurring transfer validates both accounts/currencies.

Integration tests cover skip→reopen→pay, repeated full payment, postpone guards and archived-account behavior.

---

## 18. CSV importer hardening

`CsvImportService` was audited beyond its existing mapping/preview pipeline.

Current corrections:

- major-unit conversion uses currency-aware minor-unit precision;
- invalid currency uses domain validation;
- `long.MinValue` is rejected before `Math.Abs`/sign normalization;
- overflow during major→minor conversion becomes a row error rather than process failure;
- parse errors are counted exactly once;
- in-import duplicate fingerprints are updated so duplicate rows inside one file can be skipped;
- fallback-account currency comparison is case-insensitive;
- transfer rows still require exactly two paired rows;
- transfer amounts are checked as safe opposite values;
- mapped counterparty name is validated against paired account when supplied;
- tags are attached to both transfer halves;
- errors remain bounded to a safe returned list;
- final DB commit remains transactional.

New tests cover:

- JPY major-unit import;
- KWD major-unit import;
- `long.MinValue` minor-unit rejection;
- exact parse-error count.

---

## 19. Multi-currency dashboard/report correctness

A serious presentation/correctness issue was fixed: older dashboard snapshot logic could add unlike account currencies and label the mixed result with the default currency.

The current user-facing DashboardViewModel no longer uses mixed-currency aggregate totals.

### Dashboard behavior

- reporting currency = configured default currency;
- Current Balance sums only accounts in reporting currency;
- Income/Expense/Net use currency-filtered reporting service;
- Remaining Budget sums only matching-currency budget rows;
- Top Categories uses reporting-currency dataset;
- recent transactions display each transaction's real currency;
- upcoming recurrence displays each occurrence currency;
- savings goals display each goal currency;
- cash-flow comparison uses reporting currency;
- dashboard displays a clear notice when other account currencies exist and states they are not converted/added.

### Reports behavior

- category spending, income/expense, merchant/payee and monthly comparison use selected/default reporting currency;
- account-balance trends retain account currency;
- budget performance rows retain budget currency;
- display rows are locale-aware formatted money rather than raw minor-unit integers;
- chart numeric data remains integer minor units for accurate plotting;
- explanatory text states no silent cross-currency conversion.

Integration test `ReportCurrencyIsolationTests` uses INR + USD synthetic data and proves each aggregate report sees only its selected currency.

Finora still does not implement automatic exchange-rate conversion; that remains explicit later-version work.

---

## 20. Structural preflight expansion

`build/scripts/verify_structure.py` now performs dependency-free repository checks for:

- required root policy/status/doc files;
- CI/test/release docs presence;
- XML/XAML/RESX/project parse validity;
- empty source/resource files;
- unfinished placeholder markers;
- ProjectReference target existence;
- solution project target existence;
- XAML `x:Class` matching partial class;
- XAML event-handler method existence;
- application display/build version metadata;
- Windows package version consistency;
- README application-version mention;
- DB schema constant/document consistency;
- suspicious Domain `float`/`double` monetary-field declarations;
- Android `allowBackup=false`;
- Android `usesCleartextTraffic=false`.

The schema-document regex was corrected to tolerate Markdown punctuation while still requiring the declared version.

The script still explicitly states it is **not** a compiler/analyzer/test/device/signing/store substitute.

---

## 21. CI topology correction

`.github/workflows/ci.yml` now separates concerns correctly:

### Structural preflight

Ubuntu + Python.

### Core tests

Ubuntu + .NET 10:

- UnitTests restore/test;
- IntegrationTests restore/test;
- UiTests contract restore/test;
- test result artifacts uploaded.

### Windows + Android

Windows runner:

- .NET 10;
- MAUI workload install;
- app restore;
- Windows Release build;
- Android Release build.

### Apple

macOS runner:

- .NET 10;
- MAUI workload install;
- app restore;
- iOS Release build;
- Mac Catalyst Release build.

The earlier attempt to run a full-solution formatting gate on the core Ubuntu job was removed because it could become a known formatting-only blocker for legacy compact source and because the MAUI solution requires workloads not installed in the core job.

Compiler/analyzer/test/native-build gates remain authoritative. Repository defaults still use nullable analysis, warnings-as-errors, latest-recommended analyzers and deterministic builds.

Workflow action major versions remain conservative because live web verification is unavailable in this environment.

---

## 22. Local verification scripts

`build/scripts/verify.ps1` and `verify.sh` now match CI behavior.

Both:

- run structural preflight;
- show `.NET` information;
- restore/test UnitTests, IntegrationTests and UiTests.

Native builds are host-aware:

- Windows PowerShell builds Windows + Android;
- macOS shell builds iOS + Mac Catalyst;
- Linux performs core verification and delegates native MAUI builds to CI-supported hosts;
- `FINORA_SKIP_MAUI=1` intentionally allows core-only verification.

A core-only run is never represented as native release evidence.

---

## 23. Repository documentation updated in this continuation

Materially updated:

- `DECISIONS.md`
- `docs/architecture/DATABASE_SCHEMA.md`
- `docs/TEST_PLAN.md`
- `docs/setup/BUILD.md`
- `docs/setup/TROUBLESHOOTING.md`
- `docs/security/THREAT_MODEL.md`
- `docs/releases/RELEASE_CHECKLIST.md`
- `docs/releases/STORE_READINESS.md`
- `docs/privacy/DATA_LIFECYCLE.md`
- `README.md`
- `CHANGELOG.md`
- `PROJECT_STATUS.md`
- `what_changed.md`

Documentation now explicitly covers:

- currency-aware minor units;
- no silent multi-currency totals;
- persistence-boundary validation;
- complete finance reset;
- synthetic developer reset;
- adaptive navigation;
- runtime locale formatting;
- fail-closed PIN verifier state;
- crash-safe DB/receipt restore recovery;
- recurrence reopen behavior;
- stronger integrity checks;
- corrected CSV import behavior;
- current CI/build topology;
- exact external release gates.

---

## 24. Tests added/expanded in this continuation

### Unit

- `DomainRulesTests`
- `MoneyTests`
- `ViewModelBaseTests`
- `CultureSettingsTests`
- `PinAttemptPolicyTests`
- serial culture-test collection.

### Integration

- complete finance reset coverage;
- deterministic sample reset coverage;
- direct EF persistence-invariant coverage;
- restore recovery state-decision coverage;
- crash-safe backup round trip;
- raw corruption/integrity diagnostic coverage;
- recurring reopen/idempotency/account availability coverage;
- currency-aware CSV import coverage;
- multi-currency report-isolation coverage;
- existing transfer/migration/reconciliation/revision/attachment/recurrence/import tests retained.

### UI contract

- primary mobile routes;
- equivalent desktop routes;
- privacy/recovery/reset flow obligations;
- mobile tabs;
- tablet/desktop flyout;
- resize preservation;
- large text;
- keyboard focus;
- screen-reader semantics.

UI contract tests are **not** native device UI automation and are not represented as such.

---

## 25. Representative commit messages from this continuation

This continuation intentionally used many focused commits. Messages include:

### Domain / persistence / data reset

- `fix(domain): harden account and transaction invariants`
- `test(domain): cover sign currency and split invariants`
- `feat(data): add complete finance reset contract`
- `feat(data): implement transactional full finance reset`
- `feat(data): register complete finance reset service`
- `feat(data): route settings reset to complete finance deletion`
- `feat(data): complete destructive reset flow in settings`
- `fix(data): harden reset rollback and database error handling`
- `test(data): verify complete finance reset preserves schema metadata`
- `fix(database): enforce finance invariants on every EF write path`
- `test(database): enforce invariants on direct EF writes`

### ViewModels / sample data

- `test(viewmodel): link production viewmodel base into unit tests`
- `test(viewmodel): cover busy errors notifications and async command gating`
- `feat(sample): add deterministic sample data reset contract`
- `feat(sample): implement deterministic synthetic finance dataset`
- `feat(sample): register synthetic sample data service`
- `feat(sample): expose developer sample-data reset control`
- `feat(sample): wire developer synthetic dataset reset flow`
- `test(sample): verify deterministic developer sample reset`

### Navigation / accessibility / localization

- `feat(navigation): add adaptive root route resolver`
- `feat(navigation): add desktop flyout alongside mobile tabs`
- `feat(navigation): switch adaptively between tabs and desktop flyout`
- `feat(navigation): use adaptive dashboard root at startup`
- `feat(navigation): route onboarding completion adaptively`
- `feat(navigation): route unlock completion adaptively`
- `test(navigation): cover adaptive mobile and desktop route contracts`
- `feat(accessibility): enforce scalable touch and input control sizing`
- `feat(localization): add validated runtime culture coordinator`
- `feat(localization): apply saved locale before app UI starts`
- `feat(localization): validate and apply locale and currency settings live`
- `feat(localization): show live locale and number-format preview`
- `test(localization): cover culture normalization and safe fallback`
- `feat(localization): validate and apply onboarding locale immediately`

### Security / reliability / platform metadata

- `fix(reliability): mark captured task failures as observed`
- `fix(security): fail closed on missing or malformed PIN verifier state`
- `feat(security): add bounded PIN attempt policy`
- `test(security): cover bounded PIN lockout escalation`
- `fix(security): reuse tested PIN lockout policy in app service`
- `fix(ios): declare Face ID purpose for optional app unlock`
- `fix(maccatalyst): declare biometric unlock purpose`
- `fix(windows): align package version and desktop device family`

### Crash-safe restore

- `feat(recovery): add interrupted restore recovery contract`
- `feat(recovery): add durable attachment restore journal`
- `feat(recovery): recover interrupted database and attachment restores`
- `feat(recovery): register interrupted restore recovery service`
- `feat(recovery): run interrupted restore recovery before navigation`
- `fix(recovery): preserve untouched live receipts before restore swap`
- `feat(recovery): track pending marker and rollback readiness`
- `feat(recovery): support pending-marker restore transaction protocol`
- `feat(backup): wrap encrypted restore with crash-safe journal recovery`
- `feat(backup): activate crash-safe backup and restore service`
- `fix(backup): serialize backup preview and restore operations`
- `test(recovery): verify interrupted restore commit and rollback decisions`
- `fix(recovery): clean orphan restore directories after journal resolution`

### Integrity / recurrence

- `fix(integrity): detect unsafe amounts signs currencies and receipt paths`
- `test(integrity): detect raw corruption and unsafe receipt metadata`
- `feat(recurring): add skipped occurrence reopen contract`
- `feat(recurring): implement reopen and completed-payment guards`
- `feat(recurring): expose skipped occurrence reopen action`
- `feat(recurring): add reopen action to occurrence workflow UI`
- `test(recurring): cover reopen idempotency and unavailable account guards`

### Currency / import / reports

- `feat(money): add currency-aware minor unit precision`
- `feat(money): make money conversion currency-precision aware`
- `test(money): cover zero two and three decimal currencies`
- `fix(import): make CSV money parsing currency-aware and overflow-safe`
- `test(import): cover currency precision overflow and error counts`
- `fix(test): align CSV assertions with import result contract`
- `fix(dashboard): prevent cross-currency aggregation and mislabeled amounts`
- `fix(dashboard): explain reporting currency and separated account currencies`
- `fix(reports): format report rows with their actual currencies`
- `fix(reports): display locale-aware formatted money rows`
- `test(reports): prove aggregated reports isolate currencies`

### CI/build/docs

- `ci: enforce Finora repository invariants in structural preflight`
- `fix(ci): accept documented Markdown schema version formatting`
- `fix(ci): separate core formatting from MAUI workload builds`
- `fix(ci): remove formatting false blocker from compiler gates`
- `fix(build): make PowerShell verification host-aware and deterministic`
- `fix(build): make shell verification host-aware and core-first`
- `docs(decisions): record currency recovery navigation and reset invariants`
- `docs(database): document restore marker recovery and currency invariants`
- `docs(test): expand coverage for recovery currency reset and adaptive UI`
- `docs(build): align verification commands with current CI topology`
- `docs(security): add fail-closed PIN restore recovery and currency threats`
- `docs(release): add restore recovery currency and adaptive UI gates`
- `docs(status): record current Finora hardening and external release gates`
- `docs(changelog): record Finora 0.2.0 reliability and currency hardening`
- `docs(readme): document current recovery currency and adaptive Finora source`
- `docs(store): add recovery currency and adaptive navigation validation matrix`
- `docs(troubleshooting): add recovery currency and adaptive navigation diagnostics`
- `docs(privacy): document restore recovery and synthetic reset data lifecycle`
- `docs(status): finalize complete Finora 0.2.0 hardening ledger`

The repository also retains the large earlier implementation commit history covering the original 0.2.0 finance/application/UI/schema-v2 work.

---

## 26. Repository-wide inventory audit

To avoid relying only on GitHub search indexing, the repository was compared from the original commit that contained only the Apache LICENSE to current `main`.

The comparison inventory showed the project now contains the expected groups:

### Root/configuration/policy

- editor/git attributes and ignores;
- central build/package props;
- solution;
- README/changelog/status/decisions;
- privacy/security/support/terms/contributing/code of conduct;
- third-party notices/license;
- this `what_changed.md` ledger.

### GitHub automation/community

- CI;
- CodeQL/dependency review;
- Dependabot;
- CODEOWNERS;
- issue templates/config;
- pull-request template.

### Build/docs

- verification scripts;
- architecture docs;
- DB schema docs;
- privacy/data lifecycle;
- security threat model;
- test plan;
- build/troubleshooting;
- branding guidance;
- release/store checklists.

### Source

- Shared;
- Domain;
- Application;
- Infrastructure;
- MAUI App;
- pages/viewmodels/services;
- Android/iOS/Mac Catalyst/Windows platform source/manifests;
- app icon/splash/raw/legal/localization/style resources.

### Tests

- Unit;
- SQLite integration;
- UI-contract.

This inventory confirms the project tree was audited as a whole. It does **not** replace compilation or platform tests.

---

## 27. Verification actually possible in this ChatGPT environment

The active execution container currently has Python but **does not have**:

- `dotnet`;
- `csc`;
- `mcs`;
- `msbuild`.

Therefore this current continuation cannot truthfully execute:

- NuGet restore;
- C# compilation;
- `dotnet test`;
- MAUI workload restore/build;
- Android emulator/device tests;
- Windows packaged tests;
- iOS simulator/device tests;
- Mac Catalyst native tests;
- signing/notarization;
- store packaging/upload.

Earlier in the project, a dependency-free structural validation was executed against the then-current staging tree and passed its XML/XAML/project/reference/handler/empty/placeholder checks. Since substantial source hardening has been committed after that older local staging pass, **no claim is made that the latest repository state has locally passed the newer structural script**.

The structural script itself has been expanded and committed so GitHub CI/developer environments with repository checkout can execute it.

No claim is made that Finora is bug-free.

---

## 28. CI/status truthfulness

GitHub Actions is configured as the actual compiler/platform gate for the latest source:

- structural preflight;
- core unit/integration/UI-contract tests;
- Windows/Android MAUI builds;
- iOS/Mac Catalyst MAUI builds.

CodeQL/dependency-review automation is also present.

A successful repository write/commit is **not** a successful CI result. If no status/check is registered yet for the latest commit when queried, Finora remains pending compiler/platform evidence rather than being represented as passed.

---

## 29. External validation still required before store publication

Even with current source completeness/hardening, release evidence is still required for:

### Compiler/toolchain

- exact .NET 10 SDK/workload compatibility;
- NuGet dependency graph;
- warnings-as-errors/analyzers;
- all current tests.

### Android

- Release AAB build/sign;
- notification scheduling/permission/restart;
- biometric/PIN fallback;
- `FLAG_SECURE`;
- file/share/import/export/receipt/backup;
- phone/tablet adaptive UI;
- restore kill/recovery tests;
- package upgrade/migration;
- TalkBack/large text/theme/reduced motion.

### Windows

- Release package/MSIX identity/signing;
- Windows Hello;
- scheduled toast behavior;
- display-affinity behavior;
- file/share/backup/recovery;
- resizable flyout/sidebar + keyboard/high DPI;
- migration/package upgrade;
- Narrator.

### iOS / Mac Catalyst

- supported Xcode archive/build;
- signing/provisioning/notarization;
- LocalAuthentication + purpose text;
- UserNotifications;
- file/share/backup/recovery;
- iPhone/iPad/desktop adaptive layouts;
- VoiceOver/Dynamic Type/keyboard as applicable;
- migration/app upgrade.

### Financial correctness

- release-market currency minor-unit metadata verification;
- JPY/KWD-style conversion/import regression on actual release binaries;
- multi-currency dashboard/report isolation;
- no invented exchange rate;
- migration/integrity/recovery failure injection.

### Privacy/security/accessibility

- no new network/account/telemetry requirement;
- generic lock-screen notifications;
- fail-closed PIN verifier loss;
- sanitized logs/integrity/recovery metadata;
- screen reader;
- large text;
- keyboard/focus;
- contrast;
- reduced motion;
- real store privacy/data-safety declarations.

These gates are now explicitly listed in `PROJECT_STATUS.md`, `docs/TEST_PLAN.md`, `docs/releases/RELEASE_CHECKLIST.md` and `docs/releases/STORE_READINESS.md`.

---

## 30. Intentionally later-version product work

The current master design still reserves these for later architecture/version work:

- Finora cloud synchronization;
- Finora online account/login system;
- collaboration/shared-finance backend;
- remote key escrow/account recovery;
- server/store-backed tamper-resistant commercial entitlement;
- automatic exchange-rate conversion/cross-currency consolidated total.

The local premium flag remains explicitly a non-secure development/demo capability.

---

## 31. Product identity preserved

- Product: **Finora**
- Version source: **0.2.0 (2)**
- Database schema: **2**
- Repository: https://github.com/sanskarIN/Finora
- Creator/open-source profile: https://www.github.com/sanskarIN
- Business/security: `sanskarin@outlook.in`
- Support: `supportramsandesh@gmail.com`
- Attribution: **Made by the Sanskar**
- License: Apache-2.0

---

## 32. Current state

The repository now contains a substantially hardened, local-first Finora 0.2.0 source implementation covering the master prompt's current-release architecture, finance flows, persistence, schema/migration, import/export, receipts, encrypted backup/restore, crash recovery, privacy/security, developer tools, adaptive navigation, localization readiness, accessibility foundations, tests, CI and release documentation.

The remaining work before a production store release is **external validation**, not a hidden claim of completion: compile/test the latest repository on the real .NET/MAUI/native toolchains, execute the native device/accessibility/recovery/signing/store gates, fix any issues those gates reveal, and only then tag/publish a release candidate.
