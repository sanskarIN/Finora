# Finora Developer Guide

This guide is for contributors working on the current Finora 0.2.0 local-first source line.

For quick navigation use `docs/development/CODE_MAP.md`. For exhaustive tracked-file ownership and change impact use `docs/development/REPOSITORY_FILE_REFERENCE.md`.

## 1. Prerequisites

Common development tools:

- Git;
- Python 3 for dependency-free structural/repository QA;
- .NET 10 SDK compatible with the declared target frameworks;
- .NET MAUI workloads for native app builds;
- platform SDK/tooling for the target being built.

Platform requirements:

- Android: Android SDK/emulator or physical device tooling;
- Windows: Windows development host with required Windows SDK/App SDK tooling;
- iOS/Mac Catalyst: supported macOS + Xcode host.

Never place signing credentials, provisioning secrets, keystores, passwords, or private keys in the repository.

## 2. Clone

```bash
git clone https://github.com/sanskarIN/Finora.git
cd Finora
```

## 3. First verification

Run the dependency-free structural and repository QA first:

```bash
python build/scripts/verify_structure.py
python scripts/run_repo_qa.py
```

The repository QA runner executes Python developer-tool tests, tracked-file documentation coverage, and localization validation. The documentation coverage step compares `git ls-files` with `docs/development/REPOSITORY_FILE_REFERENCE.md`.

Then use the host wrapper:

Windows:

```powershell
./build/scripts/verify.ps1
```

macOS/Linux:

```bash
./build/scripts/verify.sh
```

See [Build and Run](../setup/BUILD.md) for exact target commands and host limitations.

## 4. Solution layering

Keep dependency direction:

```text
Finora.App
  ↓
Finora.Application + Finora.Infrastructure
  ↓
Finora.Domain
  ↓
Finora.Shared
```

Do not make Domain depend on MAUI/EF Core/platform APIs.

Application contracts should describe workflows without owning native UI/storage APIs.

Infrastructure should stay platform-neutral where possible.

App owns MAUI UI and target-specific platform integrations.

## 5. Money rule

This is non-negotiable for current source:

- stored/calculated money: signed `long` minor units;
- major-unit parsing/conversion: `decimal`;
- currency precision: `CurrencyMinorUnits` / `Money`;
- checked arithmetic for totals/differences;
- no `float`/`double` monetary arithmetic;
- reject unsafe extreme values such as `long.MinValue` where magnitude/negation is required;
- never add unlike currencies and label the result as one currency without an explicit reviewed FX workflow.

## 6. Date/time rule

Persist timestamps in UTC.

When the user selects local calendar dates, use `LocalDateRange` instead of constructing UTC midnight or `23:59:59` manually.

Use start-inclusive/end-exclusive query ranges.

Monthly/yearly reporting groups by local calendar date, not raw UTC month/year.

## 7. Budget rule

Use `BudgetPeriodPolicy` for budget windows/effective planned amount.

Do not recreate weekly/monthly/custom/rollover logic independently in a ViewModel/report/service.

## 8. Transfer rule

A transfer is a linked same-currency pair. Use dedicated transfer workflow.

Never mutate one transfer half through a generic single-transaction path.

Cross-currency transfer requires future explicit FX design.

## 9. Recurrence rule

Recurrence is occurrence-first.

Scheduler/due processing persists occurrences but does not create finance movement merely because time passed.

Payment actions create/link one finance transaction or transfer pair. Preserve idempotency.

## 10. Persistence boundary

`FinoraDbContext` validates Added/Modified supported entities before commit.

Service-layer validation is still required for cross-entity workflows/atomicity. Do not rely on EF validation as the only business layer.

If schema changes:

- add versioned migration step;
- preserve every released migration path;
- advance schema marker only after successful transactional step;
- update `AppConstants.DatabaseSchemaVersion`;
- update database docs/tests/backup compatibility/release checklist.

## 11. Attachments/files

App-private receipt paths must stay under the attachment root.

Reuse path safety helpers. Do not concatenate arbitrary relative paths and assume lexical prefix checks are enough.

Reject symbolic-link/reparse traversal where current helpers do so.

Keep size/hash verification aligned with backup/integrity logic.

## 12. Backup changes

Normal app flow uses crash-safe `IBackupService` registration.

Any backup schema/content change must review:

- encrypted format compatibility;
- graph validation;
- unique IDs;
- attachment validation;
- internal-setting exclusion;
- restore DB transaction;
- attachment staging;
- recovery journal/marker;
- failure injection;
- plaintext/byte-buffer cleanup;
- documentation/release migration policy.

Do not invent custom cryptography.

## 13. Privacy/logging rule

Never log finance contents or secret values.

Use `IPrivacyLogger` event token + exception type behavior.

Do not write raw exception messages/stacks, account names, merchants, notes, amounts, locations, receipt names, PINs, backup passwords, crypto/signing secrets into diagnostics.

User-visible infrastructure errors should be generic; deliberate validation text can remain actionable.

## 14. Passive amount display

