# Finora — Next Steps to Consider

Last prioritized review: **2026-08-18**

This document is the execution roadmap for the current Finora 0.2.0 (build 2), database schema 2 source line.

It is intentionally ordered by risk. Finora is a personal-finance application, so financial correctness, migration safety, backup/restore safety, privacy, native validation, and release evidence should come before large new feature families.

The roadmap distinguishes:

- **P0 — Release blockers:** work that should be completed before representing the current source as store-ready.
- **P1 — Release-candidate completion:** packaging, store, documentation, and operational work needed around a validated release candidate.
- **P2 — Quality and product polish:** important improvements that can follow once the current core is natively proven.
- **P3 — Later-version architecture:** larger features that require new architecture/privacy/security/migration decisions and should not be silently inserted into the current local-first design.

Buy Me a Coffee support is optional and external. It must not unlock Finora features, change finance behavior, bypass store entitlement rules, or be treated as a secure premium-license mechanism.

Concrete automated evidence is retained in `docs/testing/CI_EVIDENCE.md`. Large-dataset benchmark methodology and current runtime evidence are retained in `docs/testing/PERFORMANCE_BENCHMARKING.md`.

---

## P0 — Release blockers

### Automated source-validation gates completed on 2026-08-18

The current verified source candidate is:

`8a8e7e51a2bacecdc58405d3d5301e79f3d78c8b`

Verified Finora CI run:

`32127759802`

Verified CodeQL run:

`32127759687`

Verified Dependency Review run:

`32127759673`

The following source-validation work is no longer an unknown for that exact candidate:

- ✅ dependency-free structural preflight passed;
- ✅ NuGet/test-project restores completed on GitHub-hosted CI;
- ✅ Unit tests passed: 102/102;
- ✅ Integration tests passed: 179/179;
- ✅ UI-contract tests passed: 38/38;
- ✅ total automated result: **319/319 passed, 0 failed, 0 skipped**;
- ✅ Windows Release source build passed with `WindowsPackageType=None`;
- ✅ Android Release source build passed;
- ✅ iOS Release source build passed on a GitHub macOS runner;
- ✅ Mac Catalyst Release source build passed on a GitHub macOS runner;
- ✅ CodeQL passed;
- ✅ Dependency Review passed;
- ✅ XAML compiled-binding diagnostics `XC0022`, `XC0023`, and `XC0025` remain enforced as errors and all four native source builds passed under that policy;
- ✅ interactive transaction history performs search/filter/sort/count/offset/page-size work in SQLite/EF Core through `ITransactionHistoryStore`, with 50-row UI pages, a 200-row store maximum, soft-delete exclusion, deterministic page boundaries for a fixed result set, total count/`HasMore`, and stable last-applied-query Load more behavior;
- ✅ paging regression coverage proves a 120-row 50/50/20 boundary without duplicates/missing IDs for a fixed result set, filter-before-count/page behavior, all supported sorts, invalid page/range rejection, soft-delete exclusion, and payment/location/account/category free-text search;
- ✅ a dedicated `Finora.Performance` Release harness builds with **0 warnings and 0 errors** under the repository warnings-as-errors policy;
- ✅ a bounded 10k synthetic CI performance smoke runs startup, history, reports, and integrity against production services and retains JSON evidence;
- ✅ an on-demand workflow supports 10k/50k/100k datasets, selectable operations and iterations, while keeping real finance data out of benchmark fixtures;
- ✅ schema 1 → schema 2 migration source coverage includes target-schema validation, schema-version guards, fresh initialization/reopen, data preservation/idempotence, malformed-target rollback, and legacy foreign-key corruption rejection;
- ✅ encrypted-backup hostile-input coverage includes wrong password, ciphertext tamper, truncation, authenticated unsupported schema, authenticated semantic corruption, and receipt path/size/hash corruption;
- ✅ receipt SHA-256 metadata is required by backup validation and missing checksum metadata has direct regression coverage;
- ✅ deliberate integrity-corruption coverage includes split totals, account currency, missing/changed receipts, invalid checksum metadata, category cycles, and foreign-key violations;
- ✅ linked restore-journal and linked rollback-copy recovery cases fail closed in direct integration coverage;
- ✅ complete finance-data reset coverage preserves unrelated app settings;
- ✅ representative 0-, 2-, 3-, and 4-decimal currency classes are covered with JPY, INR, KWD, and CLF through conversion/import/export/report/account/budget/savings/recurring/reconciliation/encrypted-backup paths with exact minor-unit assertions;
- ✅ CSV export → preview/import into a second SQLite database preserves exact stored minor units across those precision classes;
- ✅ encrypted backup → complete reset → restore preserves exact minor units across those precision classes and finishes with a healthy integrity check;
- ✅ shared local-calendar conversion covers UTC, UTC+05:30, UTC-07:00, deterministic DST start/end, multi-day and reversed ranges;
- ✅ `FinanceStore` budget and legacy Dashboard windows use shared local-calendar `[from,toExclusive)` UTC boundaries instead of UTC-midnight assumptions, with deterministic UTC+05:30, UTC-07:00 and DST integration coverage;
- ✅ primary CI action majors run on the Node-24-compatible versions introduced by commit `6ba519bf69174c68b67f8595872546a259c783dc`.

