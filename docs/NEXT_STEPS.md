# Finora — Next Steps to Consider

This document is the execution roadmap for the current Finora 0.2.0 (build 2), database schema 2 source line.

It is intentionally ordered by risk. Finora is a personal-finance application, so financial correctness, migration safety, backup/restore safety, privacy, native validation, and release evidence should come before large new feature families.

The roadmap distinguishes:

- **P0 — Release blockers:** work that should be completed before representing the current source as store-ready.
- **P1 — Release-candidate completion:** packaging, store, documentation, and operational work needed around a validated release candidate.
- **P2 — Quality and product polish:** important improvements that can follow once the current core is natively proven.
- **P3 — Later-version architecture:** larger features that require new architecture/privacy/security decisions and should not be silently inserted into the current local-first design.

Buy Me a Coffee support is optional and external. It must not unlock Finora features, change finance behavior, bypass store entitlement rules, or be treated as a secure premium-license mechanism.

---

## P0 — Release blockers

### 1. Run the dependency-free structural preflight

Execute:

```bash
python build/scripts/verify_structure.py
```

Do not continue toward store release until any structural failures are understood and fixed.

The preflight is expected to guard repository structure, required documentation, local Markdown links, XML/XAML parsing, solution/project wiring, selected privacy/security invariants, version/schema drift, masked secret inputs, complete-reset wiring, and other source contracts.

### 2. Restore the exact .NET/MAUI dependency graph

On supported build hosts:

```bash
dotnet --info
dotnet workload restore src/Finora.App/Finora.App.csproj
dotnet restore Finora.sln
```

Capture the actual SDK/workload/package versions used for the release candidate.

If package restore reveals deprecations, incompatible TFMs, security advisories, or licensing concerns, resolve those before native packaging.

### 3. Run core automated tests in Release configuration

Run:

```bash
dotnet test tests/Finora.UnitTests/Finora.UnitTests.csproj -c Release
dotnet test tests/Finora.IntegrationTests/Finora.IntegrationTests.csproj -c Release
dotnet test tests/Finora.UiTests/Finora.UiTests.csproj -c Release
```

Highest-priority failures to fix first:

1. money/currency correctness;
2. transfer pairing;
3. database persistence invariants;
4. migration failures;
5. backup/restore failures;
6. recurrence/payment state corruption;
7. budget/goal/reconciliation correctness;
8. privacy/display leaks;
9. XAML/source-contract failures;
10. notification replacement consistency.

### 4. Build every native target on the correct host

Android:

```bash
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-android -c Release
```

Windows:

```powershell
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-windows10.0.19041.0 -c Release
```

iOS on macOS/Xcode:

```bash
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-ios -c Release
```

Mac Catalyst on macOS/Xcode:

```bash
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-maccatalyst -c Release
```

Do not treat successful core tests as proof that MAUI/XAML/native APIs compile on every target.

### 5. Resolve all compiler/analyzer/XAML warnings treated as errors

The repository is configured for strict analysis. Fix the source rather than suppressing warnings broadly.

Review especially:

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

### 6. Execute migration validation with synthetic copies

Required cases:

- fresh schema creation;
- schema 1 → schema 2;
- every later released schema in sequence when future versions exist;
- interrupted/failed migration behavior;
- duplicate migration execution;
- malformed legacy data rejection;
- foreign-key preservation;
- finance data unchanged except intended migration transforms.

Do not edit `schema.version` manually to make a test pass.

### 7. Perform complete encrypted backup/restore validation

Test synthetic datasets containing:

- multiple accounts;
- transactions;
- transfers;
- splits;
- tags/categories;
- budgets/custom periods;
- savings goals/contributions;
- recurring rules/occurrences;
- reconciliation history;
- receipt attachments.

Required backup cases:

- create;
- preview;
- restore to clean profile;
- restore over existing data;
- wrong password;
- tampered ciphertext/tag;
- truncated file;
- unsupported schema;
- semantically invalid authenticated graph;
- invalid receipt path;
- invalid receipt size/hash;
- linked/reparse receipt path where host supports testing;
- process termination before database commit;
- process termination after database commit;
- startup recovery after interruption.

