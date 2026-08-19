# Finora Documentation Status

This matrix tracks documentation coverage for the current Finora 0.2.0 (build 2), database schema 2 source line.

## Coverage status

| Area | Primary document | Status |
|---|---|---|
| Public project overview | `README.md` | Current |
| Documentation index | `docs/README.md` | Current |
| Documentation completeness | `docs/DOCUMENTATION_STATUS.md` | Current |
| Final repository closure | `docs/FINAL_REPOSITORY_CLOSURE.md` | Current — repository engineering closure and external release-evidence boundary recorded 2026-08-19 |
| Prioritized next steps | `docs/NEXT_STEPS.md` | Current — repository backlog closed; remaining release/native/full-profile items are external evidence, optional polish, or later-version scope |
| End-user workflows | `docs/USER_GUIDE.md` | Current |
| Architecture overview | `docs/architecture/OVERVIEW.md` | Current |
| Database/schema | `docs/architecture/DATABASE_SCHEMA.md` | Current |
| Service ownership | `docs/architecture/SERVICE_CATALOG.md` | Current |
| End-to-end data flow | `docs/architecture/DATA_FLOW.md` | Current |
| Navigation/UI | `docs/architecture/NAVIGATION_AND_UI.md` | Current |
| Accounts/transactions | `docs/features/ACCOUNTS_AND_TRANSACTIONS.md` | Current |
| Budgets/goals/recurrence | `docs/features/BUDGETS_GOALS_RECURRING.md` | Current |
| Reports/import/export | `docs/features/REPORTS_IMPORT_EXPORT.md` | Current |
| Settings reference | `docs/features/SETTINGS_REFERENCE.md` | Current |
| Accessibility/localization | `docs/accessibility/ACCESSIBILITY_AND_LOCALIZATION.md` | Current |
| App lock/privacy | `docs/security/APP_LOCK_AND_PRIVACY.md` | Current |
| Encrypted backup/recovery | `docs/security/BACKUP_AND_RECOVERY.md` | Current |
| Threat model | `docs/security/THREAT_MODEL.md` | Current |
| Data lifecycle | `docs/privacy/DATA_LIFECYCLE.md` | Current |
| Diagnostics/integrity | `docs/operations/DIAGNOSTICS_AND_INTEGRITY.md` | Current |
| Data reset/sample | `docs/operations/DATA_RESET_AND_SAMPLE_DATA.md` | Current |
| Build/run | `docs/setup/BUILD.md` | Current |
| Troubleshooting | `docs/setup/TROUBLESHOOTING.md` | Current |
| Developer guide | `docs/development/DEVELOPER_GUIDE.md` | Current |
| Code map | `docs/development/CODE_MAP.md` | Current |
| Feature-change procedure | `docs/development/ADDING_A_FEATURE.md` | Current |
| Test plan | `docs/TEST_PLAN.md` | Current |
| Practical testing guide | `docs/testing/TESTING_GUIDE.md` | Current |
| Performance benchmarking | `docs/testing/PERFORMANCE_BENCHMARKING.md` | Current — synthetic harness + exact verified bounded 10k smoke + full-profile evidence boundary documented |
| Dated CI/check-run evidence | `docs/testing/CI_EVIDENCE.md` | Current — strict 2026-08-18 performance-tooling candidate recorded with 319 tests, four native source builds, CodeQL, Dependency Review and bounded 10k smoke |
| Native validation matrix | `docs/testing/NATIVE_VALIDATION_MATRIX.md` | Current |
| Android | `docs/platforms/ANDROID.md` | Current |
| Windows | `docs/platforms/WINDOWS.md` | Current |
| iOS/Mac Catalyst | `docs/platforms/APPLE.md` | Current |
| Release checklist | `docs/releases/RELEASE_CHECKLIST.md` | Current |
| Store readiness | `docs/releases/STORE_READINESS.md` | Current |
| Versioning/migrations | `docs/releases/VERSIONING_AND_MIGRATIONS.md` | Current |
| Store metadata template | `docs/releases/STORE_METADATA_TEMPLATE.md` | Current |
| Engineering decisions | `DECISIONS.md` | Current |
| Project implementation status | `PROJECT_STATUS.md` | Current — verified performance tooling/smoke, final repository closure, and remaining runtime/native gates recorded |
| Detailed change ledger | `what_changed.md` | Current after final continuation ledger commit |
| Changelog | `CHANGELOG.md` | Current source history |
| Privacy policy | `PRIVACY.md` | Current |
| Terms | `TERMS.md` | Current |
| Security reporting | `SECURITY.md` | Current |
| Support | `SUPPORT.md` | Current |
| Contributing | `CONTRIBUTING.md` | Current |
| Code of conduct | `CODE_OF_CONDUCT.md` | Current |
| Third-party notices | `THIRD_PARTY_NOTICES.md` | Current subject to release-time exact dependency audit |
| License | `LICENSE` | Apache-2.0 |

## Documentation source rules

Documentation is derived from the current repository source/contracts and the project's approved product boundary.

Do not document later-version features as if implemented. Current explicit later-version boundaries include:

- remote Finora account/login;
- cloud synchronization;
- collaboration/shared-finance server features;
- server/store-backed commercial entitlement;
- automatic exchange-rate conversion;
- default analytics/advertising telemetry.

## Project support-link rule

The canonical optional external support link is:

https://buymeacoffee.com/sanskarIN

Documentation and UI must not describe that link as premium entitlement, subscription, feature unlock, required support payment, or secure licensing. Store-facing docs must keep a release gate to review the target store's current external contribution/payment-link policy before packaging/submission.

## Documentation build/preflight rule

`build/scripts/verify_structure.py` treats the core documentation tree as required repository structure and validates repository-relative Markdown file links without network access.

The documentation index itself links the remaining cross-cutting manuals, including `docs/testing/CI_EVIDENCE.md` and `docs/testing/PERFORMANCE_BENCHMARKING.md`, so the link validator also protects indexed documents that are not separately enumerated in the hard required-path list.

The link check deliberately does not prove:

- external URLs are reachable;
- section anchors exist;
- current store-policy URLs/requirements;
- native behavior described by platform docs.

## Documentation and native validation

A platform document describes intended/current source behavior and required QA. It does not convert unexecuted native validation into a passing result.

As of 2026-08-18, `docs/testing/CI_EVIDENCE.md` records actual structural, **319-test**, four-target MAUI Release source-build, CodeQL, Dependency Review, performance-project build, and bounded 10k startup/history/reports/integrity smoke evidence for exact source candidate `8a8e7e51a2bacecdc58405d3d5301e79f3d78c8b`.

The retained smoke artifact is `9321290557` with SHA-256 `97eb07bf963491e8d89d45798b21aa99d0da312b931c3ea25b17e2dae5accb46`.

Performance timing remains a separate evidence class. The benchmark harness records synthetic observational measurements and correctness/integrity failures; arbitrary elapsed-time thresholds are not treated as finance correctness. The current evidence does not claim runtime completion of CSV/PDF/backup performance operations, the full 10k `all` profile, or 50k/100k comparisons.

The 2026-08-19 repository closure records repository engineering completion separately from external release evidence. Queued or pending GitHub Actions runs are not represented as successful executed evidence; exact runtime claims remain tied to the commit/run where they actually completed.

The remaining release decision must still retain real evidence for:

- signed/package artifacts;
- Android device/emulator behavior;
- Windows packaged behavior;
- iOS/Mac Catalyst provisioning/signing/device behavior;
- installed prior-version migration and process-interrupted backup/restore recovery;
- accessibility;
- store declarations and policy review;
- full comparable performance-profile evidence when making performance claims.

## Next-step priority rule

`docs/NEXT_STEPS.md` remains the release-evidence/future-scope roadmap after repository closure. The preferred order is:

1. preserve the proven structural/test/bounded-10k/four-target source-build/CodeQL/Dependency Review gates;
2. complete only external P0/P1 release evidence required for the actual target package/store;
3. execute optional P2 heavy performance/native evidence when it is needed for a release claim;
4. begin P3 later-version cloud/account/collaboration/entitlement/FX/telemetry work only through an explicit new-version decision.

Do not add speculative current-version features merely to keep the repository changing.

## Update policy

When code changes, update all relevant documentation in the same workstream.

Examples:

### New persisted entity/field

Update:

- schema;
- service/data flow;
- user/feature guide;
- backup/recovery;
- integrity/reset/sample behavior;
- privacy lifecycle/threat model;
- tests;
- migration/versioning;
- release checklist/status/ledger.

### New passive monetary UI

Update/test:

- privacy behavior;
- UI architecture;
- user guide;
- UI-contract tests;
- release/native validation matrix.

### New native permission/API

Update:

- platform guide;
- threat model/privacy declarations;
- App Store/Play/Windows metadata template as applicable;
- native validation matrix;
- store readiness.

### New accessibility/localization behavior

Update:

- accessibility/localization guide;
- user guide;
- affected feature/UI architecture documentation;
- native validation matrix;
- localization resources and tests/source contracts as applicable.

### New external support/payment link

Update:

- shared product identity constant;
- About/UI source contract;
- README/docs index/support docs;
- Settings reference;
- store metadata/release gates;
- current target-store policy review.

### New performance benchmark or evidence

Update:

- `docs/testing/PERFORMANCE_BENCHMARKING.md` with dataset shape, operations, runner interpretation, and correctness policy;
- benchmark workflow/CI smoke coverage as applicable;
- roadmap, project status, changelog, and detailed ledger only for evidence actually obtained.

### New CI/release evidence

Update:

- `docs/testing/CI_EVIDENCE.md` with exact commit/run/job IDs and test counts;
- project status and roadmap;
- changelog and detailed ledger;
- release/store checklists only for gates actually executed.

### New later-version network feature

Before implementation, update architecture/privacy/security/data-retention/migration/product-boundary decisions. Do not silently add it under existing local-first documentation.

## Contacts

- Repository: https://github.com/sanskarIN/Finora
- Creator: https://www.github.com/sanskarIN
- Optional project support: https://buymeacoffee.com/sanskarIN
- Business/security: `sanskarin@outlook.in`
- Support: `supportramsandesh@gmail.com`
- Attribution: Made by the Sanskar