The bounded 10k performance smoke is observational evidence, not a universal timing guarantee. The current exact evidence does **not** include runtime execution of the performance harness's complete `--operations all` profile, CSV/PDF/backup performance operations, or 50k/100k comparison profiles. Those remain explicit P2 evidence tasks even though the complete harness compiles.

This source/test/build evidence does not prove signed packaging, installed prior-version upgrade behavior on every target, real process-kill/low-disk recovery, physical-device behavior, accessibility, or store approval.

Documentation-only commits after the exact source candidate may advance the branch or `main`; runtime source evidence remains anchored to `8a8e7e51a2bacecdc58405d3d5301e79f3d78c8b` until a newer exact runtime/source candidate is recorded.

### 1. Structural preflight — completed automated evidence, keep as every-commit gate

Execute locally when available:

```bash
python build/scripts/verify_structure.py
```

The preflight guards repository structure, required documentation, local Markdown links, XML/XAML parsing, solution/project wiring, selected privacy/security invariants, version/schema drift, masked secret inputs, complete-reset wiring, and other source contracts.

The current 2026-08-18 source candidate passed this gate. It remains mandatory for every final release head.

### 2. Restore exact .NET/MAUI dependency graph — CI restore proven; release inventory still required

On supported release hosts:

```bash
dotnet --info
dotnet workload restore src/Finora.App/Finora.App.csproj
dotnet restore Finora.sln
```

CI has proven the current test/native dependency graph can restore and build. Before signed release, still capture the exact SDK/workload/direct/transitive package inventory used for the release artifact and perform license/vulnerability review.

### 3. Core automated tests — completed automated evidence, keep as regression gate

Run:

```bash
dotnet test tests/Finora.UnitTests/Finora.UnitTests.csproj -c Release
dotnet test tests/Finora.IntegrationTests/Finora.IntegrationTests.csproj -c Release
dotnet test tests/Finora.UiTests/Finora.UiTests.csproj -c Release
```

Current exact source-candidate evidence is 102 unit + 179 integration + 38 UI-contract = **319/319 passed**.

Highest-priority future failures to fix first remain:

1. money/currency correctness;
2. transfer pairing;
3. database persistence invariants;
4. migration failures;
5. backup/restore failures;
6. recurrence/payment state corruption;
7. budget/goal/reconciliation correctness;
8. privacy/display leaks;
9. XAML/source-contract failures;
10. notification replacement consistency;
11. local-calendar/timezone boundary regressions;
12. transaction-history paging/filter/sort boundary regressions.

### 4. Native Release source builds — completed automated evidence; packaging/device gates remain

Commands remain:

Android:

```bash
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-android -c Release
```

Windows source validation:

```powershell
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-windows10.0.19041.0 -c Release -p:WindowsPackageType=None
```

iOS on macOS/Xcode:

```bash
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-ios -c Release
```

Mac Catalyst on macOS/Xcode:

