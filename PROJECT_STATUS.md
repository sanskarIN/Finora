# Project Status

## Implemented in repository

- Multi-project architecture, MAUI scaffolding and all required top-level repository policy/documentation files.
- Core normalized finance data model.
- Signed `long` minor-unit money storage and decimal-safe major/minor conversion.
- Accounts, transaction persistence/search, paired transfers, soft delete/restore.
- Default categories, budget persistence/reporting, savings goals/contributions, recurrence rules and idempotent occurrence processing.
- Dashboard summaries, accessible textual reports, CSV export/import preview and PDF export.
- AES-GCM encrypted backup creation/preview/transactional restore.
- Privacy-aware settings, PIN hashing/rate limiting, inactivity lock and diagnostic redaction.
- Onboarding, dashboard, transactions, accounts, budgets, goals, reports, recurring and settings UI.
- Local premium demo state with explicit non-secure entitlement warning.
- Hidden developer panel for schema/feature-flag visibility.
- Android, iOS, Mac Catalyst and Windows platform bootstrap files.
- English resource file and initial Hindi localization resource structure.
- Original SVG branding sources.
- Unit, SQLite integration and UI-contract test projects.
- Cross-platform GitHub Actions build/test workflow.

## Verification state

Local structural validation passed for XML/XAML parsing, project-reference resolution, XAML code-behind wiring, empty files and placeholder-exception/TODO scans.

The ChatGPT execution container has no `dotnet` SDK, so restore/build/test could not be executed here. GitHub Actions is the first compiler/platform gate after this source is pushed. A store release must additionally be smoke-tested on real/simulated target devices.

## Deferred before store release

These are intentionally listed rather than falsely represented as complete:

- Receipt capture/attachment management UI and local image lifecycle controls.
- CSV mapping/commit import workflow beyond validation preview.
- Native local-notification permission/scheduling integration.
- Biometric unlock and platform screenshot-blocking implementation.
- Full account reconciliation UI.
- Bulk transaction categorization, split editor, duplicate-resolution UI and complete edit-history viewer.
- Category merge/reorder/archive/restore UI and tag management/report filtering UI.
- Rich graphical chart package integration; textual reports are present now.
- Recurring item paid/skipped/postponed/partially-paid management UI beyond generation processing.
- Store packaging/signing assets and actual device UI automation.
- Database migration chain beyond schema version 1; v1 is the initial schema.
- Cloud synchronization/account systems remain later-version work by product design.
