# Finora

Finora is an open-source, privacy-first, local-first personal finance application built with .NET MAUI, C#, XAML, SQLite/EF Core, and MVVM-style presentation logic.

> Made by the Sanskar

**Current source line:** 0.2.0 (build 2) · database schema 2.

The current release design requires no Finora login/account and keeps finance data in app-private local storage. There is no automatic cloud sync, automatic backup upload, analytics SDK, or advertising SDK in the current source.

## What Finora includes

### Finance core

- Cash, bank, credit-card, digital-wallet, savings, investment-placeholder and custom accounts.
- Expense, income, refund, adjustment and paired same-currency transfer transactions.
- Integer `long` minor-unit money storage with `decimal` major-unit arithmetic.
- Currency-aware zero-/two-/three-decimal conversion and formatting metadata.
- Search/filter, soft delete/restore, revision history, splits, tags, receipts, bulk categorization and duplicate review.
- Categories/subcategories with merge/reassign/reorder/archive/restore.
- Account reconciliation with explicit adjustment history.
- Weekly/monthly/custom budgets, rollover and warning thresholds.
- Savings goals, linked contributions/withdrawals, milestones and forecasts.
- Restart-safe recurring obligations with pending/paid/partial/skipped/reopened/postponed states.

### Dashboard and reports

- Configurable privacy-aware dashboard.
- Category, income/expense, merchant/payee, monthly comparison, account trend and budget performance reports.
- Accessible MAUI-drawn charts plus equivalent text/table data.
- Explicit reporting-currency behavior: Finora does not silently convert or add unlike currencies. Dashboard/report aggregates use one selected/default reporting currency; other currencies remain separate and labeled.

### Import/export

- User-mapped CSV preview/import with quoted-field parsing, validation, duplicate protection and transactional commit.
- Currency-aware major-unit import, including zero- and three-decimal currencies.
- Transfer/counterparty/tag/category/account validation.
- CSV export and multipage PDF export through explicit system share/save actions.

### Receipt storage

- Image/PDF receipts/documents copied into app-private storage.
- Safe-path confinement, file/type/size metadata and SHA-256 checksums.
- Open/delete/storage-usage/orphan-cleanup workflows.

### Encrypted backup and crash-safe restore

- AES-GCM authenticated encryption.
- PBKDF2-SHA256 password-derived keys with random salt.
- Backup preview, current-schema validation and attachment-byte inclusion.
- Serialized backup/preview/restore operations.
- Production crash-safe restore wrapper with private recovery journal, pre-restore receipt rollback copy, transient DB marker and startup recovery before normal navigation.
- If the DB restore did not commit, receipts roll back; if the DB committed, the new receipt tree is finalized. Unsafe unresolved recovery blocks normal initialization rather than exposing mismatched state.

### Privacy and security

- No required online account for current-release functionality.
- No background location collection; transaction location is manually entered only.
- Optional local PIN app lock with PBKDF2 verifier, secure storage, bounded lockout and inactivity locking.
- Missing/corrupt secure-storage verifier fails closed while app lock is marked enabled.
- Optional biometric/Windows Hello source with PIN fallback.
- Android and supported-Windows sensitive-screen protection source where platform capabilities allow it.
- Generic local notification text designed to avoid lock-screen finance details.
- Bounded privacy-safe diagnostics and an on-device integrity report that exposes status codes/counts rather than private finance contents.

### Adaptive UI and accessibility

- Phone bottom primary tabs.
- Tablet/desktop flyout/sidebar-equivalent primary navigation.
- Route preservation when switching responsive navigation modes.
- Adaptive startup/onboarding/unlock dashboard routing.
- Light/dark/system appearance, larger-interface sizing, reduced-motion preference and minimum touch/input target sizing.
- Semantic headings/live error states and chart text equivalents.
- Native screen-reader/keyboard/resize/Dynamic-Type validation remains part of release QA.

### Localization and formatting

- English-first UI with localization-ready resources including an initial Hindi resource structure.
- Saved locale is validated and applied at runtime before normal navigation.
- Locale-aware number/date format preview in Settings.
- Financial storage does not depend on display culture.

### Data controls and developer tools

- Full confirmed local finance-data reset that preserves schema metadata, app preferences and PIN configuration while clearing finance-domain records and receipt files safely.
- Hidden developer panel with schema/feature state, reminder sync, privacy-safe integrity check and deterministic synthetic sample reset.
- Synthetic sample reset requires typed destructive confirmation and never intentionally keeps the prior finance dataset.
- Local premium flag is a development/demo capability only and is not secure commercial entitlement validation.

## Architecture

Dependency direction:

```text
Finora.App -> Finora.Infrastructure / Finora.Application -> Finora.Domain -> Finora.Shared
```

Key source areas:

```text
src/
  Finora.Shared/
  Finora.Domain/
  Finora.Application/
  Finora.Infrastructure/
  Finora.App/
tests/
  Finora.UnitTests/
  Finora.IntegrationTests/
  Finora.UiTests/
docs/
build/scripts/
.github/
```

See `docs/architecture/OVERVIEW.md`, `docs/architecture/DATABASE_SCHEMA.md`, `DECISIONS.md`, and `docs/security/THREAT_MODEL.md`.

## Build and test

Start with dependency-free structural validation:

```bash
python build/scripts/verify_structure.py
```

Core verification:

```bash
./build/scripts/verify.sh
```

Windows PowerShell:

```powershell
./build/scripts/verify.ps1
```

The scripts run structural checks and core tests, then run native MAUI builds only where the current host supports the relevant target. CI separately builds Windows+Android on Windows and iOS+Mac Catalyst on macOS.

For full details see `docs/setup/BUILD.md` and `docs/TEST_PLAN.md`.

## Important release note

The repository contains complete source implementations and automated test/CI definitions, but **source presence is not proof of native store readiness**. A release still requires successful .NET/MAUI compilation, platform SDK/workload compatibility, device/emulator/simulator tests, signing/package validation, accessibility/resize tests, notification/biometric/capture behavior validation, migration testing, interrupted-restore failure injection and store privacy/licensing review.

See:

- `PROJECT_STATUS.md`
- `docs/releases/RELEASE_CHECKLIST.md`
- `docs/releases/STORE_READINESS.md`

## Privacy

Finora is local-first. Finance records and receipt files remain in app-private storage unless the user explicitly exports/shares them or creates/saves an encrypted backup. Uninstalling the app can remove local data, so save a separate encrypted backup first when records must be preserved.

Read `PRIVACY.md` and `docs/privacy/DATA_LIFECYCLE.md`.

## Security reports

Do not post a vulnerability with real finance data publicly. Follow `SECURITY.md` and use synthetic reproduction data.

## Contributing

Read `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `DECISIONS.md`, and the pull-request template. Preserve integer-money, local-first, privacy, migration, recovery and currency-isolation invariants when changing finance behavior.

## Links

- Repository: https://github.com/sanskarIN/Finora
- Creator/open-source profile: https://www.github.com/sanskarIN
- Business/security: `sanskarin@outlook.in`
- Support: `supportramsandesh@gmail.com`

Finora is licensed under Apache-2.0. Third-party dependencies retain their own licenses; see `THIRD_PARTY_NOTICES.md`.