```bash
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-maccatalyst -c Release
```

All four source-build targets passed on the exact current 2026-08-18 candidate. Windows MSIX generation/signing, Android signed AAB packaging, Apple provisioning/signing/archive/notarization, and device behavior are separate unresolved gates.

### 5. Compiler/analyzer/XAML warning policy — current strict gate completed

The repository is configured for strict analysis. Fix source rather than suppressing warnings broadly.

The current source retains explicit typed binding contracts across the affected pages/templates and promotes `XC0022`, `XC0023`, and `XC0025` to errors.

During the 2026-08-18 paging continuation, intermediate candidate `6617a0b6b07b4cd4befcd48ae22c476ab0b917d1` failed the integration build because a new merchant-sort assertion violated analyzer `CA1861`. The assertion was corrected with `Assert.Collection`; the warning was not suppressed. Earlier precision/calendar work similarly fixed xUnit analyzer findings instead of weakening policy. These failures are useful evidence that analyzer warnings remain release-blocking.

Continue reviewing especially:

- nullability;
- async/cancellation;
- platform conditional APIs;
- XAML handler/binding issues;
- obsolete native APIs;
- EF Core query warnings;
- integer-overflow paths;
- culture/date conversion assumptions;
- filesystem/path operations;
- cryptographic API warnings.

### 6. Migration validation — automated core completed; installed-upgrade evidence still required

Current automated production-path coverage includes:

- ✅ fresh schema creation and reopen;
- ✅ schema 1 → schema 2;
- ✅ invalid/current/future schema-version guards;
- ✅ target changed-table validation before version advance;
- ✅ SQLite foreign-key/integrity validation before version advance;
- ✅ duplicate migration execution/idempotence;
- ✅ representative legacy attachment data preservation and intended filename backfill;
- ✅ malformed target-schema rollback without advancing the marker;
- ✅ synthetic legacy foreign-key corruption rejection.

Remaining release evidence:

- install the actual prior released build/profile on each target family where applicable;
- populate a representative complete synthetic finance graph;
- upgrade with the candidate without clearing app data;
- verify the schema marker, finance graph, receipts, and settings after startup migration;
- run the normal data-integrity service after migration;
- create a new encrypted backup from the migrated profile and restore it into a clean candidate profile;
- repeat for every later released schema in sequence when future versions exist.

Do not edit `schema.version` manually to make a test pass.

### 7. Encrypted backup/restore — hostile and precision automated matrix expanded; native failure injection still required

Automated source/integration coverage directly proves:

- ✅ create/preview/restore current schema;
- ✅ wrong password rejection;
- ✅ changed ciphertext/authentication rejection;
- ✅ truncated encrypted file rejection;
- ✅ authenticated unsupported schema rejection;
- ✅ authenticated semantic relationship corruption rejection;
- ✅ authenticated receipt lexical path escape rejection;
- ✅ authenticated receipt-size drift rejection;
- ✅ authenticated receipt SHA-256 drift rejection;
- ✅ missing/invalid receipt checksum metadata rejection;
- ✅ linked/reparse receipt-path refusal where host link creation is supported;
- ✅ pending-marker recovery restores prior attachments;
- ✅ committed restore finalizes new attachments;
- ✅ incomplete rollback-copy behavior remains fail-safe;
- ✅ linked recovery-journal refusal;
- ✅ linked rollback-copy refusal while preserving live receipts and recovery state;
- ✅ internal restore settings/markers remain excluded from portable state;
- ✅ receipt/plaintext buffer clearing paths remain under regression coverage;
- ✅ JPY/INR/KWD/CLF exact minor-unit preservation across encrypted backup, authenticated preview, finance reset, restore, and integrity verification.

Still required with synthetic release-candidate profiles on native hosts/devices:

- restore over a realistic existing profile;
- process termination before database commit;
- process termination after database commit;
- relaunch/startup recovery after interruption;
- low disk during copy/staging;
- locked/unavailable file behavior;
- platform-native picker/share/save behavior;
- final receipt byte/checksum round trip after those flows.

