# Finora Testing Guide

This guide complements `docs/TEST_PLAN.md` with practical commands, test-layer selection, synthetic-data rules, and release evidence expectations.

Current dated GitHub Actions evidence is recorded in [CI_EVIDENCE.md](CI_EVIDENCE.md). The test plan and this guide define what should be exercised; the evidence record states what actually executed for a specific commit.

## Test philosophy

Finora is a local finance application. Tests should prove correctness without using real financial data.

Use synthetic data only for automated/manual test fixtures.

A passing structural check does not mean C# compiled. A passing unit test does not mean SQLite workflows passed. A passing integration test does not mean Android/iOS/Windows/Mac APIs work on a device. Keep evidence layers separate.

## 1. Structural preflight

Run:

```bash
python build/scripts/verify_structure.py
```

Current checks include repository files, XML/XAML parsing, project references, XAML handlers, version/schema drift, suspicious floating-point money declarations, Android backup/privacy configuration, secret masking/prompt patterns, biometric error redaction, and passive raw-minor XAML patterns.

Expected output ends with a pass count and explicitly states that the check is not a compiler/native substitute.

## 2. Unit tests

Project:

```text
tests/Finora.UnitTests/Finora.UnitTests.csproj
```

Run:

```bash
dotnet restore tests/Finora.UnitTests/Finora.UnitTests.csproj
dotnet test tests/Finora.UnitTests/Finora.UnitTests.csproj -c Release --no-restore
```

Use unit tests for:

- Money/currency precision;
- DomainRules;
- pure budget/date policies;
- DashboardPeriodPolicy;
- LocalDateRange;
- culture normalization;
- PIN attempt policy;
- ViewModelBase/AsyncCommand behavior where compiled as dependency-free test source.

## 3. Integration tests

Project:

```text
tests/Finora.IntegrationTests/Finora.IntegrationTests.csproj
```

Run:

```bash
dotnet restore tests/Finora.IntegrationTests/Finora.IntegrationTests.csproj
dotnet test tests/Finora.IntegrationTests/Finora.IntegrationTests.csproj -c Release --no-restore
```

Use integration tests for behavior requiring SQLite, EF Core, file system, crypto, or multiple services.

Current areas include:

- finance store;
- accounts/transfers;
- transaction edit/revisions;
- reconciliation;
- categories/tags;
- budgets;
- goals;
- recurrence;
- CSV import;
- reports;
- backup/restore/recovery;
- persistence-boundary invariants;
- integrity diagnostics;
- reset/sample data;
- notifications;
- privacy logger/temp artifact/path safety;
- migrations.

Each test should create isolated temporary DB/storage and clean it best-effort.

## 4. UI-contract tests

Project:

```text
tests/Finora.UiTests/Finora.UiTests.csproj
```

Run:

```bash
dotnet restore tests/Finora.UiTests/Finora.UiTests.csproj
dotnet test tests/Finora.UiTests/Finora.UiTests.csproj -c Release --no-restore
```

These tests inspect source/XAML contracts. They are not Appium/native UI automation.

Current contracts cover areas such as:

- adaptive navigation roots;
- Dashboard/report bindings;
- Settings masked fields/reset/About links;
- transaction sort/paging;
- signed chart implementation;
- onboarding Privacy/Terms links;
- passive amount privacy surfaces.

## 5. Combined host wrappers

Windows:

```powershell
./build/scripts/verify.ps1
```

macOS/Linux:

```bash
./build/scripts/verify.sh
```

The scripts run structural/core tests and perform host-appropriate MAUI work where configured. Linux cannot substitute for Apple/Windows native builds.

## 6. Money test cases

Every money-affecting change should consider:

- positive/negative sign;
- zero if invalid;
- `long.MinValue`;
- checked overflow;
- currency normalization;
- 0-decimal currency such as JPY behavior;
- 2-decimal currency such as INR/USD behavior;
- 3-decimal currency such as KWD behavior;
- known 4-decimal metadata where applicable;
- unlike-currency isolation;
- decimal rounding at half-unit boundaries.

Do not test finance totals with `double` expectations.

