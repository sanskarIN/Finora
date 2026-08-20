# Finora Documentation

[![Support Finora on Buy Me a Coffee](../src/Finora.App/Resources/Images/bmc_support.svg)](https://buymeacoffee.com/sanskarIN)

> **☕ Like Finora? Support ongoing development on [Buy Me a Coffee](https://buymeacoffee.com/sanskarIN).** Support is optional and remains separate from app features, entitlement, product support, and security reporting.

This directory is the documentation entry point for the current Finora 0.2.0 (build 2) source line and database schema 2.

Finora is a local-first personal-finance application with a mature .NET MAUI application path plus an additive Avalonia universal path for Linux desktop and WebAssembly/browser reach. Shared finance rules remain in platform-neutral C# projects; native persistence uses SQLite/EF Core, and the browser host keeps finance persistence disabled until a dedicated browser-local adapter passes parity, recovery, and privacy validation. The current release requires no Finora account or login, core native finance workflows are offline-capable, backups are user-triggered, and the application does not silently aggregate unlike currencies or invent exchange rates.

## Start here

- [Project README](../README.md) — product overview, current capabilities, privacy boundary, build entry points, contacts, and release status.
- [Cross-Platform Support Matrix](platforms/CROSS_PLATFORM.md) — Android, iOS/iPadOS, Windows, macOS, Linux, Web/WASM, PWA, and ChromeOS delivery paths plus parity boundaries.
- [Final Repository Closure](FINAL_REPOSITORY_CLOSURE.md) — final 2026-08-19 repository-engineering closure statement and the boundary between completed source work and external release evidence.
- [Final Hardening — 2026-08-19](FINAL_HARDENING_2026-08-19.md) — post-closure correctness, restore-safety, receipt-consistency, regression-coverage, backlog, evidence-boundary, and branch-governance audit.
- [Documentation Status](DOCUMENTATION_STATUS.md) — coverage/completeness matrix and documentation update policy.
- [Next Steps](NEXT_STEPS.md) — prioritized P0–P3 release-evidence and future-version roadmap; remaining external evidence is not treated as missing current-scope repository functionality.
- [User Guide](USER_GUIDE.md) — complete end-user workflow guide.
- [Architecture Overview](architecture/OVERVIEW.md) — solution layering and major design decisions.
- [Database Schema](architecture/DATABASE_SCHEMA.md) — current schema 2 model and migration notes.
- [Service Catalog](architecture/SERVICE_CATALOG.md) — application/infrastructure responsibilities and service ownership.
- [Data Flow](architecture/DATA_FLOW.md) — how finance data moves through UI, services, persistence, files, backups, imports, exports, and notifications.
- [Navigation and UI Architecture](architecture/NAVIGATION_AND_UI.md) — adaptive Shell navigation, pages, ViewModels, privacy/display behavior, and accessibility boundaries.

## Feature documentation

- [Accounts and Transactions](features/ACCOUNTS_AND_TRANSACTIONS.md)
- [Budgets, Savings, and Recurring Items](features/BUDGETS_GOALS_RECURRING.md)
- [Reports, Import, and Export](features/REPORTS_IMPORT_EXPORT.md)
- [Settings Reference](features/SETTINGS_REFERENCE.md)
- [Project Support / Buy Me a Coffee](features/PROJECT_SUPPORT.md)
- [Backup and Restore](security/BACKUP_AND_RECOVERY.md)
- [App Lock and Privacy](security/APP_LOCK_AND_PRIVACY.md)

## Accessibility and localization

- [Accessibility and Localization](accessibility/ACCESSIBILITY_AND_LOCALIZATION.md)
- [Navigation and UI Architecture](architecture/NAVIGATION_AND_UI.md)
- [Native Validation Matrix](testing/NATIVE_VALIDATION_MATRIX.md)

## Development

- [Build and Run](setup/BUILD.md)
- [Troubleshooting](setup/TROUBLESHOOTING.md)
- [Developer Guide](development/DEVELOPER_GUIDE.md)
- [Repository Code Map](development/CODE_MAP.md) — concise layer/area navigation guide.
- [Repository File Reference](development/REPOSITORY_FILE_REFERENCE.md) — exhaustive tracked-file responsibility/ownership reference, mechanically checked against `git ls-files`.
- [Cross-Platform File Reference](development/CROSS_PLATFORM_FILE_REFERENCE.md) — scoped inventory extension for the universal UI, native desktop host, WebAssembly host, and cross-platform solution.
- [Adding or Changing a Feature](development/ADDING_A_FEATURE.md)
- [Main Branch Protection Policy](development/BRANCH_PROTECTION.md) — intended GitHub ruleset/check policy and validation steps; current protection state is documented explicitly rather than assumed.
- [Engineering Decisions](../DECISIONS.md)

## Testing and quality

- [Test Plan](TEST_PLAN.md)
- [Testing Guide](testing/TESTING_GUIDE.md)
- [Repository QA](testing/REPOSITORY_QA.md) — dependency-free developer-tool, tracked-file documentation coverage, and localization checks.
- [Performance Benchmarking](testing/PERFORMANCE_BENCHMARKING.md) — reproducible synthetic 10k/50k/100k performance and correctness harness guidance.
- [CI Evidence](testing/CI_EVIDENCE.md) — dated, commit-specific GitHub Actions evidence and explicit source-build versus release-validation boundaries.
- [Native Validation Matrix](testing/NATIVE_VALIDATION_MATRIX.md)
- [Project Status](../PROJECT_STATUS.md)
- [What Changed](../what_changed.md)

## Operations and diagnostics

- [Diagnostics and Integrity](operations/DIAGNOSTICS_AND_INTEGRITY.md)
- [Data Reset and Sample Data](operations/DATA_RESET_AND_SAMPLE_DATA.md)
- [Data Lifecycle](privacy/DATA_LIFECYCLE.md)

## Security and privacy

- [Threat Model](security/THREAT_MODEL.md)
- [App Lock and Privacy](security/APP_LOCK_AND_PRIVACY.md)
- [Backup and Recovery](security/BACKUP_AND_RECOVERY.md)
- [Privacy Policy](../PRIVACY.md)
- [Terms](../TERMS.md)
- [Security Policy](../SECURITY.md)

## Platform documentation

- [Cross-Platform Support Matrix](platforms/CROSS_PLATFORM.md)
- [Android](platforms/ANDROID.md)
- [Windows](platforms/WINDOWS.md)
- [iOS and Mac Catalyst](platforms/APPLE.md)
- [Linux](platforms/LINUX.md)
- [Web / WebAssembly / PWA](platforms/WEB.md)
- [ChromeOS](platforms/CHROMEOS.md)

## Release documentation

- [Release Checklist](releases/RELEASE_CHECKLIST.md)
- [Store Readiness](releases/STORE_READINESS.md)
- [Versioning and Migration Policy](releases/VERSIONING_AND_MIGRATIONS.md)
- [Store Metadata Template](releases/STORE_METADATA_TEMPLATE.md)
- [Structural Release Readiness](testing/RELEASE_READINESS.md)
- [Changelog](../CHANGELOG.md)

## Community and legal

- [Contributing](../CONTRIBUTING.md)
- [Code of Conduct](../CODE_OF_CONDUCT.md)
- [Support](../SUPPORT.md)
- [Third-Party Notices](../THIRD_PARTY_NOTICES.md)
- [Apache-2.0 License](../LICENSE)

## Documentation completeness contract

The focused manuals above describe behavior. [Repository File Reference](development/REPOSITORY_FILE_REFERENCE.md) describes the established tracked-file ownership/change impact, while [Cross-Platform File Reference](development/CROSS_PLATFORM_FILE_REFERENCE.md) extends that inventory for the universal host family.

`python scripts/check_documentation_coverage.py` compares the combined approved reference set with the exact `git ls-files` set. It fails when a tracked file is uncovered, when a declared area no longer contains a tracked file, or when a reference attempts to hide detail behind a broad one-component catch-all such as `src/` or `docs/`. The same check is part of `scripts/run_repo_qa.py` and the primary CI structural preflight.

This coverage contract makes it difficult for a new source, test, workflow, script, asset, platform resource, or policy file to exist without an explicit documented responsibility. It does **not** turn documentation into runtime evidence; target-platform/native/store/browser validation remains separate.

## Product identity

- Product: **Finora**
- Attribution: **Made by the Sanskar**
- Repository: https://github.com/sanskarIN/Finora
- Creator profile: https://www.github.com/sanskarIN
- **☕ Support development: https://buymeacoffee.com/sanskarIN**
- Business/security contact: `sanskarin@outlook.in`
- Support: `supportramsandesh@gmail.com`
- License: Apache-2.0

The Buy Me a Coffee link is an optional external support/contribution link. It does not unlock Finora features, create premium entitlement, change support priority, or replace store/server-backed commercial licensing. Before a packaged store release, verify whether the target store permits that external support link in the app for the intended region and distribution model.

## Current release boundary

The current local-first source line intentionally does not claim remote Finora accounts, cloud synchronization, collaboration, server-backed commercial entitlement, automatic exchange-rate conversion, or default analytics/advertising telemetry. Those are later-version product decisions and require new architecture, privacy, security, migration, and release review before implementation.

The new universal host source also does not claim browser finance-persistence parity or completed Linux UI parity. Those status boundaries are explicitly documented in the platform matrix so build-target reach is not confused with release readiness.

## Validation statement

Documentation describes implemented source and required validation. Current commit-specific automated evidence is recorded in [CI Evidence](testing/CI_EVIDENCE.md). A successful source build is not automatically a verified store-ready package. Android, Windows, iOS/iPadOS, macOS/Mac Catalyst, Linux, WebAssembly/PWA, and ChromeOS delivery paths require the corresponding platform/toolchain evidence for packaging, signing where applicable, device/runtime behavior, storage durability, privacy, accessibility, file sharing, notifications/biometrics where applicable, interrupted-recovery behavior, and store/browser policy before release claims are made.