### 8. Data-integrity diagnostics — automated corruption matrix expanded; release-profile run still required

Automated tests directly inject and detect representative corruption including:

- ✅ SQLite foreign-key violations;
- ✅ transaction/account currency mismatch;
- ✅ transfer pairing drift through existing regression coverage;
- ✅ split-total corruption;
- ✅ category parent cycles;
- ✅ budget period/category corruption through existing regression coverage;
- ✅ goal contribution/completion corruption through existing regression coverage;
- ✅ recurrence dependency/payment corruption through existing regression coverage;
- ✅ reconciliation corruption through existing regression coverage;
- ✅ missing receipt files;
- ✅ receipt-size drift;
- ✅ changed receipt bytes/SHA-256 drift;
- ✅ invalid/missing receipt checksum metadata;
- ✅ unsafe attachment path/link behavior through existing regression coverage;
- ✅ healthy integrity state after multi-precision encrypted backup/reset/restore;
- ✅ healthy full integrity scan on the current 10k synthetic performance smoke profile.

Still required before store-ready status:

- run the integrity service against the complete migrated synthetic release profile;
- run it again after full backup/restore on a clean profile;
- confirm healthy reports on valid profiles;
- confirm issue-code/count output remains sanitized on deliberately corrupted copies;
- perform native filesystem corruption cases that cannot be faithfully modeled on every CI host.

### 9. Validate privacy mode screen by screen

Native QA should verify that passive monetary values do not remain visible through:

- Dashboard;
- Accounts;
- Account Detail;
- Transactions;
- Transaction Tools;
- Transaction Detail passive split rows;
- Budgets;
- Savings;
- recurring rules/occurrences;
- reconciliation preview/history;
- report rows;
- quantitative report charts;
- screen-reader announcements.

Explicit edit controls may show values when the user intentionally edits them.

### 10. Validate currency precision — automated core now broad; native UI matrix remains

Automated coverage explicitly exercises representative:

- ✅ 0-decimal JPY behavior;
- ✅ 2-decimal INR behavior;
- ✅ 3-decimal KWD behavior;
- ✅ 4-decimal CLF behavior.

The exact automated paths include:

- ✅ `Money` major-unit conversion/rounding;
- ✅ CSV major-unit import;
- ✅ CSV export values and preview;
- ✅ CSV `AmountMinor` export → import into a second database without value drift;
- ✅ account opening/current balances;
- ✅ budget planned/actual calculations;
- ✅ savings target/start/contribution/current values;
- ✅ recurring rule/occurrence/generated paid transaction;
- ✅ reconciliation preview/adjustment/final balance;
- ✅ income/expense reports;
- ✅ encrypted backup preview/reset/restore/integrity.

Remaining native/manual validation should still exercise:

- manual entry/editor controls and keyboards;
- currency-specific display formatting on every target;
- account/transaction edit round trips;
- platform file-picker/share flows around CSV/backup;
- assistive technology announcements of formatted values;
- any supported currency metadata outside the representative precision classes.

Do not introduce automatic FX conversion merely to simplify tests.

### 11. Validate local-calendar behavior — deterministic automated matrix expanded; actual platform timezone QA remains

Automated shared conversion covers:

- ✅ UTC;
- ✅ positive non-hour UTC+05:30;
- ✅ negative UTC-07:00;
- ✅ deterministic daylight-saving start;
- ✅ deterministic daylight-saving end;
- ✅ multi-day exclusive end boundary;
- ✅ reversed-range rejection.

Automated store-level regression coverage proves:

- ✅ budget one-day local boundary in UTC+05:30;
- ✅ legacy Dashboard one-day boundary in UTC+05:30;
- ✅ legacy Dashboard one-day boundary in UTC-07:00;
- ✅ legacy Dashboard DST-start boundary.

Production `FinanceStore` uses shared local-calendar `[from,toExclusive)` boundaries for budget periods and the legacy Dashboard aggregate instead of UTC-midnight construction.

Native/manual validation is still required on actual target hosts/devices for:

- Dashboard periods;
- transaction date filters;
- Transaction Tools;
- reconciliation statement dates;
- monthly reports;
- yearly reports;
- budget windows;
- account trends;
- device timezone changes between launches;
- actual OS daylight-saving behavior where applicable.

### 12. Validate notification lifecycle on native platforms

Exercise:

- permission granted;
- permission denied;
- permission later revoked;
- create reminder;
- deduplicated replacement;
- failed replacement keeps prior valid reminder;
- stale schedule cancellation;
- expired schedule reconciliation;
- paused/archived recurring rule cleanup;
- app restart;
- Android `NoCreate` cancellation behavior;
- OS limitations after force-stop/reboot/doze as applicable.

### 13. Validate app lock and biometric fallbacks

Test:

- PIN set/change/remove;
- invalid PIN format;
- successful unlock;
- repeated failed attempts and lockout;
- inactivity lock;
- secure-storage unavailable/error behavior;
- missing/corrupt verifier state;
- biometric success;
- biometric cancel;
- biometric unavailable;
- biometric lockout/error;
- PIN fallback;
- masked secret input clearing;
- no raw provider error text in user-visible messages.

### 14. Validate attachment and filesystem confinement

Test:

- allowed receipt types;
- rejected type;
- file-size limit;
- generated internal filenames;
- open/delete;
- orphan cleanup;
- storage usage;
- missing file;
- modified checksum;
- lexical path traversal;
- symbolic-link/reparse traversal where host permits;
- backup/restore round trip.

### 15. Run accessibility validation on every target family

Android:

- TalkBack;
- larger font/display sizes;
- touch target size;
- focus order;
- privacy-mode announcements.

Windows:

- keyboard-only navigation;
- Narrator;
- high DPI;
- resize/multi-monitor;
- focus visibility.

Apple:

- VoiceOver;
- Dynamic Type where applicable;
- keyboard/focus on Mac Catalyst;
- reduced motion;
- light/dark appearance.

### 16. Validate full local finance-data deletion

Automated reset coverage proves finance records can be cleared while unrelated app settings remain. Native release QA should additionally verify complete finance deletion removes all intended finance records and receipt files while preserving only intentionally retained preferences/security/schema state.

Then verify:

- app can continue operating;
- new account/transaction can be created;
- encrypted external backups remain outside Finora's control;
- no stale receipt orphan remains;
- integrity checker reports healthy empty/current state.

---

## P1 — Release-candidate completion

### 17. Produce signed release artifacts outside the repository

Do not commit signing secrets.

Prepare:

- Android signed AAB;
- Windows signed package/MSIX configuration as applicable;
- iOS signed/provisioned archive;
- Mac Catalyst signed/notarized or App Store artifact depending on distribution path.

### 18. Finalize package identity/publisher values

Review current source metadata before public distribution.

In particular, validate:

- `in.sanskar.finora` package/application ID;
- Windows publisher identity;
- display version/build number;
- Apple bundle/provisioning configuration;
- store listing identity.

Do not change package identifiers casually after users have installed a released version.

### 19. Create synthetic store screenshots and marketing assets

Use only invented finance data.

Recommended screenshot set:

1. Dashboard;
2. transaction search/filter/sort;
3. account detail/reconciliation;
4. budgets;
5. savings goals;
6. recurring obligations;
7. reports;
8. CSV import mapping/preview;
9. privacy/backup settings.

Never use real bank, card, receipt, merchant, or personal data.

### 20. Verify the final store privacy/data-safety declarations

Declarations must be based on the final packaged binary and dependency graph.

Check:

- analytics/telemetry SDKs;
- permissions;
- biometric use;
- notifications;
- Android backup/device-transfer behavior;
- local file picker/share behavior;
- encrypted backup behavior;
- external Buy Me a Coffee link if allowed by the target store's current policy.

Store policies can change; verify them in the live console before submission.

### 21. Review Buy Me a Coffee placement against current store policies

Current source treats `https://buymeacoffee.com/sanskarIN` as an optional external support link.

Before each store submission verify whether the store permits that link in the packaged application and in the intended region/category.

