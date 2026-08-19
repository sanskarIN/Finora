# Adding or Changing a Finora Feature

Use this checklist when implementing any non-trivial feature in the current Finora local-first architecture.

## 1. Define the product boundary first

Before code, decide:

- Is the feature local-first/offline-capable?
- Does it require login/cloud/server/network?
- Does it introduce new personal/financial data?
- Does it introduce a new permission?
- Does it change money/currency semantics?
- Does it change backup compatibility?
- Does it change schema/migration?
- Does it change a store/privacy declaration?

If it requires current non-goals such as cloud sync, remote identity, collaboration, automatic FX, or server-backed entitlement, treat it as a new architecture/product decision rather than silently adding network dependencies.

## 2. Put logic in the correct layer

Use:

- Shared for platform-neutral primitives/policies with no finance persistence ownership;
- Domain for entities, money rules, finance invariants, pure policies;
- Application for contracts/DTOs;
- Infrastructure for EF Core, files, crypto, import/export/reporting and platform-neutral workflow implementations;
- App for MAUI UI, Preferences/SecureStorage and native platform adapters.

Use `docs/development/CODE_MAP.md` for fast navigation and `docs/development/REPOSITORY_FILE_REFERENCE.md` for the exhaustive tracked-file ownership/change-impact map.

## 3. Money checklist

If the feature touches money:

- use `long` minor units in persisted/domain calculations;
- use `decimal` at major-unit text boundaries;
- use `Money`/`CurrencyMinorUnits`;
- use checked arithmetic;
- reject zero/extreme values where rules require it;
- define sign semantics;
- define currency relationship;
- never silently aggregate unlike currencies;
- add zero-/three-/other precision tests when relevant.

## 4. Date/time checklist

If the user selects calendar dates:

- model local calendar meaning explicitly;
- use `LocalDateRange` for UTC query boundaries;
- use end-exclusive range;
- test non-UTC offset;
- test month/year grouping in local time;
- avoid `DateTimeOffset.UtcNow.Date` as a substitute for local today;
- avoid hard-coded `23:59:59` end of day.

## 5. Database/schema checklist

If adding/changing persisted state:

- update Domain entity;
- update `FinoraDbContext` mapping/indexes/relationships;
- update persistence-boundary validation;
- decide required constraints/indexes;
- add migration from current released schema;
- advance schema version only with complete migration path;
- update database documentation;
- update integration/migration tests;
- update integrity diagnostics;
- update encrypted backup snapshot/validation/restore;
- update finance reset/sample behavior if applicable;
- update privacy data lifecycle;
- update release migration checklist.

A persisted feature is incomplete if backup/reset/integrity/migration are ignored.

## 6. Service checklist

If the feature is a workflow:

- add/extend Application contract;
- implement in Infrastructure or App platform adapter as appropriate;
- register in `MauiProgram`;
- keep multi-record changes transactional;
- pass cancellation tokens through public async contracts where available;
- return stable validation/result types;
- do not leak raw infrastructure exception details to UI.

## 7. Transfer/reconciliation/recurrence special rules

### Transfers

Use dedicated paired same-currency workflow. Never write one half independently.

### Reconciliation

Preserve explicit difference and adjustment audit/history. Do not rewrite history invisibly.

### Recurrence

Preserve occurrence-first/idempotent behavior. Scheduler preparation is not payment.

## 8. File/attachment checklist

If the feature writes files:

- use app-private/cache directory appropriate to lifecycle;
- confine paths canonically;
- reject link/reparse traversal where current path helpers require it;
- use generated internal names for sensitive app-owned files;
- validate size/type/hash as appropriate;
- define cleanup/retention;
- define backup inclusion or explicit exclusion;
- define restore behavior;
- define external share/save trust boundary.

## 9. Security/privacy checklist

Ask:

- Could this appear outside app lock?
- Could it expose money through text or charts?
- Could it reach diagnostics?
- Is a password/PIN/secret entered?
- Does it require secure storage?
- Does it introduce automatic upload?
- Does it change Android backup/device-transfer behavior?

Rules:

- passive money must honor hide/privacy mode;
- charts must not leak magnitude when amounts are hidden;
- secrets use masked controls;
- privacy logger never receives raw finance/secret contents;
- notification content stays generic;
- no new automatic cloud upload without architecture/privacy approval.

## 10. UI/navigation checklist

- place primary vs secondary route intentionally;
- preserve adaptive phone/desktop navigation;
- avoid hard-coded mobile root for startup/unlock/onboarding completion;
- add loading/empty/error/permission-denied states;
- add semantic labels/headings where needed;
- support larger text/interface;
- respect reduced motion;
- keep chart text/table equivalent;
- add UI-contract tests for critical bindings/handlers.

## 11. Platform-feature checklist

For notifications, biometrics, capture protection, file pickers, package APIs, etc.:

- isolate target code in App/platform adapter;
- add required manifest/plist/capability metadata;
- keep platform errors normalized;
- document unsupported behavior;
- add structural/source contracts where possible;
- run native target build;
- run emulator/simulator/device test;
- update platform/store readiness matrix.

## 12. Test checklist

Add tests at all applicable layers:

### Unit

- pure rules;
- edge/boundary arithmetic;
- date policy;
- ViewModel-independent helper.

### Integration

- SQLite persistence;
- transactions/rollback;
- migration;
- backup/restore;
- import/export;
- integrity;
- failure injection that can be simulated.

### UI contract

- XAML binding/handler/source invariants;
- privacy masking;
- route presence;
- critical control configuration.

### Native validation

- target compilation;
- permissions;
- device behavior;
- accessibility;
- packaging/signing.

## 13. Structural and repository-QA checklist

If an invariant can be checked dependency-free and is high value, extend `verify_structure.py`.

Keep structural checks deterministic and repository-focused. Never label them compiler/native tests.

Before finalizing a normal change, run:

```bash
python build/scripts/verify_structure.py
python scripts/run_repo_qa.py
```

`run_repo_qa.py` executes Python developer-tool tests, `scripts/check_documentation_coverage.py`, and localization validation. The documentation-coverage step reads `git ls-files` and requires every tracked file to be represented by an exact entry or a meaningful narrow directory entry in `docs/development/REPOSITORY_FILE_REFERENCE.md`.

Do not make the coverage check pass by adding a broad top-level prefix such as `src/`, `docs/`, `tests/`, `scripts/`, or `.github/`; those declarations are intentionally rejected.

## 14. Documentation checklist

Update all affected docs, not only README:

- `docs/USER_GUIDE.md`;
- feature guide;
- architecture/service/data flow/code map;
- schema if persisted;
- threat model;
- data lifecycle;
- test plan/testing guide;
- platform docs;
- release checklist/store readiness;
- changelog/project status;
- `what_changed.md`.

For every added, moved, or deleted tracked file, also verify the file reference's ownership/change-impact description remains accurate. A narrow directory entry may already cover the new path, but the text must still truthfully describe the area's responsibility.

## 15. Commit checklist

Prefer focused commits in logical order:

1. contract/domain policy;
2. implementation;
3. DI/presentation;
4. tests;
5. structural/repository QA guard;
6. docs;
7. final status ledger.

Use clear messages such as `feat(area): ...`, `fix(area): ...`, `test(area): ...`, `docs(area): ...`.

## 16. Final release question

Before declaring completion, answer separately:

- Is the feature implemented in source?
- Are automated tests present?
- Did the relevant tests actually run?
- Did repository documentation coverage pass for the exact candidate?
- Did native build run?
- Did device/platform behavior run?
- Is signing/store evidence available?

Do not collapse those into one unsupported “done” claim.
