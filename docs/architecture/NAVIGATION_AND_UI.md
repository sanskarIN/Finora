# Navigation and UI Architecture

This document describes the current Finora 0.2.0 MAUI navigation and presentation structure.

## Primary Shell navigation

`AppShell.xaml` defines two equivalent primary hierarchies.

### Mobile tab hierarchy

The mobile `TabBar` contains:

- Dashboard — route `dashboard`;
- Transactions — route `transactions`;
- Budgets — route `budgets`;
- Goals — route `goals`;
- Settings — route `settings`.

### Desktop/tablet flyout hierarchy

The alternate flyout hierarchy contains:

- Dashboard — route `dashboard-desktop`;
- Transactions — route `transactions-desktop`;
- Budgets — route `budgets-desktop`;
- Goals — route `goals-desktop`;
- Settings — route `settings-desktop`.

Only the active navigation hierarchy is intended to be visible at one time.

## Adaptive route selection

`AppRoutes` chooses the root hierarchy.

Desktop navigation is selected when:

- `DeviceInfo.Idiom` is Desktop or Tablet; or
- current Shell/window width is at least 900.

Otherwise Finora uses the mobile root.

Current root constants:

- `//dashboard` for mobile;
- `//dashboard-desktop` for desktop/tablet.

Startup, onboarding completion, and unlock completion route through `AppRoutes.DashboardRoot` instead of hard-coding one hierarchy.

When the window crosses the adaptive boundary, the Shell implementation attempts to preserve the equivalent primary section rather than always throwing the user back to Dashboard.

## Hidden root routes

Shell also contains non-primary root routes:

- `onboarding`;
- `lock`.

These are hidden from normal flyout/tab navigation and are used by app lifecycle/first-run/security state.

## Secondary workflow routes

Additional detail/tool/legal/import/reporting routes are registered by the Shell code-behind/current navigation setup rather than being primary tabs.

Examples represented in the current UI/source include:

- account detail;
- reconciliation;
- transaction detail;
- transaction tools;
- CSV import;
- categories/tags;
- recurring;
- reports;
- privacy/terms/legal/about-related flows.

Contributors should preserve the distinction between a primary section and a secondary task route. Adding every feature as a new bottom tab would break the current five-section mobile information architecture.

## MVVM/presentation boundary

Pages bind to ViewModels for finance state/workflows. Small platform/navigation/file-picker/share interactions may remain in code-behind when they require MAUI APIs.

Long-running work such as database access, import/export, backup/restore, attachment copy, and cryptography should remain asynchronous.

## `ViewModelBase`

Shared ViewModel behavior provides property notification, busy state, error state, and async-command coordination.

Unexpected `AsyncCommand` failures are routed to the privacy-safe unexpected-failure hook configured in `MauiProgram` rather than intentionally escaping ordinary UI command execution.

## Dashboard UI contract

Dashboard includes an activity-period selector with:

- Current financial month;
- Previous financial month;
- Last 30 days;
- Last 90 days;
- Year to date.

The displayed current account balance remains current state, while activity cards follow the chosen date period.

The Dashboard also displays reporting-currency scope so other-currency rows are not mistaken for converted aggregates.

## Transaction-list UI contract

Transaction history provides:

- search;
- advanced filters;
- sort picker;
- 50-row display pages;
- Load more when additional loaded matches exist;
- tap/select navigation into transaction detail.

The selected date range is a local calendar range and uses shared UTC conversion logic.

## Report UI contract

Reports expose:

- date range;
- reporting-currency notice;
- summary;
- category spending;
- income vs expense;
- monthly comparison;
- yearly comparison;
- merchant/payee rows;
- budget performance;
- recurring obligations;
- savings progress;
- account balance trends;
- CSV/PDF export.

Charts are supplemental. Equivalent text/table values remain present.

## Chart semantics

`ReportBarChartView` uses a true zero baseline.