If a store disallows or restricts external contribution/payment links inside apps, adjust the packaged UI for that store while keeping the repository/project website documentation accurate.

Do not imply that buying coffee:

- unlocks premium features;
- purchases a subscription;
- grants financial functionality;
- changes support priority;
- creates secure entitlement state.

### 22. Finalize support and public documentation links

Verify these canonical destinations:

- repository: https://github.com/sanskarIN/Finora
- creator: https://www.github.com/sanskarIN
- Buy Me a Coffee: https://buymeacoffee.com/sanskarIN
- business/security: `sanskarin@outlook.in`
- support: `supportramsandesh@gmail.com`

Check About, README, docs index, support guide, store metadata, release notes, and any future website copy for drift.

### 23. Perform exact dependency-license and vulnerability review

Before binary release:

- enumerate exact restored direct/transitive packages;
- review licenses;
- update `THIRD_PARTY_NOTICES.md` if required;
- review security advisories;
- review CodeQL/dependency-review/Dependabot findings;
- document accepted risk explicitly rather than ignoring alerts.

### 24. Create a release candidate tag only after evidence exists

Do not tag an unverified source commit as production-ready.

Before tagging, ensure release evidence is attached/recorded for:

- structural preflight;
- restore/build;
- automated tests;
- native builds;
- migration;
- backup/recovery;
- privacy;
- accessibility;
- store packaging.

### 25. Prepare release notes and known limitations

Release notes should clearly state:

- version/build;
- schema version;
- major user-visible capabilities;
- privacy/local-first model;
- backup compatibility;
- known native limitations;
- intentionally later-version features.

Do not claim guaranteed data-loss prevention, guaranteed notification delivery, universal screenshot blocking, automatic exchange conversion, cloud recovery, or bug-free operation.

---

## P2 — Quality and product polish

### 26. Database-level transaction history paging — implemented and automated

Interactive transaction history uses `ITransactionHistoryStore` to apply search/filter/sort/count/offset/page-size in SQLite/EF Core before materialization.

Current behavior:

- 50-row UI pages;
- bounded store page size (`MaximumPageSize = 200`);
- deterministic secondary ordering across page boundaries for a fixed result set;
- free-text/account/category/type/local-date filter semantics preserved;
- soft-deleted rows excluded before count/page;
- total matching count + `HasMore`;
- last applied query is snapshotted so **Load more** cannot mix applied and un-applied filter states;
- legacy full-result search contract remains for bounded workflows.

Automated integration coverage proves a 120-row 50/50/20 boundary with no duplicates/missing rows for a fixed result set, filter-before-count/page behavior, all supported sorts, invalid range/page rejection, soft-delete exclusion, and extended search fields.

### 27. Performance and large-dataset tooling — implemented; full comparison evidence remains

Implemented source/tooling now includes:

- ✅ standalone `tools/Finora.Performance` net10.0 harness using production Application/Infrastructure services;
- ✅ batched synthetic seeding for transactions plus bounded accounts, budgets, goals, recurrences, and SHA-256-verified receipt files;
- ✅ startup measurement;
- ✅ first/deep history paging, broad/selective search, and amount-sort measurement;
- ✅ long-range report measurement;
- ✅ CSV export plus isolated CSV import round-trip measurement with exact-count/no-skip/no-invalid correctness checks;
- ✅ PDF export measurement;
- ✅ encrypted backup creation and restore measurement with restored transaction/attachment count checks;
- ✅ full `DataIntegrityService` measurement that fails on unhealthy synthetic state;
- ✅ managed-heap and process working-set observations;
- ✅ machine-readable JSON output with dataset/runtime/runner/evidence-policy metadata;
- ✅ normal CI 10k bounded smoke;
- ✅ on-demand 10k/50k/100k workflow with selectable operations and 1–3 iterations;
- ✅ documentation in `docs/testing/PERFORMANCE_BENCHMARKING.md`.

Exact current executed evidence:

- ✅ 10,000 synthetic transactions seeded successfully in Finora CI run `32127759802`;
- ✅ startup/history/reports/integrity smoke completed successfully;
- ✅ performance artifact `9321290557`, SHA-256 `97eb07bf963491e8d89d45798b21aa99d0da312b931c3ea25b17e2dae5accb46`;
- ✅ complete performance project compiled with zero warnings/errors.