### 8. Run data-integrity checks against clean and deliberately corrupted synthetic datasets

Verify the local integrity service catches expected corruption classes without leaking private finance contents.

Required families include:

- SQLite integrity;
- foreign keys;
- transaction/account/currency state;
- transfer pairing;
- split totals/signs;
- category cycles;
- budget period/category state;
- goal contribution history/completion state;
- recurrence dependencies/payment links;
- reconciliation links/arithmetic;
- receipt path/file/size/hash state.

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

### 10. Validate currency precision with real supported examples

At minimum verify representative:

- 0-decimal currency behavior;
- 2-decimal currency behavior;
- 3-decimal currency behavior;
- any supported 4-decimal convention.

Exercise:

- manual entry;
- account balances;
- transaction details;
- CSV import/export;
- reports;
- budgets;
- savings;
- recurring items;
- reconciliation;
- backup round trip.

Do not introduce automatic FX conversion merely to simplify tests.

### 11. Validate local-calendar behavior in multiple time zones

Required cases should include:

- UTC;
- a positive non-hour-offset time zone such as India;
- a negative offset;
- a daylight-saving zone;
- DST transition dates where supported by test environment.

Exercise:

- Dashboard periods;
- transaction date filters;
- Transaction Tools;
- reconciliation statement dates;
- monthly reports;
- yearly reports;
- budget windows;
- account trends.

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

Verify complete finance deletion removes all intended finance records and receipt files while preserving only intentionally retained preferences/security/schema state.

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

### 26. Replace bounded in-memory transaction presentation with database-level paging when data scale justifies it

Current transaction history uses bounded 50-row incremental display after query results are loaded.

A later performance pass can introduce true database paging while preserving:

- stable deterministic sort;
- filter semantics;
- cancellation;
- privacy formatting;
- revision/detail navigation;
- selected-item behavior.

Benchmark before changing the architecture.

### 27. Add performance and large-dataset benchmarks

Create synthetic datasets covering:

- 10k transactions;
- 50k transactions;
- 100k transactions where practical;
- large receipt counts;
- many recurrence rules;
- many budgets/goals;
- long report ranges.

Measure:

- startup;
- transaction search;
- report generation;
- CSV import;
- PDF export;
- backup create/restore;
- integrity scan;
- memory pressure.

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
- report period selection.

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
- large dataset/performance testing.

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

Use this order for the next major development/release workstream:

1. structural preflight;
2. restore dependencies/workloads;
3. compile core and native targets;
4. fix every compiler/XAML/analyzer error;
5. run all automated tests;
6. fix financial-correctness failures first;
7. execute migration tests;
8. execute backup/restore and interruption tests;
9. run privacy/security/integrity tests;
10. validate notifications/app lock/biometrics;
11. validate local-calendar/currency behavior;
12. run accessibility/native UI QA;
13. validate complete data deletion;
14. build signed release candidates outside source control;
15. validate store privacy/payment-link rules, including Buy Me a Coffee placement;
16. create synthetic screenshots/store metadata;
17. dependency/license/security review;
18. final release checklist/store readiness review;
19. tag/release only after evidence exists;
20. then begin P2 product polish.

---

## Definition of the next successful milestone

The strongest next milestone is not “more features.” It is:

> **A fully reproducible Finora 0.2.0 release candidate that restores, builds, tests, migrates, backs up/restores, protects private finance displays, passes native platform validation, and has evidence for every applicable release checklist item.**

After that milestone, P2 improvements can be prioritized using actual performance, accessibility, user feedback, and support data instead of guessing.

---

## Canonical project links

- Repository: https://github.com/sanskarIN/Finora
- Creator/open-source profile: https://www.github.com/sanskarIN
- Support development: https://buymeacoffee.com/sanskarIN
- Business/security: `sanskarin@outlook.in`
- Support: `supportramsandesh@gmail.com`
- Attribution: **Made by the Sanskar**