- positive values extend above zero;
- negative values extend below zero;
- all-positive/all-negative data keeps zero at the appropriate plot edge;
- chart labels are bounded for readability;
- chart data is not shown when privacy mode suppresses amounts.

Do not regress to absolute-value bars that turn a negative net value into a visually positive bar.

## Privacy amount display

Passive monetary display must honor `PrivacyMode` or `HideAmountsOnLaunch`.

The shared `PrivacyMoneyConverter` formats actual currency-aware major-unit values when visible and returns `••••` when hidden.

Current passive surfaces protected by this rule include:

- account lists;
- transaction history;
- Transaction Tools;
- account detail history;
- budgets;
- savings cards;
- recurring rule/occurrence cards;
- reconciliation history;
- transaction-detail split rows;
- reports.

Some ViewModels also create masked summaries/forecast text where a converter alone cannot prevent disclosure.

Editable entries are intentionally different from passive display because the user is explicitly entering/editing a value.

## Theme and accessibility settings

Current settings/presentation include:

- system/light/dark theme;
- larger interface preference;
- reduced-motion preference;
- scalable control sizing/minimum targets;
- semantic headings/descriptions on important screens;
- text/tabular report equivalents;
- screen-reader-described lock/PIN/biometric controls.

These source features do not replace native accessibility validation.

## Accessibility release validation

Before release, verify on target platforms:

- TalkBack on Android;
- Narrator/keyboard focus/high contrast on Windows;
- VoiceOver/Dynamic Type on iOS;
- VoiceOver/keyboard focus on Mac Catalyst;
- large text/larger interface;
- focus order;
- empty/loading/error states;
- resize across adaptive navigation threshold;
- reduced-motion behavior;
- semantic meaning of security controls;
- chart text/table equivalence.

## Onboarding UI

Onboarding communicates:

- local-first behavior;
- no Finora login requirement;
- default currency;
- locale;
- financial month start;
- optional opening balance;
- optional sample-data opt-in;
- uninstall/backup warning;
- manual-location/no-background-location boundary;
- Privacy link;
- Terms link;
- Settings revisit availability.

Revisiting onboarding should not duplicate opening/sample finance data when accounts already exist.

## Lock UI

The lock screen is shown only when app-lock state requires it.

Security UX rules:

- PIN entry remains masked;
- lockout state remains visible/actionable;
- biometric action is optional and falls back to PIN;
- provider-specific biometric error details are not surfaced raw;
- successful unlock returns to the adaptive Dashboard root.

## Settings UI

Settings is the control surface for preferences, backup/restore, app lock, notifications, privacy, accessibility, Dashboard cards, onboarding revisit, About/legal/support links, destructive finance reset, and hidden developer options.

Backup password/New PIN/Confirm PIN controls are dedicated masked `Entry` fields, not plain `DisplayPromptAsync` secret entry.

Full finance-data deletion uses the dedicated reset handler/service and typed destructive confirmation.

## About UI

About uses packaged app metadata for version/build rather than a duplicated hard-coded version string.

Current About links include product/repository/creator/support/security/contribution/legal information defined by the project identity constants and repository documents.

## Platform UI boundaries

The following should remain behind target-aware adapters/code:

- biometric/Windows Hello;
- sensitive-screen/capture protection;
- local notification scheduling;
- system file pickers;
- system share/save UI;
- packaged app metadata;
- device idiom/window characteristics.

Domain/Application projects should not gain MAUI control/platform dependencies merely to simplify a page.

## Navigation testing

UI-contract tests currently inspect source contracts rather than pretending to be full native automation.

Contract coverage includes adaptive roots, Dashboard/report bindings, Settings security/reset/About wiring, transaction paging/sort, onboarding Privacy/Terms links, signed chart implementation, and passive amount privacy bindings.

Native navigation, focus, resize, accessibility, back behavior, and platform transitions still require emulator/simulator/device validation.