Still required before claiming complete large-dataset comparison evidence:

- ⏳ execute a 10k `--operations all` profile so CSV/PDF/backup performance paths have runtime evidence in the harness itself;
- ⏳ execute 50k `--operations all` on a comparable runner;
- ⏳ execute 100k `--operations all` on a comparable runner;
- ⏳ retain JSON artifacts/digests and compare trends on the same runner class;
- ⏳ investigate any outlier with profiling/query inspection before changing finance architecture.

Timing remains observational. Do not introduce a release-breaking arbitrary millisecond threshold without a separately approved and reproducible performance SLO.

### 28. Expand localization coverage

Current source is English-first and localization-ready with an initial Hindi resource structure.

Next localization work can include:

- moving remaining hard-coded user-facing strings into resources;
- completing Hindi translation;
- pluralization and date/number review;
- text expansion testing;
- RTL readiness review before claiming RTL support;
- translator/reviewer workflow.

Do not mark a language complete until every user-visible screen/error/notification is reviewed.

### 29. Add native UI automation where practical

Source-contract tests are valuable but are not native UI automation.

Potential automation targets:

- onboarding;
- add transaction;
- transfer;
- privacy mode;
- transaction sort/load-more;
- backup password flow using synthetic files;
- Settings destructive confirmation;
- app lock flow;
- report period selection;
- currency-specific amount entry;
- timezone/date-boundary display.

### 30. Improve accessibility continuously

After baseline native validation, address discovered issues in:

- screen-reader labels;
- focus order;
- keyboard shortcuts;
- large-text layout;
- chart alternatives;
- contrast;
- reduced-motion behavior;
- desktop navigation density.

### 31. Add richer import diagnostics without leaking private data

Potential improvements:

- downloadable sanitized import-error summary;
- row-number/error-code aggregation;
- mapping presets stored locally;
- safer date-format detection;
- dry-run statistics.

Do not log imported finance contents.

### 32. Expand export configuration

Possible later local-only options:

- date-range export;
- account/category filters;
- report export;
- optional receipt manifest;
- additional safe PDF layouts.

Keep exports explicit user actions.

### 33. Improve backup usability without weakening security

Potential improvements:

- backup history metadata stored locally;
- clearer verified-backup status;
- optional reminder frequency controls;
- explicit restore compatibility explanation;
- backup test/verification action that does not replace live data.

Never store the backup password for convenience unless a new secure design is approved.

### 34. Expand deterministic sample datasets

Add multiple synthetic profiles for:

- single-currency basic user;
- multi-currency user without FX conversion;
- credit-card/reconciliation user;
- budget-heavy user;
- recurring-heavy user;
- savings-heavy user;
- large dataset/performance testing;
- currency-precision matrix user;
- local-date/timezone boundary user.

### 35. Improve contributor workflow

Consider:

- contributor architecture diagram;
- issue labels by layer/risk;
- release-blocker label;
- automated docs/link/preflight job artifacts;
- coding conventions examples;
- test-data builders;
- migration template/checklist.

---

## P3 — Later-version architecture

The following items are intentionally not current-release promises. Each requires a separate architecture/security/privacy/migration decision before implementation.

### 36. Remote Finora accounts

Would require decisions for:

- authentication;
- account recovery;
- secure token storage;
- data ownership;
- deletion/export rights;
- server availability;
- privacy policy changes;
- breach response.

Do not add login merely to support a cosmetic feature.

### 37. Cloud synchronization

Would require:

- conflict resolution;
- end-to-end data model/versioning;
- encryption strategy;
- offline-first merge semantics;
- attachment synchronization;
- retention/deletion;
- multi-device consistency;
- migration from local-only profiles.

### 38. Shared/collaborative finance spaces

Would require:

- permissions/roles;
- invitation/revocation;
- conflict handling;
- audit history;
- privacy boundaries between members;
- server-side authorization.