## 7. Local date/time cases

For local calendar filters/reports test:

- UTC time zone;
- non-UTC fixed offset;
- range start/end inclusion;
- reversed range rejection;
- month boundary;
- year boundary;
- financial-month start before/after configured day;
- future-dated row excluded from current month/year;
- DST-capable local environment in native/manual validation where practical.

## 8. Database tests

Use SQLite provider, not an in-memory fake, for relational behavior.

Test:

- foreign keys;
- transaction rollback;
- unique indexes;
- direct `FinoraDbContext.SaveChanges` invariant rejection;
- migration path;
- database lock/force-close scenarios where practical;
- data-integrity check after migration.

## 9. Transfer tests

Prove:

- exactly two rows;
- same currency;
- opposite sign/equal magnitude;
- reciprocal counterparties;
- shared group;
- atomic create;
- edit preserves pair;
- delete/restore preserves pair;
- generic transaction edit rejects transfer half;
- cross-currency transfer rejects.

## 10. Backup tests

Automated coverage should include:

- round trip;
- wrong password;
- tampered ciphertext/tag;
- format length/magic;
- future/unsupported schema;
- semantic graph corruption;
- attachment missing/size/hash/path errors;
- symbolic-link/reparse path where supported;
- accumulated sensitive buffer cleanup paths;
- internal marker exclusion;
- crash-safe pending-marker rollback;
- committed-marker finalization;
- incomplete rollback copy safety;
- orphan directory cleanup.

Manual/native failure injection should kill the process at restore phase boundaries.

## 11. CSV import tests

Test:

- encoding/size/row limits;
- mapping;
- major/minor modes;
- currency precision;
- invalid amount/type/date/currency;
- fallback account;
- category creation;
- tags;
- duplicate existing/same-batch;
- transfer counterparties/groups;
- exact invalid-row count;
- transactional failure.

## 12. Privacy tests

Prove that:

- privacy/hide-on-launch masks passive money;
- charts do not reveal hidden magnitude;
- raw `*Minor` values are not user-facing labels;
- backup/PIN fields are masked;
- biometric provider text is normalized;
- diagnostics omit exception message/stack/properties/finance values;
- notification payloads are generic;
- Android backup/data-transfer exclusions remain wired.

## 13. Accessibility tests

Automated source contracts can check semantics/configuration, but native validation must include:

- TalkBack;
- VoiceOver;
- Narrator;
- keyboard focus;
- large text/Dynamic Type;
- high contrast;
- reduced motion;
- adaptive resize;
- chart text equivalence.

## 14. Platform build commands

See [Build and Run](../setup/BUILD.md) for current target commands.

Do not mark platform build complete without the correct workload/host.

For the 2026-08-15 strict candidate, GitHub-hosted Windows, Android, iOS, and Mac Catalyst Release source-build evidence is retained in [CI_EVIDENCE.md](CI_EVIDENCE.md). Those successful compiler jobs do not replace package signing or device validation.

## 15. Synthetic test data rules

Never commit or attach real:

- bank/account identifiers;
- transaction history;
- receipt images;
- addresses/locations;
- backup files containing real records;
- PINs/passwords;
- signing credentials.

Use generated names, dates, amounts, and attachments.

## 16. Regression-test rule

For every confirmed financial correctness/security/privacy/data-loss bug:

1. reproduce with synthetic data;
2. add a test that fails before the fix;
3. fix at the lowest correct layer;
4. keep the regression test permanently unless the feature is removed;
5. update docs/release checks when behavior changes.

## 17. Release evidence

Retain evidence for the exact release commit:

- structural preflight output;
- restored dependency graph;
- unit test result;
- integration result;
- UI-contract result;
- native platform build logs;
- device/simulator/emulator smoke results;
- migration/backup failure-path evidence;
- accessibility/security checks;
- signing/package/store validation.

The current concrete automated record is [CI_EVIDENCE.md](CI_EVIDENCE.md). It records exact commit/run/job identities and explicitly leaves package/device/store gates unresolved. An empty GitHub classic combined-status response is not passing CI evidence.