Any new passive monetary UI must respect privacy/hide-on-launch and currency precision.

Prefer the shared `PrivacyMoneyConverter` when binding `(minor, currency)` values. If a ViewModel builds a textual summary/forecast, explicitly mask the amount there when hiding is active.

Do not leave raw `AmountMinor`/`BalanceMinor` labels in XAML.

If adding a chart, hiding text is not enough: remove/suppress quantitative shape when privacy mode is active.

## 15. Platform APIs

Keep native behavior behind App/platform adapters:

- notifications;
- biometrics/Windows Hello;
- sensitive-screen protection;
- file picker/share;
- package metadata.

Compile/API behavior must be validated on the target host/device.

## 16. UI and navigation

Keep five primary mobile sections unless product IA intentionally changes:

- Dashboard;
- Transactions;
- Budgets;
- Goals;
- Settings.

Secondary workflows should remain detail/tool routes rather than automatically becoming tabs.

Use `AppRoutes.DashboardRoot` for adaptive root navigation instead of hard-coded `//dashboard` when returning from onboarding/unlock/startup.

## 17. Async/cancellation

Database/disk/crypto/import/export operations should remain asynchronous. Pass cancellation tokens through application/infrastructure contracts where available.

Avoid blocking `.Result`/`.Wait()` on UI paths.

## 18. Tests required for changes

Choose the lowest appropriate layer:

- pure/domain policy → UnitTests;
- SQLite/service/backup/import/integrity → IntegrationTests;
- XAML/ViewModel/source wiring → UiTests contract tests;
- platform API behavior → native platform validation + any source contracts possible.

Every financial correctness bug should receive a regression test at the layer that can reproduce it.

## 19. Structural and repository QA

If adding a repository invariant that can be checked without .NET, extend `build/scripts/verify_structure.py` carefully when it belongs to structural/privacy/source-contract validation.

Good structural checks include:

- required file exists;
- XML/XAML parse;
- project/solution reference;
- XAML handler exists;
- version/schema drift;
- obvious forbidden money type/pattern;
- secret field masking;
- platform manifest privacy requirement.

Do not make structural preflight pretend to compile C# or prove native behavior.

Repository-wide dependency-free checks belong in `scripts/run_repo_qa.py`. The runner currently includes:

- all Python tool unit tests under `scripts/tests/`;
- `scripts/check_documentation_coverage.py`;
- `scripts/validate_localization.py`.

The primary Finora CI structural-preflight job runs both `verify_structure.py` and `run_repo_qa.py` before expensive downstream jobs.

## 20. Tracked-file documentation ownership

Every `git ls-files` path must be represented by the repository file reference.

The reference allows exact files and meaningful narrow directories. Broad declarations such as `src/`, `docs/`, `tests/`, `scripts/`, or `.github/` are rejected so the check cannot be bypassed with a catch-all.

When adding, moving, or deleting a file:

1. place it in the narrowest correct area;
2. verify the area's responsibility/change-impact description remains truthful;
3. update the reference when the responsibility changed or a new area is needed;
4. run `python scripts/check_documentation_coverage.py`;
5. run the complete dependency-free repository QA.

Coverage means documented repository ownership. It is not proof of runtime execution, native behavior, or store readiness.

## 21. Documentation requirement

A feature is not complete when code alone changes. Update relevant documentation:

- README for public capability/boundary changes;
- `docs/README.md` if navigation changes;
- feature/user guide;
- architecture/service/data-flow docs for design changes;
- database schema for persistence changes;
- threat model/data lifecycle for privacy/security/data-flow changes;
- test plan;
- release checklist/store readiness;
- changelog/status;
- `what_changed.md` as detailed ledger;
- repository file reference when tracked-file ownership/change impact changes.

## 22. Commit hygiene

Use focused imperative/conventional commit messages, for example:

- `feat(reports): add yearly comparison`;
- `fix(privacy): mask recurring amounts`;
- `test(backup): cover interrupted restore`;
- `docs(security): document app lock behavior`.

Do not bundle unrelated finance, platform, migration, and documentation changes into one opaque commit when they can be separated.

## 23. Local Git email

When committing locally and the intended identity is the project contact:

```bash
git config user.email "sanskarin@outlook.in"
```

The ChatGPT GitHub connector cannot force author/committer email fields, so connector-created commits must not be falsely claimed to use that email.

## 24. Pull request/review checklist

Before requesting review:

- structural preflight passes;
- dependency-free repository QA passes;
- tracked-file documentation coverage passes;
- relevant tests pass;
- native builds tested where change is target-specific;
- no secrets/private data added;
- money/date/currency invariants reviewed;
- migration/backup impact reviewed;
- privacy/logging reviewed;
- accessibility considered;
- docs updated;
- release notes/checklist updated when behavior changes.

## 25. Release honesty

Do not mark Android/Windows/iOS/Mac Catalyst as validated based on source inspection, documentation coverage, or an empty classic commit-status response. Retain actual CI/build/device/store evidence for release.