### 39. Server/store-backed commercial entitlement

The current local premium demo flag is not secure entitlement.

A real paid tier would require platform/store or server-backed verification and a clear answer for:

- offline grace periods;
- restore/reinstall behavior;
- family/shared purchases if applicable;
- refund/revocation;
- platform differences;
- privacy impact.

Buy Me a Coffee must remain separate from this entitlement system unless a future explicitly designed commercial model says otherwise and complies with store rules.

### 40. Explicit foreign-exchange workflow

Finora currently blocks implicit cross-currency transfer conversion.

A future FX design would need:

- source amount;
- destination amount;
- explicit user-entered or sourced exchange rate;
- rate timestamp/source if remote;
- fees;
- rounding rules;
- realized/unrealized reporting decisions;
- audit/revision semantics;
- offline behavior.

Never silently invent a rate.

### 41. Optional remote exchange-rate lookup

If ever added, make it explicit and optional.

It would introduce network/privacy/security considerations and must not break offline finance workflows.

### 42. Analytics/crash telemetry

Current product does not depend on remote analytics/advertising telemetry.

If future telemetry is considered, decide:

- opt-in/opt-out model;
- data minimization;
- finance-field exclusion;
- retention;
- vendor/dependency risk;
- privacy disclosures;
- regional compliance.

Prefer privacy-preserving diagnostics and never send finance contents by default.

---

## Recommended execution order

The completed automated source-build, database-paging, bounded-10k performance smoke, precision/calendar, and data-safety regression block is now an every-commit gate rather than the next unknown. Continue in this order:

1. keep structural preflight + **319-test** + bounded 10k performance smoke + four-target source-build + CodeQL + Dependency Review gates green;
2. execute a complete installed prior-version/schema-1→2 upgrade profile and run integrity/backup validation after migration;
3. execute native process-interruption, low-disk, locked-file, and relaunch recovery validation;
4. validate privacy/security/currency/time-zone behavior on native UI, using JPY/INR/KWD/CLF and real target timezone settings;
5. validate notifications/app lock/biometrics/Windows Hello and capture limitations;
6. validate attachment/file/share/import/export flows and path confinement on native platforms;
7. run accessibility/native UI QA;
8. validate complete data deletion on native hosts/devices;
9. build signed release candidates outside source control;
10. validate store privacy/payment-link rules, including Buy Me a Coffee placement;
11. create synthetic screenshots/store metadata;
12. complete exact dependency/license/security review;
13. final release checklist/store readiness review;
14. tag/release only after evidence exists;
15. run the full 10k/50k/100k comparable `all` performance matrix and use the results to prioritize any P2 optimization rather than guessing;
16. then continue localization, native UI automation, accessibility polish, import/export UX, backup usability, sample data, and contributor workflow work.

---

## Definition of the next successful milestone

The next release milestone remains evidence-focused because database-backed transaction paging, performance tooling, bounded 10k performance smoke, migration, hostile-backup, integrity-corruption, restore-link safety, reset-safety, representative currency precision, local-calendar source behavior, source compilation, and automated tests now have concrete CI evidence:

> **A fully reproducible Finora 0.2.0 release candidate that preserves the current green automated/source-build/data-safety/paging/performance-tooling/precision/calendar gates and adds complete installed-upgrade, real recovery failure-injection, native privacy/security/currency/time-zone/accessibility, signed packaging, dependency-review, and store evidence for every applicable release checklist item.**

The next performance-evidence milestone is separate and explicit:

> **Comparable retained 10k, 50k and 100k `--operations all` JSON results on the same runner class, with correctness gates passing and any optimization justified by measured evidence.**

After the release milestone, P2 improvements can be prioritized using actual performance, accessibility, user feedback, and support data instead of guessing.

---

## Canonical project links

- Repository: https://github.com/sanskarIN/Finora
- Creator/open-source profile: https://www.github.com/sanskarIN
- Support development: https://buymeacoffee.com/sanskarIN
- Business/security: `sanskarin@outlook.in`
- Support: `supportramsandesh@gmail.com`
- Attribution: **Made by the Sanskar**