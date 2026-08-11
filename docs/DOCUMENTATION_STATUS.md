# Finora Documentation Status

This matrix tracks documentation coverage for the current Finora 0.2.0 (build 2), database schema 2 source line.

## Coverage status

| Area | Primary document | Status |
|---|---|---|
| Public project overview | `README.md` | Current |
| Documentation index | `docs/README.md` | Current |
| End-user workflows | `docs/USER_GUIDE.md` | Current |
| Architecture overview | `docs/architecture/OVERVIEW.md` | Current |
| Database/schema | `docs/architecture/DATABASE_SCHEMA.md` | Current |
| Service ownership | `docs/architecture/SERVICE_CATALOG.md` | Current |
| End-to-end data flow | `docs/architecture/DATA_FLOW.md` | Current |
| Navigation/UI | `docs/architecture/NAVIGATION_AND_UI.md` | Current |
| Accounts/transactions | `docs/features/ACCOUNTS_AND_TRANSACTIONS.md` | Current |
| Budgets/goals/recurrence | `docs/features/BUDGETS_GOALS_RECURRING.md` | Current |
| Reports/import/export | `docs/features/REPORTS_IMPORT_EXPORT.md` | Current |
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
| Native validation matrix | `docs/testing/NATIVE_VALIDATION_MATRIX.md` | Current |
| Android | `docs/platforms/ANDROID.md` | Current |
| Windows | `docs/platforms/WINDOWS.md` | Current |
| iOS/Mac Catalyst | `docs/platforms/APPLE.md` | Current |
| Release checklist | `docs/releases/RELEASE_CHECKLIST.md` | Current |
| Store readiness | `docs/releases/STORE_READINESS.md` | Current |
| Versioning/migrations | `docs/releases/VERSIONING_AND_MIGRATIONS.md` | Current |
| Store metadata template | `docs/releases/STORE_METADATA_TEMPLATE.md` | Current |
| Engineering decisions | `DECISIONS.md` | Current |
| Project implementation status | `PROJECT_STATUS.md` | Current |
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

## Documentation build/preflight rule

`build/scripts/verify_structure.py` treats the core documentation tree as required repository structure and validates repository-relative Markdown file links without network access.

The link check deliberately does not prove:

- external URLs are reachable;
- section anchors exist;
- current store-policy URLs/requirements;
- native behavior described by platform docs.

## Documentation and native validation

A platform document describes intended/current source behavior and required QA. It does not convert unexecuted native validation into a passing result.

The release decision must still retain real evidence for:

- .NET restore/build/tests;
- MAUI target builds;
- Android device/emulator behavior;
- Windows packaged behavior;
- iOS/Mac Catalyst builds/devices;
- accessibility;
- signing;
- store declarations.

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

### New later-version network feature

Before implementation, update architecture/privacy/security/data-retention/migration/product-boundary decisions. Do not silently add it under existing local-first documentation.

## Contacts

- Repository: https://github.com/sanskarIN/Finora
- Business/security: `sanskarin@outlook.in`
- Support: `supportramsandesh@gmail.com`
- Attribution: Made by the Sanskar