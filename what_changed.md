# What Changed — Finora

Last continuation: **2026-08-18**  
Repository: https://github.com/sanskarIN/Finora  
Current branch: **main**  
Current source line: **Finora 0.2.0 (build 2)**  
Current database schema: **2**

This file is intentionally detailed because implementation, design decisions, source changes, validation boundaries, and release status that would otherwise occupy the chat are recorded here.

---

## 1. Source of truth and product boundary

The uploaded `01_Finora_Personal_Finance_Master_Prompt.md` remains the implementation source of truth for the current project.

Current product identity and release rules remain:

- product name: **Finora**;
- attribution: **Made by the Sanskar**;
- repository: https://github.com/sanskarIN/Finora;
- creator/open-source profile: https://www.github.com/sanskarIN;
- business/security contact: `sanskarin@outlook.in`;
- support contact: `supportramsandesh@gmail.com`;
- license: Apache-2.0;
- framework: .NET MAUI;
- application language/UI: C# + XAML;
- local relational persistence: SQLite through EF Core;
- architecture: App / Application / Domain / Infrastructure / Shared plus Unit/Integration/UI-contract tests;
- current release requires no Finora account or login;
- core finance functionality remains local-first/offline-capable;
- no automatic cloud synchronization;
- no automatic backup upload;
- no analytics/advertising telemetry dependency added to the current line;
- no background location collection;
- transaction location remains manually entered text only;
- persisted/calculated money remains signed 64-bit integer minor units;
- user major-unit conversion/parsing remains `decimal` based;
- current transfer model remains same-currency only;
- no automatic or invented exchange-rate conversion.

Intentionally later-version product boundaries remain:

- remote Finora account/login;
- cloud synchronization;
- collaboration/shared-finance server features;
- server/store-backed commercial entitlement validation;
- automatic exchange-rate conversion;
- analytics/advertising telemetry by default.

These later-version features were not silently introduced as hidden network requirements.

---

## 2. Git commit identity limitation

Requested Git commit email: `sanskarin@outlook.in`.

The GitHub connector used in this ChatGPT environment can create repository commits but does not expose a field that forces the Git author/committer email. Therefore connector-created commits in this continuation cannot truthfully be claimed to use `sanskarin@outlook.in`.

For local Git commits, the intended configuration remains:

```bash
git config user.email "sanskarin@outlook.in"
```

This limitation is documented rather than hidden.

---

## 3. Validation boundary for this continuation

The current execution environment does **not** provide a usable local .NET/MAUI compiler/toolchain.

No claim is made that this continuation locally executed:

- `dotnet restore`;
- `dotnet build`;
- `dotnet test`;
- MAUI workload restore;
- Android packaging;
- Windows packaging;
- iOS archive/build;
- Mac Catalyst archive/build;
- native screen-reader testing;
- signing;
- store-console validation.

The repository contains automated tests, structural preflight source, CI workflows, and detailed platform/release gates, but source presence does not equal a passing native release.

An empty classic GitHub commit-status response is not treated as proof that GitHub Actions/check runs passed.

---

## 4. Architecture retained

Current solution layers remain:

- `src/Finora.Shared`;
- `src/Finora.Domain`;
- `src/Finora.Application`;
- `src/Finora.Infrastructure`;
- `src/Finora.App`;
- `tests/Finora.UnitTests`;
- `tests/Finora.IntegrationTests`;
- `tests/Finora.UiTests`.

Dependency direction remains:

`App -> Application / Infrastructure -> Domain -> Shared`

Repository engineering retained:

- `Finora.sln`;
- central package versions;
- nullable reference types;
- warnings-as-errors;
- latest recommended analysis;
- deterministic build settings;
- `.editorconfig`;
- `.gitattributes`;
- hardened `.gitignore`;
- dependency-free structural preflight;
- staged GitHub Actions workflow;
- CodeQL;
- dependency review;
- Dependabot;
- CODEOWNERS;
- issue templates;
- pull-request template;
- privacy/security/support/legal/contributing documentation;
- test/release/store-readiness documentation.

---

## 5. Dashboard period policy added

A new domain policy now defines Dashboard activity periods instead of scattering date arithmetic through the ViewModel.

### New source

`src/Finora.Domain/DashboardPeriodPolicy.cs`

Supported periods:

- Current financial month;
- Previous financial month;
- Last 30 days;
- Last 90 days;
- Year to date.

The financial-month start remains constrained to day 1–28.

Current behavior:

- current financial month starts on the configured financial start day;
- when today's calendar day is before the configured start day, current financial month begins in the prior calendar month;
- previous financial month resolves the complete preceding financial window;
- trailing 30 days contains 30 local calendar days including today;
- trailing 90 days contains 90 local calendar days including today;
- year-to-date begins January 1 and ends today.

### Tests

Unit coverage now includes:

- financial-month rollover behavior;
- previous financial month;
- 30-day range;
- 90-day range;
- year-to-date;
- boundary behavior around configured start day.

---

## 6. Shared local-calendar to UTC boundary policy added

User-selected dates are local calendar concepts while persisted transaction timestamps are UTC.

A new shared policy removes duplicated UTC-midnight/`23:59:59` conversions.

### New source

`src/Finora.Shared/LocalDateRange.cs`

It converts an inclusive local `DateOnly` range into:

- `FromUtc` inclusive;
- `ToExclusiveUtc` exclusive.

The helper handles:

- non-UTC time-zone offsets;
- invalid local midnight transitions;
- ambiguous local times;
- reversed/invalid ranges;
- full local-day coverage without fractional-last-second gaps.

### Current consumers

The shared local-date boundary policy is now used in current code for appropriate local-calendar workflows including:

- Dashboard selected period;
- Reports selected range;
- account balance trend boundaries;
- budget performance windows;
- monthly comparison grouping/range;
- yearly comparison grouping/range;
- transaction advanced filters;
- Transaction Tools date filters;
- reconciliation statement-day boundary.

### Tests

Unit coverage includes non-UTC fixed-offset conversion and invalid/reversed range behavior.

---

## 7. Dashboard current-balance correctness and UI period selector

Dashboard was hardened so date filters do not redefine current account balance.

Current Dashboard logic now separates:

- **current account balance** — current state from direct account summaries;
- **period activity** — income, spending, net, categories, recent transactions, date-sensitive budget context.

This avoids rebuilding current balance from a selected historical activity period or all-history trend query.

Dashboard UI now includes:

- period picker;
- Apply action;
- resolved date-range label;
- reporting-currency scope explanation.

A stale Dashboard XAML binding was fixed:

- old/incorrect `ReportingCurrencyNotice` reference removed;
- actual `CurrencyScope` ViewModel property is used.

Dashboard continues to avoid the legacy mixed-currency `GetDashboardAsync` aggregate path.

---

## 8. Complete report matrix expanded

The Advanced Report contract/service/UI now exposes the report categories required by the master prompt more completely.

Existing report areas retained:

- spending by category;
- income versus expense;
- account balance trend;
- budget performance;
- merchant/payee;
- monthly comparison;
- tag reporting through category/tag service.

New/expanded report areas in this continuation:

- yearly comparison;
- recurring obligations;
- savings progress.

### Yearly comparison

Returns trailing calendar-year rows with:

- year;
- income minor units;
- expense minor units;
- net minor units.

The UI currently surfaces a five-year comparison.

### Recurring-obligation report

Rows retain:

- rule ID;
- rule name;
- transaction type;
- recurrence status;
- amount;
- currency;
- next due date;
- end date.

Archived rules are not represented as active obligations.

### Savings-progress report

Rows retain:

- goal ID;
- name;
- target;
- current amount derived from checked contribution history;
- currency;
- progress ratio;
- target date;
- completion state.

Savings history is validated while report rows are constructed.

---

## 9. Advanced report local-calendar correctness

Monthly/yearly grouping now follows local calendar meaning instead of grouping UTC timestamps directly.

Current behavior:

- transaction UTC timestamp is converted to local date before month/year grouping;
- account trend day/month boundaries use local calendar ranges;
- budget report windows use `BudgetPeriodPolicy` then `LocalDateRange`;
- current monthly comparison starts at requested trailing-month start and stops at **today**;
- current yearly comparison starts January 1 of first trailing year and stops at **today**.

This continuation specifically fixed a future-date reporting edge:

- future-dated imported transactions can remain stored if otherwise valid;
- they no longer appear in current monthly/yearly comparisons before their local date arrives.

Integration regression coverage now proves this behavior.

---

## 10. Budget report policy alignment

During report expansion, the implementation was checked against the actual shared budget policy API.

Budget performance now uses:

`BudgetPeriodPolicy.TryResolve(...)`

and the actual resolved planned amount instead of assuming a different policy return shape.

This preserves the existing shared budget semantics:

- explicit period precedence;
- weekly Monday–Sunday windows;
- monthly calendar windows;
- custom cadence only within explicit periods;
- rollover only when enabled;
- positive checked effective plan.

---

## 11. Signed report chart correctness fixed

The bar chart previously used absolute magnitude for signed values, which could render a negative net change as a visually positive bar.

`src/Finora.App/Controls/ReportBarChartView.cs` now:

- includes zero in the chart scale;
- calculates a real `zeroY` baseline;
- renders positive values above zero;
- renders negative values below zero;
- does not call `Math.Abs(item.ValueMinor)` to determine signed direction;
- keeps text/list equivalents independently available.

This is particularly important for monthly/yearly net-change reporting.

---

## 12. Report privacy mode hardened

Reports now treat chart geometry as sensitive monetary information.

When either:

- `PrivacyMode`; or
- `HideAmountsOnLaunch`

is active:

- formatted report money becomes `••••`;
- Dashboard/report textual summaries do not reveal monetary values;
- category amount summaries do not reveal category amounts;
- merchant/payee amount rows are masked;
- budget planned/actual/variance rows are masked;
- account balance trend rows are masked;
- recurring-obligation amounts are masked;
- savings current/target amounts are masked;
- monthly/yearly amount rows are masked;
- quantitative chart collections are withheld;
- chart controls are hidden.

Reason:

A bar height can reveal relative magnitude even when its text label is hidden.

---

## 13. Shared passive-money privacy converter added

A reusable multi-value converter was added:

`src/Finora.App/Converters/PrivacyMoneyConverter.cs`

Registered in:

`src/Finora.App/App.xaml`

Inputs:

- stored minor-unit value;
- currency code.

Behavior:

- if privacy/hide-on-launch is active: return `••••`;
- otherwise format through `Money` using the current culture and the row's currency;
- malformed/unavailable display input returns a neutral placeholder rather than raw stored data.

This centralizes passive XAML money display across multiple finance surfaces.

---

## 14. Passive account amount display hardened

### Accounts list

Account balance rows now:

- use each account's currency;
- use currency-aware major-unit formatting;
- honor privacy/hide-on-launch;
- no longer show raw `BalanceMinor` as a user-facing number.

### Account detail

Account detail now:

- masks current balance when privacy/hide-on-launch is active;
- formats opening balance edit text using actual currency decimal places;
- formats credit-limit edit text using actual currency decimal places;
- no longer assumes two decimal places;
- displays account-history transaction amounts through the shared privacy/currency converter;
- aligns credit-card billing-day UI with domain range 1–31.

---

## 15. Transaction history sorting and bounded display added

Transaction history already had search and filtering. This continuation added explicit sorting and bounded incremental presentation.

Sort choices:

- Newest first;
- Oldest first;
- Amount high to low;
- Amount low to high;
- Merchant A–Z.

Presentation paging:

- internal matching set remains available to ViewModel;
- first displayed page is limited to 50 rows;
- **Load more transactions** appends next 50 rows;
- `HasMore` controls Load more visibility;
- `HistoryStatus` reports displayed count vs total matching count.

This is intentionally described as **bounded incremental display**, not database-level paging.

The underlying store search API remains backward-compatible.

---

## 16. Transaction advanced-filter date boundaries fixed

Transaction advanced date filtering now:

- validates end date >= start date;
- converts selected local dates through `LocalDateRange`;
- applies complete local-day bounds;
- avoids duplicated local-midnight/UTC assumptions.

Clear filters restores:

- search text;
- account/category filter;
- type;
- default recent date range;
- default newest-first sort;
- advanced-filter visibility.

---

## 17. Transaction history passive amounts hardened

Transaction list cards now:

- use actual transaction currency;
- format through currency-aware money logic;
- honor privacy/hide-on-launch;
- no longer display raw `AmountMinor` text labelled as “minor”.

This improves both privacy and correctness for currencies whose minor-unit exponent is not two.

---

## 18. Transaction Tools local-date and privacy hardening

Transaction Tools now use `LocalDateRange` for selected From/Through dates.

Current tool behavior retains:

- bulk categorization;
- revision preservation;
- duplicate scanning;
- selected CSV/PDF export;
- no automatic duplicate deletion.

Passive tool rows now:

- use transaction currency;
- format money through shared privacy converter;
- mask amounts under privacy/hide-on-launch.

Duplicate candidates likewise no longer show raw stored minor values.

---

## 19. Transaction detail currency precision fixed

Transaction detail previously formatted editable stored amounts with a fixed two-decimal string.

Current detail formatting now uses:

- actual `Money.DecimalPlaces` for transaction currency;
- safe magnitude conversion that rejects `long.MinValue`;
- current culture for editable major-unit text.

Split editor rows now retain two distinct concepts:

- editable major-unit amount text;
- passive privacy-safe display amount.

XAML passive split list binds `DisplayAmount`, not editable raw amount text.

---

## 20. Budget passive amount display hardened

Budget list cards now:

- format planned amount using budget currency;
- format actual spending using budget currency;
- honor privacy/hide-on-launch;
- avoid raw stored minor-unit labels.

Existing budget create/edit amount inputs remain explicit user-edit controls.

---

## 21. Savings passive amount and forecast privacy hardened

Savings goal cards now:

- format current amount with goal currency;
- format target amount with goal currency;
- honor privacy/hide-on-launch.

Savings planning text now avoids leaking the estimated monthly contribution amount while privacy/hide-on-launch is active.

It still shows non-monetary planning context such as target date and remaining day count.

Existing completion/milestone behavior remains.

---

## 22. Recurring amount display hardened

Recurring rule and occurrence cards now:

- use each rule/occurrence currency;
- format scheduled amount correctly;
- format paid amount correctly;
- honor privacy/hide-on-launch.

Rule type/status/frequency/due dates remain visible because they are not direct monetary values.

Existing lifecycle remains:

- pause;
- resume;
- archive;
- pending;
- paid;
- partially paid;
- skipped;
- postponed;
- reopen.

---

## 23. Reconciliation local-day and privacy fixes

Reconciliation no longer builds a statement boundary by constructing a local `23:59:59` timestamp.

Current statement-date boundary:

- selected local date -> `LocalDateRange`;
- uses final tick before exclusive next-local-day UTC boundary.

This covers the complete local statement date without missing fractional timestamps.

Reconciliation preview now masks:

- book balance;
- statement balance;
- difference

when privacy/hide-on-launch is active.

History now uses a display DTO with formatted/masked difference text rather than binding raw `DifferenceMinor` directly.

---

## 24. Onboarding Privacy + Terms completed

Onboarding already covered:

- no-login/local-first model;
- no automatic upload;
- default currency;
- locale;
- financial-month start;
- optional opening balance;
- explicit sample-data opt-in;
- revisit guidance;
- manual-only location behavior.

This continuation added:

- Privacy button;
- Terms button;
- accessibility descriptions/headings;
- Terms navigation handler.

Revisiting onboarding with existing accounts remains designed not to recreate opening/sample data blindly.

---

## 25. Settings complete finance deletion wiring corrected

Settings XAML now explicitly routes:

**Delete all local finance data**

through:

`OnDeleteAllFinanceDataClicked`

which is the dedicated complete reset workflow backed by the complete finance data reset service.

The UI no longer points at the obsolete partial-delete handler name.

Typed destructive confirmation remains required.

Structural preflight and UI source-contract tests now guard this wiring.

---

## 26. Settings About completeness expanded

The About surface now exposes current source requirements more completely.

Visible identity/technology information includes:

- Finora version/build;
- **Made by the Sanskar**;
- .NET MAUI;
- C#;
- XAML;
- SQLite;
- MVVM.

Links/contacts include:

- repository;
- creator GitHub profile;
- business/security email;
- support email;
- Privacy;
- Terms;
- Third-party notices;
- Contributing guide;
- Security reporting guide;
- Support guide.

License text remains Apache-2.0.

---

## 27. About version/build drift removed

About no longer duplicates version/build as a hard-coded XAML literal.

`SettingsViewModel.AppVersion` now derives from packaged application metadata:

- `AppInfo.Current.VersionString`;
- `AppInfo.Current.BuildString`.

This reduces the chance that source version and displayed About version drift during a later release bump.

UI source-contract tests guard the AppInfo dependency and XAML binding.

---

## 28. Android biometric provider text redacted

Android biometric callback previously received OS/provider `errString` text.

Current callback behavior:

- does not forward provider-supplied `errString` into a public `Result.Failure`;
- returns stable Finora wording;
- preserves PIN fallback;
- avoids exposing raw platform/provider diagnostics in user-visible text.

Structural preflight now checks for regression that routes `errString` into public failure messages.

---

## 29. Android reminder cancellation hardened

Android reminder cancellation previously risked obtaining a PendingIntent in a mode that could create/update an object during cancellation.

Current cancellation uses:

`PendingIntentFlags.NoCreate | PendingIntentFlags.Immutable`

Behavior:

- query for existing reminder PendingIntent;
- if none exists, do nothing;
- if it exists, cancel AlarmManager entry;
- cancel existing PendingIntent;
- do not create a new pending broadcast only to cancel it.

Existing reminder consistency model remains:

- schedule replacement first;
- commit replacement/disable-old database state;
- best-effort cancel old native schedule after commit;
- failed replacement preserves prior working reminder;
- reconciliation cleans disabled/expired drift.

---

## 30. Structural preflight expanded again

`build/scripts/verify_structure.py` now additionally guards current continuation rules.

Current checks include:

- required repository/policy files;
- XML/XAML parsing;
- project references;
- XAML handler wiring;
- solution project references;
- version consistency;
- schema consistency;
- Domain floating-point monetary field patterns;
- Android `allowBackup=false`;
- Android cleartext disabled;
- Android legacy backup exclusion resource wiring;
- Android 12+ data-extraction resource wiring;
- full-domain Android private-data exclusions;
- Settings backup/PIN fields remain masked;
- password/PIN prompts do not regress to unmasked `DisplayPromptAsync`;
- complete finance deletion remains wired to dedicated reset handler;
- raw exception messages are not passed into user alerts;
- Android biometric provider `errString` is not routed into public failure text;
- passive XAML does not label stored `*Minor` values as user-facing raw minor units.

A passing structural preflight still does not replace C# compilation, MAUI XAML compilation, analyzers, tests, native device QA, signing, or store validation.

---

## 31. Advanced report integration coverage added

New integration coverage includes:

- recurring-obligation report retains currency/type/status/next due;
- savings-progress report uses checked contribution history;
- yearly comparison separates current and previous calendar years;
- current monthly/yearly comparison excludes future-dated rows.

The yearly test was hardened so it does not rely on January 15 already having passed; current-year synthetic data now uses today's local date.

---

## 32. Dashboard and local-date unit coverage added

New unit test areas include:

- Dashboard period resolution;
- current financial month;
- previous financial month;
- trailing 30 days;
- trailing 90 days;
- year-to-date;
- financial start boundaries;
- non-UTC local-date UTC conversion;
- inclusive local range semantics;
- invalid/reversed range behavior.

---

## 33. UI source-contract coverage expanded

UI-contract tests do not pretend to be native UI automation. They protect source wiring that can otherwise regress silently.

New/expanded source contracts cover:

### Dashboard

- period picker binding;
- selected-period binding;
- resolved range label;
- `CurrencyScope` binding;
- `DashboardPeriodPolicy.Resolve` usage;
- `LocalDateRange.ToUtc` usage;
- current account summary loading;
- continued absence of legacy `GetDashboardAsync(` call.

### Reports

Required report binding source includes:

- category;
- income/expense;
- monthly;
- yearly;
- merchant;
- budget;
- recurring;
- savings;
- account trend.

Service calls for yearly/recurring/savings are source-guarded.

### Settings

Source contracts cover:

- revisit onboarding;
- default currency;
- financial month start;
- privacy/hide amounts;
- default account/type;
- backup reminder;
- receipt quality;
- sanitized diagnostic export;
- dedicated full-delete handler;
- masked backup password/PIN fields;
- attribution;
- technology summary;
- business/support contacts;
- Apache-2.0;
- Contributing/Security/Support guide;
- packaged `AppInfo` version/build.

### Transactions

Source contracts cover:

- sort picker;
- Load more binding;
- 50-row page size;
- shared local-date filter policy.

### Charts

Source contracts cover:

- scale includes zero;
- signed `ValueMinor / span` direction;
- no `Math.Abs(item.ValueMinor)` magnitude-direction bug.

### Onboarding

Source contracts cover:

- local-first/no-auto-upload wording;
- revisit guidance;
- Privacy link;
- Terms link;
- Terms route.

### Passive privacy surfaces

Source contracts cover:

- shared privacy converter settings rule;
- Accounts;
- Account detail;
- Transactions;
- Transaction Tools;
- Budgets;
- Savings;
- Recurring;
- Reconciliation;
- Transaction detail splits;
- Reports;
- savings forecast;
- currency-specific account detail edit precision.

---

## 34. UI-contract project source inventory expanded

`tests/Finora.UiTests/Finora.UiTests.csproj` now copies current source-contract targets into test output, including:

- Dashboard XAML/ViewModel;
- Reports XAML/ViewModel;
- report chart renderer;
- privacy money converter;
- Accounts page;
- Account Detail XAML/ViewModel;
- Transactions XAML/ViewModel;
- Transaction Detail XAML/ViewModel;
- Transaction Tools XAML/ViewModel;
- Budgets page;
- Savings XAML/ViewModel;
- Recurring page;
- Reconciliation XAML/ViewModel;
- Onboarding XAML/link handler;
- Settings XAML/ViewModel;
- Settings reset/security/About partials.

This allows source-contract tests to inspect actual production source files without adding MAUI runtime dependency to the UI-contract test target.

---

## 35. Current finance persistence invariants retained

Earlier domain/EF boundary hardening remains unchanged in intent.

### Accounts

- name required and bounded;
- valid currency;
- billing day 1–31;
- credit limit nonnegative;
- credit metadata only for credit-card account type.

### Transactions

- nonzero amount;
- no `long.MinValue`;
- valid currency;
- account required;
- timestamp required;
- Expense negative;
- Income/Refund positive;
- transfer group/counterparty required for transfers;
- transfer source/counterparty differ;
- transfer linkage forbidden on non-transfer rows;
- transfer rows cannot contain category splits;
- split amounts nonzero and not `long.MinValue`;
- split signs match parent;
- checked split total equals parent;
- deletion state/timestamp agreement validated.

### Categories/tags

- name/icon metadata validated;
- tag metadata bounded;
- transaction-tag link IDs required;
- hierarchy/cycle protection retained.

### Budgets

- positive limit;
- valid currency;
- threshold 1–100;
- kind/category relationship valid;
- periods valid;
- overlap forbidden.

### Savings

- positive target;
- starting amount 0..target;
- valid currency;
- contribution nonzero/non-`long.MinValue`;
- valid timestamps;
- running history cannot drop below zero.

### Recurrence

- rule name/interval/amount/currency valid;
- valid source account;
- valid date relationship;
- day/grace/reminder bounds;
- no recurring Adjustment template;
- transfer destination required/different;
- category forbidden on transfer recurrence;
- non-transfer destination forbidden;
- occurrence state/payment metadata consistency validated.

### Schema-v2 metadata

EF boundary continues validating:

- attachment metadata;
- transaction revisions;
- reconciliations;
- notifications;
- app settings;
- audit entries;
- backup metadata.

---

## 36. Current accounts/transfers/reconciliation feature set retained

Accounts currently support:

- cash;
- bank;
- credit card;
- wallet;
- savings;
- investment placeholder;
- custom;
- name/icon/color/currency;
- opening/current balance;
- active/hidden/archived state;
- credit limit;
- billing day;
- detail/history;
- default account preference;
- archive/restore;
- reconciliation history.

Transfers remain:

- same-currency only;
- two reciprocal rows;
- shared transfer group;
- equal/opposite amounts;
- atomic workflow;
- paired edit/delete/restore.

Reconciliation retains:

- preview;
- book balance;
- statement balance;
- difference;
- explicit adjustment option;
- history;
- checked arithmetic;
- adjustment link validation;
- reconciled opening-balance protection.

---

## 37. Current transactions feature set retained

Transaction capabilities remain:

- Expense;
- Income;
- Refund;
- Adjustment;
- paired Transfer;
- quick add;
- decimal-safe calculator;
- date/time;
- account/category;
- merchant/payee;
- payment method;
- manually entered location;
- note;
- search/filter;
- detailed edit;
- critical revision history;
- bulk categorization;
- duplicate review;
- splits;
- tags;
- receipt attachments;
- soft delete/restore;
- selected/all CSV export;
- selected/all PDF export.

Current continuation adds sort/incremental display and privacy/date improvements without removing these capabilities.

---

## 38. Current category/tag feature set retained

Categories/tags continue to support:

- default categories;
- user categories;
- subcategories;
- arbitrary-depth cycle prevention;
- reorder;
- archive/restore;
- safe merge/reassignment;
- subcategory-budget semantics protection;
- tag create/edit/archive/restore;
- explicit-currency tag reporting.

---

## 39. Current budget feature set retained

Budgets continue supporting:

- overall;
- category;
- subcategory;
- weekly;
- monthly;
- custom;
- explicit periods;
- rollover;
- warning thresholds;
- recursive category descendants;
- split-aware actuals;
- reminder coordination.

`BudgetPeriodPolicy` remains the shared semantics source.

Explicit-period replacement remains transactional with rollback coverage.

---

## 40. Current savings feature set retained

Savings goals continue supporting:

- name;
- icon;
- target;
- starting amount;
- target date;
- notes;
- contribution;
- withdrawal;
- optional linked transaction;
- forecast;
- milestones;
- completion state;
- reduced-motion-friendly completion messaging.

Earlier safe derived-state repair remains:

- new goal initializes completion when starting amount reaches target;
- startup may repair stale derived completion when history validates;
- corrupt negative/overflowing history is not silently normalized.

---

## 41. Current recurring feature set retained

Recurring items continue supporting:

- expense;
- income;
- refund;
- transfer;
- daily;
- weekly;
- monthly;
- yearly;
- custom interval;
- start/end date;
- grace period;
- reminder lead;
- persisted occurrences;
- idempotent due processing;
- bounded backlog;
- paid;
- partially paid;
- skipped;
- postponed;
- reopened;
- pause;
- resume;
- archive;
- generated transaction validation;
- recurring transfer-pair validation;
- stale reminder cleanup.

Paid occurrences may retain valid historical postponed dates.

---

## 42. Current CSV import feature set retained

CSV import continues to include:

- system file selection;
- header detection;
- explicit field mapping;
- preview;
- validation;
- required date/type/amount/account mappings;
- optional currency/category/merchant/note/payment method/manual location/transfer-group/counterparty/tags mappings;
- major/minor unit option;
- currency-aware major-unit conversion;
- UTF-8 validation;
- file-size limit;
- row-count limit;
- quoted field parsing;
- account/category/tag resolution;
- optional category creation;
- duplicate protection;
- same-batch duplicate protection;
- transfer pair/counterparty checks;
- `long.MinValue` protection;
- transaction/account currency validation;
- transactional commit;
- explicit row errors.

---

## 43. Current export feature set retained

CSV export continues to include rich transaction fields.

PDF export remains dependency-free and multi-page.

Export remains explicit user action through system share/save surfaces.

Temporary app-owned share copies remain cache artifacts and are eligible for bounded stale cleanup.

---

## 44. Current attachment/path hardening retained

Receipts/documents remain app-private files with SQLite metadata.

Current protections retained:

- generated internal filename;
- sanitized original filename;
- path confinement;
- platform-correct path comparison;
- symbolic-link/reparse traversal rejection;
- content-type allow-list;
- per-file size limit;
- async copy;
- byte count;
- SHA-256 metadata;
- open/delete/storage-usage/orphan cleanup;
- encrypted backup inclusion.

No-link policy continues through:

- attachment access;
- backup validation;
- restore staging;
- crash-safe rollback copy;
- recovery journal paths;
- integrity checking;
- privacy log storage where applicable.

---

## 45. Current encrypted backup/restore hardening retained

Backup remains explicit user action only.

Cryptography remains:

- PBKDF2-SHA256;
- random salt;
- high iteration count;
- AES-GCM;
- random nonce/tag;
- authenticated format magic.

Snapshot/restore validation continues covering:

- schema;
- IDs;
- accounts/currencies;
- transactions;
- transfers;
- splits;
- categories;
- tags;
- budgets/periods;
- goals/contributions;
- recurrence rules/occurrences;
- attachments;
- revisions;
- reconciliations;
- notification metadata;
- settings boundaries.

Crash-safe restore retains:

- serialized operation gate;
- pre-restore attachment snapshot;
- durable journal;
- pending DB marker;
- staged attachments;
- transactional DB replacement;
- startup recovery before finance navigation;
- pre-commit prior-tree restoration;
- post-commit new-tree finalization;
- safe orphan staging cleanup.

Sensitive receipt/plaintext byte buffers continue to be cleared as early as practical on success/failure.

Finora still cannot recover a forgotten backup password.

---

## 46. Current notification hardening retained

Notification system retains:

- persisted schedule records;
- dedupe keys;
- permission handling;
- Android gateway;
- iOS/Mac Catalyst gateway;
- Windows gateway;
- backup reminders;
- budget warning reminders;
- recurring reminders;
- generic privacy-safe text;
- stale schedule cleanup.

Failure-safe replacement remains:

1. schedule new native reminder first;
2. commit new enabled row / old disabled row;
3. cancel stale native reminder after commit;
4. if new native scheduling fails, leave old reminder enabled;
5. reconcile disabled/expired drift best-effort.

Current continuation adds Android `NoCreate` cancellation behavior.

---

## 47. Current PIN/biometric/security hardening retained

PIN remains:

- 4–12 ASCII digits;
- random salt;
- PBKDF2-SHA256 verifier;
- fixed-time verifier comparison;
- OS secure storage;
- bounded escalating lockout;
- inactivity auto-lock.

Secure-storage state handling remains:

- temporary provider failure fails closed if lock-enabled marker exists;
- readable missing/corrupt verifier can clear stale marker to prevent permanent nonexistent-verifier trap.

Settings secret entries remain:

- masked backup password;
- masked new PIN;
- masked confirm PIN;
- cleared from UI after operations.

Lock-screen PIN remains masked/cleared.

Biometric/Windows Hello remains optional with PIN fallback.

Current continuation additionally redacts Android provider callback text.

---

## 48. Current Android privacy packaging retained

Android manifest/source continues to enforce:

- `android:allowBackup="false"`;
- `android:usesCleartextTraffic="false"`;
- legacy full-backup rules;
- Android 12+ data-extraction rules.

Private-data domains excluded in source policy include:

- root;
- file;
- database;
- shared preferences;
- external app storage.

Final merged-manifest and physical device/cloud-transfer behavior remains an external release gate.

---

## 49. Current temporary artifact cleanup retained

Startup cleaner remains best-effort and narrowly scoped.

Eligible stale patterns include known Finora-generated:

- CSV share copies;
- PDF share copies;
- encrypted backup share copies;
- integrity report share copies.

Grace period remains 24 hours.

Cleaner preserves:

- fresh managed copies;
- unrelated cache files;
- diagnostic logs.

File links are removed as links rather than traversing target.

Cleanup failure does not block finance startup.

---

## 50. Current diagnostics/integrity hardening retained

Privacy logger continues to:

- ignore arbitrary caller property dictionaries;
- record event/type tokens rather than private payload;
- avoid exception message/stack serialization;
- bound/sanitize event tokens;
- rotate bounded local file;
- reject linked log paths.

ViewModel error mapper continues to:

- preserve short user/action validation messages;
- redact filesystem/database/crypto/provider-style infrastructure text.

Unexpected `AsyncCommand` failures remain contained and routed through privacy-safe logging.

Integrity checker continues covering:

- SQLite integrity;
- foreign keys;
- account/transaction/currency state;
- transfer pairs;
- split totals/signs;
- category cycles;
- budgets/periods;
- goal histories/completion;
- recurrence relations/payment state;
- reconciliation links/arithmetic;
- attachment parent/path/presence/size/hash/link state.

Export remains counts/codes only, not private finance contents.

---

## 51. Current adaptive UI/accessibility source retained

Navigation remains adaptive:

- phone bottom tabs;
- tablet/desktop flyout/sidebar;
- adaptive root switching;
- primary-section preservation;
- onboarding/unlock adaptive routing.

Accessibility-related source retains:

- scalable minimum control sizing;
- larger interface preference;
- reduced motion preference;
- semantic headings/descriptions;
- report text equivalents;
- recurrence control descriptions;
- lock-screen semantics.

Current continuation adds semantics/source contracts around:

- Dashboard period selector;
- report sections;
- transaction filters/sort/load-more;
- onboarding Privacy/Terms;
- Settings About/security/destructive controls.

Native TalkBack/VoiceOver/Narrator/keyboard/focus/large-text/high-contrast validation remains external.

---

## 52. Localization state retained

Current source remains:

- English-first baseline;
- localization-ready architecture;
- Hindi starter/common resource structure;
- runtime locale normalization/application;
- date/number formatting preview.

This continuation does not claim complete Hindi translation.

---

## 53. Settings current surface

Settings currently includes:

- System/Light/Dark theme;
- reduced motion;
- larger interface;
- privacy mode;
- hide amounts on launch;
- sensitive-screen protection preference;
- biometric/Windows Hello preference;
- default currency;
- locale;
- number/date formatting preview;
- financial month start day;
- default account;
- default transaction type;
- local notifications;
- backup reminders;
- notification permission/sync action;
- receipt quality;
- attachment storage usage;
- orphan attachment cleanup;
- masked encrypted-backup password;
- backup create/restore;
- sanitized diagnostic export;
- inactivity auto-lock;
- masked PIN set/change fields;
- safe PIN removal;
- category/tag management;
- revisit onboarding;
- complete local finance-data deletion;
- About identity/docs/contacts;
- hidden developer panel;
- schema display;
- data integrity check;
- deterministic synthetic sample reset;
- feature flags;
- reminder sync simulation;
- local premium demo flag.

---

## 54. Developer-option source retained

Hidden developer panel remains available behind the existing unlock gesture/flow.

It includes:

- schema version;
- local integrity check;
- synthetic sample-data reset;
- feature flags;
- reminder synchronization simulation;
- local premium demo flag.

Local premium remains explicitly non-tamper-proof and is not represented as secure commercial entitlement.

---

## 55. Documentation aligned in this continuation

The following repository documents were updated to match the current source rather than leaving the new behavior only in code:

- `DECISIONS.md`;
- `docs/TEST_PLAN.md`;
- `CHANGELOG.md`;
- `PROJECT_STATUS.md`;
- `docs/privacy/DATA_LIFECYCLE.md`;
- `docs/security/THREAT_MODEL.md`;
- `docs/releases/RELEASE_CHECKLIST.md`;
- `docs/releases/STORE_READINESS.md`;
- `README.md`;
- `what_changed.md`.

New documented decisions include:

- local calendar/date boundary policy;
- Dashboard period semantics;
- passive finance amount hiding;
- quantitative chart suppression under privacy mode;
- true zero baseline for signed charts;
- explicit report matrix;
- current comparison exclusion of future-dated rows.

---

## 56. Release checklist expanded

`docs/releases/RELEASE_CHECKLIST.md` now includes explicit gates for:

- structural preflight extensions;
- local-calendar/date correctness;
- Dashboard period choices;
- complete report matrix;
- signed chart direction;
- transaction sort/incremental display;
- passive privacy across finance surfaces;
- currency-specific edit precision;
- dedicated full-reset wiring;
- About packaged version/links;
- onboarding Privacy/Terms/revisit;
- biometric provider text;
- Android `NoCreate` reminder cancellation;
- Android automatic-backup/device-transfer exclusions;
- platform accessibility for new controls;
- store metadata consistency.

None of those release boxes are marked complete from source inspection alone.

---

## 57. Store-readiness matrix expanded

`docs/releases/STORE_READINESS.md` now requires platform evidence for the new source behaviors.

Android gates include:

- merged backup/privacy manifest;
- backup/data-transfer exclusions;
- notification replacement/cancellation;
- `NoCreate` cancellation behavior;
- biometric provider-text redaction;
- Dashboard local periods;
- local monthly/yearly reports;
- signed chart direction;
- privacy amount/chart behavior;
- transaction sort/load-more;
- TalkBack navigation;
- onboarding/settings controls.

Windows gates include:

- packaged metadata;
- Hello/toasts;
- local calendar under non-UTC zone;
- privacy mode;
- signed charts;
- transaction sort/load-more;
- keyboard/Narrator/high-DPI/resizing.

Apple gates include:

- LocalAuthentication/UserNotifications;
- local-calendar/DST behavior;
- privacy amount/chart behavior;
- signed charts;
- transaction sort/load-more;
- Onboarding/Settings accessibility;
- VoiceOver/Dynamic Type/reduced motion.

---

## 58. Continuation changed-file inventory

The 2026-08-11 continuation intentionally touched the following implementation/test/documentation areas.

### Domain/Shared

- `src/Finora.Domain/DashboardPeriodPolicy.cs` — new Dashboard period policy.
- `src/Finora.Shared/LocalDateRange.cs` — new local calendar -> UTC range policy.

### Application

- Advanced reporting contract file containing `IAdvancedReportService`/report DTOs — yearly, recurring-obligation and savings-progress contract expansion.

### Infrastructure

- `src/Finora.Infrastructure/AdvancedReportService.cs` — local-calendar reports, new report types, future-date exclusion, budget-policy alignment.

### App — Dashboard/Reports

- `src/Finora.App/ViewModels/DashboardViewModel.cs` — selected periods/local boundaries/current balance source.
- `src/Finora.App/Pages/DashboardPage.xaml` — period picker/range/currency-scope binding.
- `src/Finora.App/ViewModels/ReportsViewModel.cs` — complete report rows and privacy-aware formatting/chart suppression.
- `src/Finora.App/Pages/ReportsPage.xaml` — yearly/recurring/savings sections and privacy-aware chart visibility.
- `src/Finora.App/Controls/ReportBarChartView.cs` — signed zero-baseline renderer.

### App — privacy money display

- `src/Finora.App/Converters/PrivacyMoneyConverter.cs` — new reusable passive-money formatter/hider.
- `src/Finora.App/App.xaml` — converter registration.
- `src/Finora.App/Pages/AccountsPage.xaml` — account balance formatting/privacy.
- `src/Finora.App/ViewModels/AccountDetailViewModel.cs` — account detail privacy/currency precision.
- `src/Finora.App/Pages/AccountDetailPage.xaml` — history privacy + billing day 1–31.
- `src/Finora.App/Pages/BudgetsPage.xaml` — planned/actual privacy/currency.
- `src/Finora.App/ViewModels/SavingsViewModel.cs` — forecast privacy.
- `src/Finora.App/Pages/SavingsPage.xaml` — goal money privacy/currency.
- `src/Finora.App/Pages/RecurringPage.xaml` — rule/occurrence money privacy/currency.
- `src/Finora.App/ViewModels/ReconciliationViewModel.cs` — privacy display/local statement boundary.
- `src/Finora.App/Pages/ReconciliationPage.xaml` — formatted history display.

### App — transactions

- `src/Finora.App/ViewModels/TransactionsViewModel.cs` — sorting, 50-row incremental display, local-date filters.
- `src/Finora.App/Pages/TransactionsPage.xaml` — sort/load-more/privacy display.
- `src/Finora.App/ViewModels/TransactionToolsViewModel.cs` — local-date filters.
- `src/Finora.App/Pages/TransactionToolsPage.xaml` — privacy/currency display.
- `src/Finora.App/ViewModels/TransactionDetailViewModel.cs` — currency edit precision, safe magnitude, privacy split display.
- `src/Finora.App/Pages/TransactionDetailPage.xaml` — privacy split binding.

### App — onboarding/settings/security/platform

- `src/Finora.App/Pages/OnboardingPage.xaml` — Privacy/Terms/accessibility.
- `src/Finora.App/Pages/OnboardingPage.Links.cs` — Terms route.
- `src/Finora.App/Pages/SettingsPage.xaml` — complete reset handler, About links, packaged version binding.
- `src/Finora.App/Pages/SettingsPage.About.cs` — Contributing/Security/Support links.
- `src/Finora.App/ViewModels/SettingsViewModel.cs` — packaged AppInfo version/build.
- `src/Finora.App/PlatformBiometricService.cs` — Android provider-text redaction.
- `src/Finora.App/PlatformNotificationGateway.cs` — Android `PendingIntentFlags.NoCreate` cancellation.

### Build/preflight

- `build/scripts/verify_structure.py` — complete-reset, biometric-provider text and raw minor-display guards in addition to existing checks.

### Unit tests

- Dashboard period-policy test source.
- Local-date UTC-boundary test source.

### Integration tests

- `tests/Finora.IntegrationTests/AdvancedReportCoverageTests.cs` — yearly/recurring/savings/future-date report coverage.

### UI-contract tests

- `tests/Finora.UiTests/Finora.UiTests.csproj` — expanded production source-copy inventory.
- `tests/Finora.UiTests/ReportDashboardSourceContractTests.cs` — Dashboard/report source contracts.
- `tests/Finora.UiTests/SettingsSourceContractTests.cs` — Settings reset/identity/version/secret/docs contracts.
- `tests/Finora.UiTests/TransactionsChartOnboardingContractTests.cs` — transaction paging/chart/onboarding contracts.
- `tests/Finora.UiTests/PrivacyAmountSurfaceContractTests.cs` — privacy/currency surface contracts.

### Documentation

- `DECISIONS.md`;
- `docs/TEST_PLAN.md`;
- `CHANGELOG.md`;
- `PROJECT_STATUS.md`;
- `docs/privacy/DATA_LIFECYCLE.md`;
- `docs/security/THREAT_MODEL.md`;
- `docs/releases/RELEASE_CHECKLIST.md`;
- `docs/releases/STORE_READINESS.md`;
- `README.md`;
- `what_changed.md`.

No intentionally changed implementation file from this continuation is omitted from the ledger by design; the exact Git history remains the authoritative commit-by-commit record.

---

## 59. Representative focused commit trail — 2026-08-11

The continuation was intentionally split into many focused commits. Representative commit messages include:

- `fix(dashboard): bind reporting currency notice to current viewmodel property`
- `feat(dashboard): add deterministic dashboard period policy`
- `test(dashboard): cover financial month rolling and trailing period ranges`
- `fix(time): add timezone-safe local date range conversion`
- `test(time): cover local-date UTC conversion and boundary validation`
- `feat(dashboard): add selectable date ranges and local-day report boundaries`
- `feat(dashboard): expose period selector and resolved date range`
- `feat(reports): add recurring savings and yearly report contracts`
- `feat(reports): implement recurring savings yearly and local-calendar reporting`
- `fix(reports): align budget reporting with shared period policy contract`
- `test(reports): cover recurring savings and yearly report requirements`
- `feat(reports): expose yearly recurring and savings progress report data`
- `feat(reports): add yearly recurring and savings progress sections`
- `test(ui): enforce dashboard period and complete report source bindings`
- `fix(settings): route full data deletion through complete reset service`
- `feat(settings): expose contributing security and support documentation links`
- `feat(settings): implement repository contribution security and support links`
- `fix(security): redact Android biometric provider error text`
- `ci(preflight): guard biometric text redaction and complete reset wiring`
- `fix(settings): derive About version from packaged app metadata`
- `fix(settings): bind About version to packaged metadata`
- `fix(android): cancel reminders without creating pending intents`
- `fix(charts): render negative report values below a true zero baseline`
- `feat(transactions): add sorting bounded history pages and safe date filters`
- `feat(transactions): expose sorting and incremental history controls`
- `feat(onboarding): add terms access and accessibility semantics`
- `feat(onboarding): implement terms navigation`
- `test(ui): enforce transaction paging chart baseline and onboarding legal links`
- `feat(privacy): add reusable currency-aware hidden-money converter`
- `feat(privacy): register shared hidden-money converter`
- `fix(accounts): format and hide account balances consistently`
- `fix(transactions): format and hide history amounts consistently`
- `fix(budgets): format and hide budget amounts consistently`
- `fix(savings): format and hide goal amounts consistently`
- `fix(recurring): format and hide recurring amounts consistently`
- `fix(accounts): honor privacy and currency precision in account detail`
- `fix(accounts): align account detail billing range and hidden transaction amounts`
- `fix(reconciliation): honor privacy and use safe local statement boundaries`
- `fix(reconciliation): display privacy-safe formatted history values`
- `fix(transactions): honor currency precision and privacy in detail splits`
- `fix(transactions): display privacy-safe split amounts in detail`
- `fix(privacy): mask report values and suppress quantitative charts`
- `fix(privacy): hide quantitative report charts with hidden amounts`
- `fix(reports): remove collection-expression inference risk in privacy branches`
- `fix(privacy): hide savings forecast amounts when privacy mode is active`
- `fix(transactions): use shared local-date boundaries in transaction tools`
- `fix(transactions): format and hide transaction tools amounts consistently`
- `test(ui): enforce privacy-safe currency display across finance surfaces`
- `ci(preflight): reject raw minor-unit display bindings in XAML`
- `fix(reports): exclude future-dated rows from current comparisons`
- `test(reports): prove current comparisons exclude future-dated rows`
- `test(reports): remove calendar-date dependency from yearly comparison coverage`
- `docs(decisions): record local-calendar privacy and report completeness rules`
- `docs(test): add dashboard report privacy date and paging regression gates`
- `docs(changelog): record dashboard reports privacy and local-date hardening`
- `docs(status): refresh dashboard report privacy and transaction coverage`
- `docs(privacy): document local-calendar and passive amount privacy lifecycle`
- `docs(security): add display privacy local-date and chart integrity threats`
- `docs(release): add dashboard privacy report and local-date release gates`
- `docs(store): add dashboard privacy local-date and report platform gates`
- `docs(readme): expose current dashboard reports privacy and local-date behavior`
- final ledger commit updating this file.

The exact commit list/history at `main` is authoritative if a commit is not named in this representative list.

---

## 60. Current automated/source test inventory remains broad

Current repository test areas include, among others:

- Money conversion/rounding/currency precision;
- DomainRules;
- DashboardPeriodPolicy;
- LocalDateRange;
- Decimal calculator;
- Culture settings;
- PIN attempt policy;
- ViewModelBase/AsyncCommand behavior;
- transfers;
- reconciliation;
- category mutation safety;
- tag currency scope;
- budgets/rollover/custom-period rollback;
- recurrence payment links/lifecycle/state transitions;
- report currency isolation;
- report consistency;
- yearly/recurring/savings/future-date reports;
- currency-aware CSV import;
- backup graph validation;
- receipt backup round-trip;
- crash-safe restore recovery;
- finance-data reset;
- sample-data reset;
- persistence invariants;
- schema-v2 metadata persistence invariants;
- migration v1→v2;
- integrity regression;
- attachment path/symlink safety;
- privacy logger;
- local notification consistency;
- temporary artifact cleaner;
- adaptive navigation source contracts;
- Dashboard/report source contracts;
- Settings source contracts;
- transaction/chart/onboarding source contracts;
- passive amount/privacy source contracts.

Again, test source presence is not represented as passing execution in this environment.

---

## 61. External native validation remains required

Before Finora 0.2.0 can be represented as production store-ready, execute and retain evidence for:

### General

- structural preflight;
- exact NuGet/workload restore;
- Release build;
- analyzer output;
- unit tests;
- integration tests;
- UI-contract tests;
- dependency/license/security review.

### Android

- MAUI Android Release build;
- signed AAB;
- adaptive/monochrome icon and splash;
- merged manifest;
- backup/data-transfer exclusion resources;
- actual cloud backup/device-transfer behavior;
- notification permission/scheduling/replacement/cancellation;
- `NoCreate` cancellation behavior;
- biometric success/cancel/unavailable/failure/lockout;
- provider-text redaction;
- PIN fallback;
- `FLAG_SECURE` behavior;
- local-calendar behavior under non-UTC zone;
- signed report charts;
- privacy-mode passive money/chart suppression;
- transaction sort/load-more;
- TalkBack/large text/reduced motion;
- file picker/share/backup/restore/import/export/receipts;
- migration/upgrade.

### Windows

- MAUI Windows Release/package;
- final identity/publisher/signing;
- Windows Hello;
- scheduled toasts;
- display-affinity capture behavior;
- file/share/export/backup/receipt behavior;
- local calendar under non-UTC/DST zone;
- signed chart direction;
- privacy-mode passive amount hiding;
- Dashboard period picker;
- transaction sort/load-more;
- keyboard/focus/Narrator/high-DPI/resizing;
- migration/upgrade.

### iOS

- supported Xcode/.NET MAUI archive/build;
- provisioning/signing;
- LocalAuthentication;
- UserNotifications;
- file picker/share/import/export/backup/receipt flows;
- local calendar under non-UTC/DST zones;
- signed chart direction;
- privacy amount/chart behavior;
- transaction sort/load-more;
- VoiceOver/Dynamic Type/reduced motion/dark mode;
- migration/upgrade;
- App Store privacy declarations.

### Mac Catalyst

- archive/build;
- signing/notarization;
- LocalAuthentication/UserNotifications;
- file picker/share/import/export/backup/receipt flows;
- local calendar/time-zone behavior;
- signed chart direction;
- privacy amount/chart behavior;
- transaction sort/load-more;
- keyboard/mouse/focus/VoiceOver/high-DPI/resizing;
- migration/upgrade.

---

## 62. Release evidence must remain evidence-based

Do not mark a release gate complete because:

- source exists;
- a file appears syntactically plausible;
- a unit test source exists but was not run;
- a platform conditional branch exists;
- classic GitHub status list is empty;
- a manifest source looks correct before merged-manifest/package inspection.

Production claims require actual compiler/test/platform/store evidence.

---

## 63. No regression in current release product promises

This continuation does not add or claim:

- investment-return guarantees;
- financial advice;
- cloud synchronization;
- remote account authentication;
- automatic exchange rates;
- secure commercial entitlement from the local demo flag;
- universal screenshot blocking;
- universal native notification delivery guarantees;
- complete Hindi localization;
- bug-free operation.

---

## 64. Current release decision

**Finora 0.2.0 source is materially more complete and hardened for dashboard periods, full reporting, local-calendar correctness, passive monetary privacy, signed chart integrity, transaction history usability, Settings/About/onboarding completeness, Android biometric-message privacy, and Android reminder cancellation.**

The source line must still pass the external compiler, automated-test, native-device, accessibility, packaging, signing, migration, backup/recovery, privacy, and store gates documented in:

- `docs/TEST_PLAN.md`;
- `docs/releases/RELEASE_CHECKLIST.md`;
- `docs/releases/STORE_READINESS.md`.

No source-only continuation should be represented as a production-store release until those gates have actual evidence.

---

## 65. Complete project documentation continuation

A dedicated documentation completion pass was performed after the implementation-hardening continuation above.

Starting documentation-pass head:

`180aa293526eadfc4ad700017266f357ce22ede2`

Starting commit message:

`docs(status): finalize Finora dashboard reports privacy and local-date ledger`

Before this final ledger write, `main` was **33 commits ahead** of that starting head for the documentation pass.

The pass intentionally concentrated on complete user, developer, architecture, security, privacy, operations, testing, platform, migration, and store/release documentation while preserving the already-implemented source code on `main`.

The final ledger commit itself is an additional focused documentation commit after those 33 pre-ledger commits.

---

## 66. Documentation hub and completeness matrix added

New documentation entry point:

`docs/README.md`

It provides navigable links to:

- project overview;
- documentation status;
- end-user guide;
- architecture/schema/service/data-flow/navigation docs;
- feature manuals;
- accessibility/localization guide;
- development docs;
- testing docs;
- diagnostics/operations docs;
- security/privacy docs;
- platform docs;
- release docs;
- community/legal docs.

New completeness matrix:

`docs/DOCUMENTATION_STATUS.md`

It records current documentation coverage for:

- public project overview;
- user workflows;
- architecture;
- database/schema;
- services/data flow/navigation;
- finance feature families;
- Settings;
- accessibility/localization;
- app lock/privacy;
- backup/recovery;
- threat model/data lifecycle;
- diagnostics/reset/sample data;
- build/troubleshooting;
- developer/code-map/feature-change workflow;
- testing/native validation;
- Android/Windows/Apple platform guidance;
- release/store/versioning docs;
- project status/change ledger/changelog/legal/community files.

The matrix also defines a documentation update policy for future persisted fields, passive monetary UI, native APIs/permissions, accessibility/localization work, and later-version network features.

---

## 67. Complete end-user guide added

New file:

`docs/USER_GUIDE.md`

The guide documents current end-user behavior for:

- local-first product model;
- first launch/onboarding;
- adaptive navigation;
- Dashboard cards and period selection;
- accounts and credit-card metadata;
- same-currency transfers;
- transaction quick-add/search/filter/sort/paging/detail;
- categories/tags;
- splits;
- receipts;
- reconciliation;
- budgets;
- savings goals;
- recurring rules/occurrences;
- report matrix;
- CSV import;
- CSV/PDF export;
- encrypted backup/restore and crash recovery;
- privacy mode/hide amounts;
- PIN/biometric behavior;
- notifications;
- Settings;
- developer tools;
- full finance-data deletion;
- accessibility;
- troubleshooting/support;
- current explicit product limitations.

The user guide distinguishes implemented behavior from native/platform validation requirements and does not claim later-version cloud/login/FX features.

---

## 68. Finance feature manuals added

New feature documentation:

### `docs/features/ACCOUNTS_AND_TRANSACTIONS.md`

Covers:

- account lifecycle/currency invariants;
- current/opening balances;
- credit-card metadata;
- archive restrictions;
- linked same-currency transfers;
- transaction sign rules;
- quick add;
- date/time behavior;
- search/filter/sort/paging;
- revisions;
- soft delete/restore;
- categories/tags;
- splits;
- receipts;
- duplicate review;
- bulk categorization;
- Transaction Tools;
- reconciliation;
- passive amount privacy;
- export/integrity boundaries.

### `docs/features/BUDGETS_GOALS_RECURRING.md`

Covers:

- budget kinds/cadence;
- `BudgetPeriodPolicy` semantics;
- split/descendant actuals;
- warning thresholds;
- savings target/history/completion/forecast;
- recurrence lifecycle;
- occurrence-first payment model;
- recurring transfers;
- dependency safety;
- backlog protection;
- notifications/reporting/privacy/integrity.

### `docs/features/REPORTS_IMPORT_EXPORT.md`

Covers:

- reporting currency isolation;
- local-date range behavior;
- category/income-expense/account/budget/merchant/monthly/yearly/recurring/savings/tag reports;
- signed zero-baseline charts;
- privacy chart suppression;
- CSV limits/mapping/amount modes;
- account/category/tag resolution;
- duplicates/transfers;
- preview/transactional import;
- CSV/PDF export;
- share/save trust boundary;
- release validation.

### `docs/features/SETTINGS_REFERENCE.md`

Covers:

- default currency/locale;
- financial-month start;
- privacy/hide amounts;
- theme/reduced motion/larger interface;
- backup reminders/notifications;
- onboarding state;
- auto-lock;
- biometric/sensitive-screen settings;
- receipt quality;
- default account/type;
- last backup timestamp;
- Dashboard cards;
- local premium demo;
- transient backup password;
- PIN storage boundary;
- About/legal/support links;
- backup/restore;
- complete finance deletion;
- developer panel;
- preference storage/reset distinctions.

---

## 69. Architecture/service/data-flow/navigation documentation added

New architecture docs:

### `docs/architecture/SERVICE_CATALOG.md`

Maps current dependency-injection/services to responsibilities, including:

- `DatabaseInitializer`;
- `IFinanceStore`;
- finance reset/sample services;
- restore recovery;
- transaction maintenance;
- account management;
- category/tag service;
- reconciliation;
- recurring workflow;
- CSV import;
- advanced reports;
- crash-safe backup;
- export;
- attachments;
- integrity;
- temporary cleanup;
- privacy logger;
- exception coordinator;
- settings/app lock;
- native notifications;
- local notification persistence;
- biometrics;
- sensitive-screen protection;
- reminder coordinator;
- cross-cutting money/date/budget/error/platform rules.

### `docs/architecture/DATA_FLOW.md`

Documents end-to-end flows for:

- startup/recovery;
- Settings;
- accounts;
- transactions;
- transfers;
- splits;
- transaction search;
- reconciliation;
- budgets;
- savings;
- recurring;
- reports;
- CSV import/export;
- receipt attachments;
- encrypted backup creation;
- encrypted restore and recovery;
- integrity diagnostics;
- privacy logger;
- notifications;
- passive privacy display;
- finance reset;
- synthetic sample reset.

### `docs/architecture/NAVIGATION_AND_UI.md`

Documents:

- mobile five-tab hierarchy;
- desktop/tablet flyout equivalents;
- 900-pixel adaptive threshold plus idiom behavior;
- hidden onboarding/lock roots;
- secondary workflow routes;
- MVVM/code-behind boundary;
- ViewModelBase behavior;
- Dashboard/transactions/reports UI contracts;
- signed chart semantics;
- passive privacy display;
- theme/accessibility;
- onboarding/lock/Settings/About UI;
- platform UI boundaries;
- UI-contract vs native testing distinction.

Existing `docs/architecture/OVERVIEW.md`, `DATABASE_SCHEMA.md`, and `DECISIONS.md` remain the higher-level design/schema/decision sources and are indexed from the new hub.

---

## 70. Security/privacy documentation expanded with dedicated manuals

New file:

`docs/security/APP_LOCK_AND_PRIVACY.md`

Documents:

- current PIN format;
- PBKDF2-SHA256 verifier flow;
- 150,000 app-lock iterations;
- SecureStorage boundary;
- fail-closed provider failure vs stale readable verifier state;
- bounded failed-attempt lockout;
- PIN removal behavior;
- biometrics/Windows Hello with PIN fallback;
- secret-entry masking;
- privacy-mode surfaces;
- chart privacy;
- screen-capture limitations;
- manual-only location;
- notification privacy;
- diagnostic privacy;
- Android backup/device-transfer source controls;
- local premium demo limitation;
- native release validation.

New file:

`docs/security/BACKUP_AND_RECOVERY.md`

Documents:

- user-triggered-only backup boundary;
- `FINORA01` format identity;
- schema 2 relation;
- PBKDF2-SHA256/AES-GCM format;
- 210,000 backup key-derivation iterations;
- registered `CrashSafeBackupService` wrapper;
- creation/snapshot validation;
- preview;
- staging/database replacement;
- rollback/finalization;
- durable recovery journal/marker;
- startup recovery decisions;
- orphan restore-directory handling;
- attachment path safety;
- masked password handling;
- cache share-copy lifecycle;
- failure/error behavior;
- automated and native failure-injection requirements;
- recovery limitations.

These complement rather than replace `docs/security/THREAT_MODEL.md` and `docs/privacy/DATA_LIFECYCLE.md`.

---

## 71. Diagnostics, integrity, reset and sample-data operations documented

New file:

`docs/operations/DIAGNOSTICS_AND_INTEGRITY.md`

Documents:

- privacy logger goals;
- forbidden diagnostic content;
- bounded log storage/rotation;
- AsyncCommand/exception coordinator flow;
- UI error mapping;
- structural privacy preflight;
- data-integrity service scope;
- sanitized output;
- persistence-boundary vs integrity distinction;
- attachment integrity;
- sanitized report export;
- temporary artifact cleanup;
- developer/support workflow;
- release validation.

New file:

`docs/operations/DATA_RESET_AND_SAMPLE_DATA.md`

Documents:

- distinction between complete finance deletion and sample reset;
- typed confirmation;
- finance tables/relationships removed;
- intentional preference/schema/security-state preservation boundary;
- attachment orphan cleanup;
- transactional database deletion;
- external-copy limitation;
- deterministic sample reset sequence;
- synthetic-data privacy rule;
- test/release expectations;
- factory-reset distinction.

---

## 72. Developer documentation added

New file:

`docs/development/DEVELOPER_GUIDE.md`

Covers:

- prerequisites/clone/verification;
- solution layering;
- money/date/budget/transfer/recurrence rules;
- persistence/migration requirements;
- attachment/backup/privacy/display/platform rules;
- UI/navigation;
- async/cancellation;
- testing layers;
- structural preflight;
- documentation requirements;
- commit hygiene;
- local Git email configuration;
- review/release honesty.

New file:

`docs/development/CODE_MAP.md`

Maps root, Shared, Domain, Application, Infrastructure, App, tests, build scripts, GitHub automation, and docs areas, with guidance on where different change types belong.

New file:

`docs/development/ADDING_A_FEATURE.md`

Defines the change workflow for:

- product boundary;
- layer selection;
- money/date/schema/service rules;
- transfer/reconciliation/recurrence special rules;
- file/security/privacy/UI/platform review;
- test layers;
- structural preflight;
- documentation updates;
- focused commit order;
- final evidence separation.

---

## 73. Testing documentation added

New file:

`docs/testing/TESTING_GUIDE.md`

Documents practical commands and test-layer selection for:

- structural preflight;
- UnitTests;
- IntegrationTests;
- UiTests source contracts;
- host wrappers;
- money/date/database/transfer/backup/import/privacy/accessibility cases;
- native build commands via build guide;
- synthetic test data;
- regression-test policy;
- release evidence.

New file:

`docs/testing/NATIVE_VALIDATION_MATRIX.md`

Defines platform evidence requirements for:

- common functional flows;
- Android native APIs/privacy/backup/accessibility;
- Windows package/Hello/toasts/capture/accessibility;
- iOS LocalAuthentication/UserNotifications/accessibility;
- Mac Catalyst desktop behavior;
- privacy-mode surface checks;
- local-calendar/time-zone checks;
- backup/recovery process-kill checks;
- notifications;
- accessibility;
- release evidence.

Existing `docs/TEST_PLAN.md` remains the detailed formal test matrix and is linked from the new testing guide/hub.

---

## 74. Platform handbooks added

New Android guide:

`docs/platforms/ANDROID.md`

Documents:

- `net10.0-android`;
- minimum API 26;
- application ID/version;
- build command;
- manifest privacy flags;
- USE_BIOMETRIC/USE_FINGERPRINT/POST_NOTIFICATIONS;
- no background location;
- backup/device-transfer exclusions;
- notification and `NoCreate` cancellation QA;
- biometric provider-text redaction;
- `FLAG_SECURE` boundary;
- app-private storage;
- picker/share/receipts;
- adaptive UI;
- TalkBack/privacy/store QA.

New Windows guide:

`docs/platforms/WINDOWS.md`

Documents:

- `net10.0-windows10.0.19041.0`;
- package identity/version metadata;
- current publisher source value;
- build command;
- Windows Hello;
- scheduled toasts;
- display-affinity boundary;
- file/share/export;
- app-private storage;
- NTFS/reparse safety;
- adaptive desktop UI;
- Narrator/keyboard/high-DPI/privacy/package release QA.

New Apple guide:

`docs/platforms/APPLE.md`

Documents:

- `net10.0-ios`;
- `net10.0-maccatalyst`;
- minimum 15.0 source settings;
- build/archive prerequisites;
- iOS/Mac plist metadata;
- Face ID/biometric purpose strings;
- LocalAuthentication;
- UserNotifications;
- file/share flows;
- app-private storage;
- restore recovery;
- iOS orientation/iPad behavior;
- Catalyst desktop behavior;
- VoiceOver/Dynamic Type/keyboard;
- screen-capture limitation;
- privacy/local-calendar/store QA.

---

## 75. Accessibility and localization manual added

New file:

`docs/accessibility/ACCESSIBILITY_AND_LOCALIZATION.md`

Documents:

- accessibility goals;
- chart text/table equivalence;
- privacy-safe screen-reader behavior;
- secret-entry accessibility;
- keyboard/focus;
- touch targets;
- reduced motion;
- theme/contrast;
- adaptive layout;
- current localization architecture;
- English-first/localization-ready boundary;
- initial Hindi common-string resource structure;
- locale vs currency;
- runtime culture;
- number/date preview;
- parsing/local-calendar behavior;
- string extraction guidelines;
- RTL-readiness caveat;
- platform accessibility QA;
- localization completion definition.

The project still does not claim complete Hindi screen-by-screen localization.

---

## 76. Versioning, migration and store documentation added

New file:

`docs/releases/VERSIONING_AND_MIGRATIONS.md`

Documents coordination of:

- app display version 0.2.0;
- build version 2;
- Windows package 0.2.0.0;
- DB schema 2;
- backup magic FINORA01;
- migration rules;
- future schema-v3 workflow;
- synthetic migration fixtures;
- database schema vs backup compatibility;
- semantic-version intent;
- release tags;
- upgrade/downgrade/rollback behavior;
- release evidence.

New file:

`docs/releases/STORE_METADATA_TEMPLATE.md`

Provides a store-preparation template for:

- canonical product identity;
- short/long description drafts;
- privacy highlights;
- prohibited/unverified claims;
- feature bullets;
- synthetic screenshot rules;
- Android/Apple/Mac/Windows store preparation;
- release notes/review notes;
- contacts.

The template explicitly states that live store policies, forms, SDK requirements, signing rules, fees, and current declarations must be verified at submission time rather than assumed from static documentation.

---

## 77. Documentation-aware structural preflight added

`build/scripts/verify_structure.py` was expanded again during the documentation pass.

It now explicitly requires the complete core documentation tree, including:

- docs index/status/user guide;
- accessibility guide;
- architecture/schema/service/data-flow/navigation docs;
- feature/Settings docs;
- security/privacy docs;
- operations docs;
- setup/troubleshooting;
- developer docs;
- testing/native docs;
- platform docs;
- release/store/versioning docs.

It also validates repository-relative Markdown file links without network access.

The Markdown link check:

- skips external HTTP/HTTPS/mailto/tel/data targets;
- skips pure section anchors;
- strips query/fragment for repository-file checks;
- rejects repository-root escape;
- reports missing relative targets.

It intentionally does **not** claim to validate:

- external URLs;
- remote store-policy pages;
- Markdown anchor existence;
- C# compilation;
- native platform behavior.

Existing structural checks for money/privacy/XAML/project/version/schema/Android policy remain.

---

## 78. Build guide aligned to documentation and preflight

`docs/setup/BUILD.md` now additionally documents:

- the documentation index;
- complete documentation/preflight checks;
- repository-relative Markdown link validation behavior;
- Android/Windows/Apple platform docs;
- native validation matrix;
- versioning/migration policy;
- backup/recovery guide;
- diagnostics/integrity guide;
- testing guide;
- store metadata template.

It continues to separate structural/core verification from native target build evidence.

---

## 79. Changelog and project status aligned

`CHANGELOG.md` now records the complete documentation pass under Unreleased, including:

- documentation index/status;
- user guide;
- feature manuals;
- architecture docs;
- security/operations docs;
- developer/testing docs;
- platform guides;
- versioning/store template;
- accessibility/localization;
- documentation-aware preflight;
- build-guide alignment.

`PROJECT_STATUS.md` now includes an explicit Documentation section marking source documentation coverage while preserving external validation caveats.

It also records that structural preflight now requires the core documentation tree and local Markdown links.

---

## 80. Documentation pass exact pre-ledger changed-file inventory

Compared with documentation-pass base `180aa293526eadfc4ad700017266f357ce22ede2`, the pre-ledger `main` was 33 commits ahead and changed/added the following files:

### Existing files modified

- `CHANGELOG.md`;
- `PROJECT_STATUS.md`;
- `build/scripts/verify_structure.py`;
- `docs/setup/BUILD.md`.

### New documentation files

- `docs/README.md`;
- `docs/DOCUMENTATION_STATUS.md`;
- `docs/USER_GUIDE.md`;
- `docs/accessibility/ACCESSIBILITY_AND_LOCALIZATION.md`;
- `docs/architecture/DATA_FLOW.md`;
- `docs/architecture/NAVIGATION_AND_UI.md`;
- `docs/architecture/SERVICE_CATALOG.md`;
- `docs/development/ADDING_A_FEATURE.md`;
- `docs/development/CODE_MAP.md`;
- `docs/development/DEVELOPER_GUIDE.md`;
- `docs/features/ACCOUNTS_AND_TRANSACTIONS.md`;
- `docs/features/BUDGETS_GOALS_RECURRING.md`;
- `docs/features/REPORTS_IMPORT_EXPORT.md`;
- `docs/features/SETTINGS_REFERENCE.md`;
- `docs/operations/DATA_RESET_AND_SAMPLE_DATA.md`;
- `docs/operations/DIAGNOSTICS_AND_INTEGRITY.md`;
- `docs/platforms/ANDROID.md`;
- `docs/platforms/APPLE.md`;
- `docs/platforms/WINDOWS.md`;
- `docs/releases/STORE_METADATA_TEMPLATE.md`;
- `docs/releases/VERSIONING_AND_MIGRATIONS.md`;
- `docs/security/APP_LOCK_AND_PRIVACY.md`;
- `docs/security/BACKUP_AND_RECOVERY.md`;
- `docs/testing/NATIVE_VALIDATION_MATRIX.md`;
- `docs/testing/TESTING_GUIDE.md`.

This is **25 newly added documentation files** in the documentation pass, plus four existing files aligned before the final ledger write.

The final `what_changed.md` update is intentionally the final content commit after that pre-ledger inventory.

---

## 81. Representative documentation commit trail

The documentation pass used many focused commits rather than one monolithic change.

Representative messages include:

- `docs: add complete documentation index`
- `docs(user): add complete Finora user guide`
- `docs(features): document accounts transactions and reconciliation`
- `docs(features): document budgets goals and recurring workflows`
- `docs(features): document reports import and export`
- `docs(architecture): add service catalog`
- `docs(architecture): document finance data flows`
- `docs(architecture): document adaptive navigation and UI contracts`
- `docs(security): document app lock privacy and screen protection`
- `docs(security): document encrypted backup and crash recovery`
- `docs(operations): document diagnostics and integrity tools`
- `docs(operations): document finance reset and sample data`
- `docs(development): add contributor developer guide`
- `docs(development): add repository code map`
- `docs(development): document safe feature change workflow`
- `docs(testing): add practical testing guide`
- `docs(testing): add native validation matrix`
- `docs(platform): add Android engineering and QA guide`
- `docs(platform): add Windows engineering and QA guide`
- `docs(platform): add iOS and Mac Catalyst guide`
- `docs(release): document versioning and migration policy`
- `docs(release): add store metadata template`
- `ci(preflight): require documentation tree and validate local links`
- `docs(setup): align build guide with documentation preflight`
- `docs: add documentation completeness matrix`
- `docs: link documentation completeness matrix`
- `docs(features): add settings reference`
- `docs(accessibility): document accessibility and localization boundaries`
- `docs: index settings and accessibility references`
- `docs: complete documentation coverage matrix`
- `ci(preflight): require every core documentation reference`
- `docs(changelog): record complete documentation suite`
- `docs(status): record complete documentation coverage`
- `docs(status): finalize complete Finora project documentation ledger`

The Git history on `main` remains the authoritative exact ordered commit list.

---

## 82. Git commit email handling for the documentation pass

The requested commit email remains:

`sanskarin@outlook.in`

No connector write exposed an author/committer email override field, and there was no connector commit failure that could be solved by supplying that address through the available schema.

Therefore this ledger continues to state the limitation truthfully rather than claiming the connector authored commits with that email.

For local Git work, the documented configuration remains:

```bash
git config user.email "sanskarin@outlook.in"
```

---

## 83. Documentation validation boundary

The documentation pass used repository source/contracts/manifests/plists and the project's existing approved product boundary as its basis.

No live store-policy or current external-web verification was performed because web search is unavailable in this environment.

Accordingly:

- static store metadata docs are templates, not live policy advice;
- platform guides document current source + required QA, not proven native behavior;
- external URLs are not claimed verified by the local Markdown link checker;
- structural preflight source exists but was not locally executed from a complete checked-out repository in this connector-only workflow;
- no `.NET` restore/build/test/native compile pass is claimed from this documentation continuation;
- no Android/iOS/Windows/Mac Catalyst signing/device/store validation is claimed.

The documentation repeatedly separates implemented source from required external evidence.

---

## 84. Final documentation state

The current repository now has a structured documentation system covering the complete current Finora project from the perspectives needed for ongoing development and release work:

- end user;
- contributor/developer;
- architecture maintainer;
- database/migration maintainer;
- security/privacy reviewer;
- backup/recovery reviewer;
- operations/support engineer;
- tester/QA engineer;
- Android engineer;
- Windows engineer;
- iOS/Mac Catalyst engineer;
- accessibility/localization reviewer;
- release/store preparer.

The documentation does not erase or weaken the previously documented financial, privacy, backup, native-validation, migration, and later-version boundaries.

`docs/README.md` is the documentation entry point.

`docs/DOCUMENTATION_STATUS.md` is the documentation completeness/update-policy matrix.

`what_changed.md` remains the cumulative detailed project ledger.

No repository content should be changed after this final ledger commit in this continuation; subsequent activity should begin a new continuation and update the ledger last again.

---

## 85. Buy Me a Coffee and next-steps continuation — 2026-08-12

This continuation began from final documentation head:

`63dbb9e13fcfcd80bd1d75515bcd69012d9090a2`

Commit message:

`docs(status): finalize complete Finora project documentation ledger`

The continuation had two explicit goals:

1. add the canonical Buy Me a Coffee link `https://buymeacoffee.com/sanskarIN` to Finora's project identity and appropriate public/in-app documentation surfaces;
2. create a concrete, prioritized next-step execution roadmap instead of leaving future work as a vague feature list.

Before this final ledger write, `main` was **18 focused commits ahead** of that starting head.

The final ledger write is the nineteenth continuation commit.

---

## 86. Canonical Buy Me a Coffee source identity added

`src/Finora.Shared/AppConstants.cs` now contains:

`BuyMeACoffeeUrl = "https://buymeacoffee.com/sanskarIN"`

The shared constant is the source-level canonical URL for the project's optional external support page.

It sits alongside the existing canonical product identity values:

- product name;
- repository URL;
- creator profile URL;
- business/security email;
- support email;
- watermark/attribution.

The support URL is not stored in finance data, preferences, database records, encrypted backups, or premium entitlement state.

---

## 87. Settings/About Buy Me a Coffee action added

`src/Finora.App/Pages/SettingsPage.About.cs` now includes `OnBuyMeACoffeeClicked`.

Behavior:

- obtains the URL from `Finora.Shared.AppConstants.BuyMeACoffeeUrl`;
- opens the page using the system `Launcher`;
- if the system cannot open it, shows generic user-facing text;
- if an exception occurs, routes only a privacy-safe event through `IPrivacyLogger` using `Settings.BuyMeACoffeeOpenFailed`;
- does not expose raw browser/platform exception text to the user.

`src/Finora.App/Pages/SettingsPage.xaml` now exposes an accessible About button:

**Support development · Buy Me a Coffee**

The About card also explicitly states:

- support is optional;
- it does not unlock Finora features;
- it does not replace store entitlement validation.

This separation is intentional because Finora's current local premium flag is development/demo state, not secure commercial entitlement.

---

## 88. Buy Me a Coffee UI source-contract coverage added

`tests/Finora.UiTests/SettingsSourceContractTests.cs` now guards:

- visible Buy Me a Coffee About text;
- the `OnBuyMeACoffeeClicked` XAML handler;
- use of `AppConstants.BuyMeACoffeeUrl` in the About partial;
- privacy-safe failure event `Settings.BuyMeACoffeeOpenFailed`.

The existing Settings source contract remains responsible for the rest of the About/security/reset identity surface.

As with all UI-contract tests in this repository, source presence is not represented as a native-device execution pass in this environment.

---

## 89. Prioritized next-step roadmap added

New file:

`docs/NEXT_STEPS.md`

The roadmap is intentionally ordered by release/data risk rather than novelty.

### P0 — Release blockers

P0 contains the work that should be completed before representing Finora 0.2.0 as store-ready, including:

- structural preflight;
- exact SDK/workload/dependency restore;
- Release unit/integration/UI-contract tests;
- Android/Windows/iOS/Mac Catalyst builds;
- compiler/analyzer/XAML warning/error resolution;
- migration validation;
- encrypted backup/restore plus interruption recovery;
- data-integrity validation;
- privacy-mode screen-by-screen validation;
- 0/2/3/4-decimal currency precision QA where supported;
- multiple time-zone and DST local-calendar validation;
- native notification lifecycle validation;
- PIN/biometric fallback validation;
- receipt/filesystem confinement validation;
- accessibility validation;
- complete local finance-data deletion validation.

### P1 — Release-candidate completion

P1 covers release packaging/evidence after P0 correctness is proven:

- signed artifacts outside source control;
- final package IDs/publisher/provisioning;
- synthetic screenshots/store assets;
- final privacy/data-safety declarations;
- target-store review of the Buy Me a Coffee external support link;
- canonical public contact-link review;
- exact dependency-license/vulnerability review;
- release-candidate tagging only after evidence exists;
- release notes and known limitations.

### P2 — Quality and product polish

P2 includes improvements that should follow a proven release candidate rather than displacing correctness work:

- true database-level transaction paging if benchmarks justify it;
- large-dataset performance benchmarks;
- fuller localization, including Hindi completion work;
- native UI automation;
- continuing accessibility improvements;
- richer privacy-safe import diagnostics;
- expanded export configuration;
- backup usability improvements without password weakening;
- richer deterministic sample datasets;
- contributor workflow improvements.

### P3 — Later-version architecture

P3 records intentionally non-current product areas that require new architecture/privacy/security/migration decisions:

- remote Finora accounts;
- cloud synchronization;
- shared/collaborative finance spaces;
- server/store-backed commercial entitlement;
- explicit foreign-exchange workflow;
- optional remote exchange-rate lookup;
- analytics/crash telemetry decisions.

The roadmap explicitly preserves the local-first boundary and forbids silently inventing FX rates or turning Buy Me a Coffee into hidden entitlement state.

---

## 90. Recommended next milestone defined

The roadmap defines the strongest next milestone as:

> **A fully reproducible Finora 0.2.0 release candidate that restores, builds, tests, migrates, backs up/restores, protects private finance displays, passes native platform validation, and has evidence for every applicable release checklist item.**

This means the recommended next action is not simply “add more features.”

The project should first establish evidence for:

1. structural verification;
2. dependency/workload restore;
3. compiler/XAML/analyzer correctness;
4. all automated test suites;
5. schema migration;
6. backup/restore and crash recovery;
7. privacy/security/integrity;
8. currency/local-date correctness;
9. notifications/app lock/biometrics;
10. accessibility;
11. complete data deletion;
12. native packaging/signing;
13. store-policy and dependency/license review.

Only after the P0/P1 milestone is satisfied should P2/P3 work become the primary focus.

---

## 91. Public documentation updated for project support and roadmap

The following public/support documentation now includes the canonical Buy Me a Coffee URL and/or roadmap where relevant:

- `README.md`;
- `SUPPORT.md`;
- `docs/README.md`;
- `docs/DOCUMENTATION_STATUS.md`;
- `docs/USER_GUIDE.md`;
- `docs/features/SETTINGS_REFERENCE.md`;
- `docs/releases/STORE_METADATA_TEMPLATE.md`;
- `docs/releases/RELEASE_CHECKLIST.md`;
- `docs/releases/STORE_READINESS.md`;
- `docs/setup/BUILD.md`;
- `PROJECT_STATUS.md`;
- `CHANGELOG.md`;
- `what_changed.md`.

The public README now includes:

- Buy Me a Coffee in About/current contacts;
- the explicit entitlement separation;
- a dedicated Next Steps section;
- a link to `docs/NEXT_STEPS.md`;
- the recommended release-candidate milestone.

The documentation hub now treats `docs/NEXT_STEPS.md` as a Start Here document.

The documentation status matrix now tracks the roadmap as a current required area.

---

## 92. Support documentation boundary strengthened

`SUPPORT.md` now lists:

- user support email;
- business/security email;
- repository;
- creator profile;
- optional Buy Me a Coffee project-support URL.

It explicitly states that a contribution:

- does not unlock features;
- does not create premium entitlement;
- does not guarantee or accelerate support;
- does not change security-reporting priority;
- does not create a service-level agreement.

This preserves support access independently from project contributions.

---

## 93. Settings reference aligned

`docs/features/SETTINGS_REFERENCE.md` now documents the About support action and its implementation boundary.

It records that:

- Buy Me a Coffee is opened through the system launcher;
- open failures use generic user-facing text/privacy-safe logging;
- it is not a Finora setting;
- it is not an entitlement/subscription/premium flag;
- it does not change support priority;
- current target-store policy must be reviewed before keeping the link in a packaged store build.

The Settings reference now also links to `docs/NEXT_STEPS.md`.

---

## 94. Store metadata and external-link policy gates added

`docs/releases/STORE_METADATA_TEMPLATE.md` now includes Buy Me a Coffee in canonical project identity and adds a dedicated external-support boundary.

The template explicitly prohibits describing Buy Me a Coffee as:

- an in-app purchase;
- subscription;
- premium entitlement;
- feature unlock;
- required support payment;
- guaranteed faster support;
- secure license token.

Android, Apple, Mac and Windows store-preparation sections now require live policy review when the link is included in the target distribution.

The static template is still not represented as current store-policy advice; final store-console rules must be verified at submission time.

---

## 95. Release checklist and store-readiness gates expanded

`docs/releases/RELEASE_CHECKLIST.md` now requires evidence that:

- the canonical URL is `https://buymeacoffee.com/sanskarIN`;
- the About action uses the shared URL;
- open failures remain privacy-safe;
- Buy Me a Coffee is not represented as entitlement/subscription/feature unlock/support-priority purchase;
- the current target-store external contribution/payment-link policy has been reviewed before retaining it in the packaged build;
- unresolved P0 items in `docs/NEXT_STEPS.md` close or block release.

`docs/releases/STORE_READINESS.md` now includes the same concept in common/platform/store-evidence gates.

Platform sections specifically require policy review for:

- Google Play distribution;
- Microsoft Store/package distribution where applicable;
- Apple App Store distribution;
- Mac Catalyst distribution channel.

No store policy is marked passed merely because the documentation contains the link.

---

## 96. Structural preflight now protects roadmap and support identity

`build/scripts/verify_structure.py` now:

- requires `docs/NEXT_STEPS.md` as part of the core documentation tree;
- defines the expected canonical `BUY_ME_A_COFFEE_URL`;
- validates `AppConstants.BuyMeACoffeeUrl` contains that exact URL;
- validates Settings/About still exposes `OnBuyMeACoffeeClicked` and Buy Me a Coffee text;
- validates Settings keeps explicit “does not unlock Finora features” wording;
- validates `SettingsPage.About.cs` uses the shared `AppConstants.BuyMeACoffeeUrl`;
- validates the docs index and roadmap retain the canonical external URL;
- continues all previous documentation-link, XAML, project, version/schema, money/privacy, secret-entry, reset, biometric, and Android backup/privacy checks.

The preflight does **not** perform a network request to Buy Me a Coffee and does not claim that the external page or store policy is currently valid/reachable.

---

## 97. Build guide aligned again

`docs/setup/BUILD.md` now documents that structural preflight also checks:

- the roadmap exists;
- the canonical Buy Me a Coffee source identity;
- Settings/About shared-constant/handler wiring;
- the no-feature-unlock boundary.

It also links `docs/NEXT_STEPS.md` in release preparation and requires store-policy review if the external support link is packaged.

The build guide continues to distinguish static structural verification from compiler/test/native/store evidence.

---

## 98. Project status and changelog aligned

`PROJECT_STATUS.md` now has a 2026-08-12 source-review date and records:

- Buy Me a Coffee About source and UI-contract coverage;
- the entitlement/support-priority boundary;
- store-policy validation as still external;
- the P0–P3 roadmap;
- the recommended release-candidate milestone;
- the support-link policy review as an explicit release gate.

`CHANGELOG.md` now records the support-link/roadmap continuation under Unreleased.

---

## 99. Exact pre-ledger changed-file inventory — 2026-08-12

Compared with continuation base:

`63dbb9e13fcfcd80bd1d75515bcd69012d9090a2`

pre-ledger `main` was **18 commits ahead** and changed the following files:

### Source

- `src/Finora.Shared/AppConstants.cs`;
- `src/Finora.App/Pages/SettingsPage.About.cs`;
- `src/Finora.App/Pages/SettingsPage.xaml`.

### Tests

- `tests/Finora.UiTests/SettingsSourceContractTests.cs`.

### Build/preflight

- `build/scripts/verify_structure.py`.

### New roadmap

- `docs/NEXT_STEPS.md`.

### Public/support/project docs

- `README.md`;
- `SUPPORT.md`;
- `PROJECT_STATUS.md`;
- `CHANGELOG.md`.

### Documentation system

- `docs/README.md`;
- `docs/DOCUMENTATION_STATUS.md`;
- `docs/USER_GUIDE.md`;
- `docs/features/SETTINGS_REFERENCE.md`;
- `docs/setup/BUILD.md`.

### Release/store docs

- `docs/releases/RELEASE_CHECKLIST.md`;
- `docs/releases/STORE_METADATA_TEMPLATE.md`;
- `docs/releases/STORE_READINESS.md`.

The final `what_changed.md` update is intentionally the final content commit after this inventory.

---

## 100. Focused commit trail — 2026-08-12

The pre-ledger continuation commit trail includes:

- `feat(identity): add Buy Me a Coffee project support link`
- `feat(settings): add Buy Me a Coffee support action`
- `feat(settings): expose Buy Me a Coffee in About`
- `test(ui): guard Buy Me a Coffee About support link`
- `docs(roadmap): add prioritized Finora next steps`
- `docs(index): link Buy Me a Coffee and next-steps roadmap`
- `docs(settings): document Buy Me a Coffee support boundary`
- `docs(support): add optional Buy Me a Coffee support link`
- `docs(store): add Buy Me a Coffee policy boundary and roadmap`
- `docs(status): track roadmap and Buy Me a Coffee coverage`
- `ci(preflight): require roadmap and Buy Me a Coffee identity`
- `docs(readme): add Buy Me a Coffee and prioritized next steps`
- `docs(status): add roadmap milestone and Buy Me a Coffee boundary`
- `docs(changelog): record Buy Me a Coffee and next-step roadmap`
- `docs(user): add Buy Me a Coffee and next-step guidance`
- `docs(release): gate Buy Me a Coffee and next-step release blockers`
- `docs(store): gate external support link and P0 roadmap evidence`
- `docs(build): include roadmap and support-link preflight checks`
- final ledger commit updating this file.

The Git history remains the authoritative exact ordered commit record.

---

## 101. Buy Me a Coffee is deliberately not a Finora entitlement system

The repository now consistently treats Buy Me a Coffee as an optional external project-support destination.

It is **not**:

- a Finora account;
- a login mechanism;
- a subscription;
- an in-app purchase implementation;
- a premium entitlement token;
- a server-backed license;
- a replacement for store purchase APIs;
- a guarantee of support priority or response time;
- a finance-data/payment feature inside Finora.

A future commercial/premium model remains a separate later-version architecture problem requiring store/server-backed entitlement decisions.

If a future distribution channel prohibits or constrains external contribution links, the packaged UI must follow that channel's current rules without changing the documented entitlement truth.

---

## 102. Current external-policy validation boundary

This continuation did not perform a live current-policy determination for Google Play, Apple App Store, Microsoft Store, Mac distribution channels, or Buy Me a Coffee service availability.

The repository therefore does not claim:

- that every store currently permits the external support link;
- that the external page is guaranteed reachable in every region;
- that a contribution can be processed from every platform/region;
- that static documentation supersedes current store-console rules.

The release checklist, store readiness, store metadata template, roadmap, README, Settings reference, user guide, and support docs all require live store-policy review before public packaged submission when the link is included.

---

## 103. Validation status after this continuation

The same evidence rules remain in force.

This connector-only continuation changed source/tests/docs on `main`, but no local .NET/MAUI toolchain was available in the execution environment.

Therefore no claim is made here that:

- structural preflight actually executed on a complete checkout;
- `dotnet restore` succeeded;
- unit/integration/UI-contract tests executed successfully;
- Android/Windows/iOS/Mac Catalyst builds succeeded;
- native Launcher opening of Buy Me a Coffee was device-tested;
- native accessibility was tested;
- store external-link policy was approved;
- signing/package/store submission succeeded.

Those are precisely the next P0/P1 evidence tasks documented in `docs/NEXT_STEPS.md`.

---

## 104. Final current project direction

The current project direction is now explicit:

### Maintain current local-first core

Preserve:

- finance correctness;
- signed integer minor-unit money;
- explicit currency scope;
- no invented FX;
- local calendar correctness;
- schema/migration safety;
- crash-safe encrypted backup/restore;
- privacy-safe diagnostics;
- complete local finance-data deletion;
- optional local app lock;
- explicit user-controlled import/export/backup;
- no mandatory login/cloud dependency.

### Complete release evidence before large expansion

Prioritize P0/P1 from `docs/NEXT_STEPS.md`.

### Treat project support separately from product entitlement

Canonical optional support destination:

https://buymeacoffee.com/sanskarIN

This remains separate from Finora finance data, functionality, and licensing.

### Defer architecture-heavy features until their design is approved

Remote accounts, cloud sync, collaboration, secure commercial entitlement, explicit FX/network rates, and telemetry remain P3/later-version decisions.

`docs/NEXT_STEPS.md` is now the primary roadmap for the next workstream.

`docs/README.md` remains the documentation hub.

`docs/DOCUMENTATION_STATUS.md` remains the documentation coverage/update-policy matrix.

`what_changed.md` remains the cumulative detailed project ledger and is intentionally the final content write of this continuation.

---

## 105. Cross-platform build stabilization continuation — 2026-08-14 to 2026-08-15

This continuation resumed from a large sequence of MAUI/platform/XAML build-hardening commits on `main` and converted the previous source-only validation uncertainty into concrete GitHub Actions evidence.

The work intentionally followed actual CI diagnostics rather than weakening analyzers or broadly suppressing failures.

Primary objectives completed in this continuation:

- expose every native platform failure independently;
- fix Android platform-version analyzer issues;
- fix Apple analyzer/platform entry-point conflict;
- remove the EF Core linker failure with a matching servicing-line dependency update;
- separate Windows source compilation from MSIX packaging-toolchain validation;
- run and retain exact automated test evidence;
- run all four MAUI Release source-build targets;
- run CodeQL;
- audit successful native logs rather than trusting only green job status;
- eliminate the large XAML compiled-binding warning set;
- make the relevant XAML binding diagnostics fatal so the warning class cannot silently regress;
- update stale GitHub Actions runtime majors;
- add a permanent CI evidence document;
- align project status, roadmap, changelog, testing guide, documentation index/status, and this cumulative ledger.

---

## 106. Independent native CI topology

The earlier native workflow layout allowed one target failure to hide another target's diagnostics because Windows preceded Android and iOS preceded Mac Catalyst in paired jobs.

The workflow was changed so these targets execute as independent jobs:

- MAUI Windows;
- MAUI Android;
- MAUI iOS;
- MAUI Mac Catalyst.

Each job retains its own diagnostic artifact/log.

This was a diagnostic correctness change: a failure on one target no longer cancels a different target before its compile/link error can be observed.

---

## 107. Android API-level analyzer fixes

Two source-level Android issues were exposed by CodeQL/platform analysis and fixed in focused commits.

### Biometric APIs

`src/Finora.App/PlatformBiometricService.cs` now uses a runtime-recognized:

`OperatingSystem.IsAndroidVersionAtLeast(28)`

guard around Android API 28+ biometric APIs.

The activity main executor is also explicitly validated rather than flowing a possibly-null executor into the platform builder.

Representative commit:

`fix(android): guard biometric APIs by platform version`

### Notification permission

The Android notification permission provider now uses:

`OperatingSystem.IsAndroidVersionAtLeast(33)`

before referencing the Android 13 `POST_NOTIFICATIONS` permission.

Representative commit:

`fix(android): guard notification permission by API level`

These changes allow the platform compatibility analyzer to prove the guards instead of relying on checks it did not recognize.

---

## 108. Apple AppDelegate analyzer conflict fixed narrowly

Mac Catalyst exposed `CA1711` on the required Apple entry-point class name `AppDelegate`.

The conflict was handled at the required Apple entry-point boundary rather than disabling naming analyzers for the project.

This keeps analyzer coverage intact for ordinary application code while permitting the platform-mandated delegate name.

---

## 109. EF Core native linker failure fixed with servicing packages

The iOS/native linker failure was traced to `IL2037` emitted from Microsoft.EntityFrameworkCore metadata around `ExecuteUpdateAsync` rather than to Finora's own JSON/reflection code.

The centrally pinned EF Core packages were moved from 10.0.0 to 10.0.10:

- `Microsoft.EntityFrameworkCore` 10.0.10;
- `Microsoft.EntityFrameworkCore.Sqlite` 10.0.10.

This aligned the application with the current .NET 10 servicing toolchain used by the CI runners and cleared the observed linker failure across the native builds.

Unrelated package versions were not changed as part of that fix.

---

## 110. Windows source validation separated from MSIX packaging

The Windows failure was isolated to the Windows App SDK manifest/package generation task loading `System.Security.Permissions`, not to Finora C# compilation.

The CI Windows source-build gate now uses:

`WindowsPackageType=None`

This lets CI validate the Windows MAUI application source, analyzers, XAML, and references independently from Microsoft Store/MSIX identity/signing/toolchain work.

This does **not** mark MSIX packaging complete.

Still required separately:

- final Windows package identity;
- publisher configuration;
- MSIX generation;
- signing;
- packaged toast/Hello/file behavior;
- Store validation.

Representative commit:

`fa5055b…` — Windows compile-only CI correction.

---

## 111. Exact automated test evidence established

GitHub Actions retained exact test evidence for strict source candidate:

`f7dbfbb8691edc79cee559101f284ccd90a44cf7`

Finora CI run:

`31872362394`

Results:

- Unit: **97/97 passed**;
- Integration: **109/109 passed**;
- UI-contract: **35/35 passed**;
- Total: **241/241 passed**;
- Failures: **0**.

Structural preflight also passed on that same candidate.

This supersedes the older Aug 12 source-only statements in sections 3, 60, 61, and 103 for the newer Aug 15 evidence point. Those historical sections remain unchanged because they accurately recorded the validation state at the time they were written.

---

## 112. Four-target MAUI Release source-build evidence established

The same strict candidate passed all four independent native Release source-build jobs:

- Windows job `94983017634` — success;
- Android job `94983017627` — success;
- iOS job `94983017606` — success;
- Mac Catalyst job `94983017649` — success.

The iOS job completed its actual `Build iOS with analyzers/warnings as errors` step successfully on the GitHub macOS runner.

This proves source compilation for the tested target frameworks and configuration. It does not prove signed packages, physical-device behavior, app-store submission, accessibility, or OS-level notification/biometric behavior.

---

## 113. CodeQL evidence established

CodeQL run:

`31872362398`

completed successfully for strict source candidate:

`f7dbfbb8691edc79cee559101f284ccd90a44cf7`.

Its Android analysis build completed successfully before the CodeQL analysis step closed green.

The earlier Android platform-version findings were no longer the blocking source failures after the focused guard fixes.

---

## 114. Native log audit exposed 1,152 XAML compiled-binding warnings

Successful native jobs were not treated as the end of quality validation.

The native build logs were inspected and exposed approximately **1,152 `XC0022` warnings per native build**, concentrated across 16 application XAML pages.

The warnings indicated bindings that could not be compiled because binding-context/template types were not explicit enough.

The warning set was fixed instead of hidden with `NoWarn`, `x:Object`, or `x:Null` shortcuts.

---

## 115. Typed XAML binding migration completed

The affected XAML surfaces were updated with real compile-time binding contracts, including:

- page-level `x:DataType` declarations;
- typed `DataTemplate` declarations;
- typed picker/item display bindings;
- nested collection item types where required.

The work was intentionally split across many focused page-level commits.

Known focused commits from the pass include:

- Dashboard: `e019162a41ff02c0aaec3aa6740caea962d9b451`;
- Reports: `a3549edd20aa1aee980057a9c6fcbbd7f64375bc`;
- Transaction Detail: `9fa36787e8b5684a59626396b59b9cf203fe8e03`;
- Recurring: `ffdf15ee2bb6a333ba9b4435d0287cfd21ab9034`.

Other affected finance/settings/onboarding/list pages were migrated in separate commits as part of the same pass.

---

## 116. XAML warning classes are now build-breaking regressions

After the binding migration, `src/Finora.App/Finora.App.csproj` was changed so these diagnostics are errors:

- `XC0022`;
- `XC0023`;
- `XC0025`.

Strict enforcement commit:

`f7dbfbb8691edc79cee559101f284ccd90a44cf7`

Commit message:

`build(xaml): enforce compiled binding diagnostics`

The full four-target successful native run occurred on this exact strict candidate, so the migration is validated under the policy that makes those warning classes fatal.

---

## 117. GitHub Actions Node runtime maintenance

The strict core test log exposed GitHub-hosted warnings that several action majors in `.github/workflows/ci.yml` still targeted the deprecated Node 20 runtime and were being forced to Node 24.

The primary Finora CI workflow was updated to:

- `actions/checkout@v7`;
- `actions/setup-python@v7`;
- `actions/setup-dotnet@v6`;
- `actions/upload-artifact@v7`.

CI-only commit:

`6ba519bf69174c68b67f8595872546a259c783dc`

Commit message:

`ci: update actions to Node 24 runtimes`

CodeQL and dependency-review workflows were already using current compatible majors and did not require the same migration.

The follow-up run `31873092936` successfully executed the updated checkout/Python structural path and progressed through updated .NET setup/restores/test execution before the intentional documentation commit sequence superseded the run through CI concurrency.

---

## 118. CI evidence documentation added

New file:

`docs/testing/CI_EVIDENCE.md`

records:

- exact strict source candidate SHA;
- Finora CI and CodeQL run IDs;
- exact 241-test result;
- native job IDs;
- Windows unpackaged source-build boundary;
- strict XAML diagnostic policy;
- CI runtime-maintenance commit/run;
- what the evidence proves;
- what remains external.

The evidence document is linked from:

- `docs/README.md`;
- `docs/testing/TESTING_GUIDE.md`;
- `docs/DOCUMENTATION_STATUS.md`.

Repository-relative Markdown link validation therefore protects the evidence link from silently becoming broken.

---

## 119. Status, roadmap, and changelog advanced

`PROJECT_STATUS.md` now has a 2026-08-15 review state and records actual automated evidence instead of leaving CI as entirely unconfirmed.

`docs/NEXT_STEPS.md` now treats structural preflight, the 241 tests, four-target Release source builds, CodeQL, and strict XAML diagnostics as completed automated regression gates for the recorded candidate.

Remaining P0/P1 priority is explicitly shifted to evidence that source CI cannot prove:

- migration/upgrade validation;
- backup/restore process-interruption recovery;
- integrity checks on migrated/restored datasets;
- native privacy-mode behavior;
- currency/time-zone/DST QA;
- native notification lifecycle;
- PIN/biometric/Windows Hello/capture behavior;
- attachment/file picker/share flows;
- accessibility;
- complete finance-data deletion;
- signed packaging;
- exact dependency/license/security review;
- current store-policy/privacy/data-safety review.

`CHANGELOG.md` records the cross-platform build, linker, XAML, CI-runtime, and evidence changes under Unreleased.

---

## 120. 2026-08-15 final continuation changed-file inventory

This continuation changed source/build/documentation areas including:

### Platform source

- `src/Finora.App/PlatformBiometricService.cs` — analyzer-recognized Android API guard and executor safety;
- Android notification-permission source — analyzer-recognized API 33 guard;
- Apple platform entry-point source — narrow `AppDelegate` analyzer handling.

### Dependency/build configuration

- `Directory.Packages.props` — EF Core/SQLite servicing update to 10.0.10;
- `src/Finora.App/Finora.App.csproj` — strict compiled-binding diagnostic enforcement;
- `.github/workflows/ci.yml` — independent native targets, Windows unpackaged source validation, current Node-24-compatible action majors.

### XAML

The binding-contract migration covered the affected application pages/templates, including Dashboard, Reports, Transaction Detail, Recurring, and the other warning-producing finance/settings/onboarding/list surfaces.

### Documentation/evidence

- `docs/testing/CI_EVIDENCE.md` — new exact evidence record;
- `docs/README.md` — evidence link;
- `docs/testing/TESTING_GUIDE.md` — evidence policy/link;
- `PROJECT_STATUS.md` — verified automated status;
- `docs/NEXT_STEPS.md` — advanced automated gates and remaining P0/P1 work;
- `CHANGELOG.md` — stabilization/evidence history;
- `docs/DOCUMENTATION_STATUS.md` — CI evidence coverage/update policy;
- `what_changed.md` — this cumulative final ledger update.

The exact Git history is authoritative for every focused page-level/build-fix commit and its ordering.

---

## 121. Current evidence-based release boundary after this continuation

Finora 0.2.0 now has concrete automated evidence for:

- structural preflight;
- exact current Unit/Integration/UI-contract execution;
- **241/241 passing automated tests**;
- Windows MAUI Release source compilation;
- Android MAUI Release source compilation;
- iOS MAUI Release source compilation;
- Mac Catalyst MAUI Release source compilation;
- strict XAML compiled-binding diagnostics;
- CodeQL.

This continuation still does **not** claim:

- a signed Android AAB production artifact;
- a generated/signed production Windows MSIX;
- Apple provisioning/signing/notarization/store archives;
- physical-device/emulator native behavior for notifications/biometrics/files/backup/capture;
- process-kill restore recovery evidence on target devices;
- TalkBack/VoiceOver/Narrator/keyboard/large-text/high-contrast validation;
- final dependency-license/vulnerability acceptance;
- current store approval or current external-support-link policy approval;
- complete absence of undiscovered defects.

Those remaining gates are preserved in `docs/NEXT_STEPS.md`, `docs/releases/RELEASE_CHECKLIST.md`, `docs/releases/STORE_READINESS.md`, and `docs/testing/NATIVE_VALIDATION_MATRIX.md`.

`docs/testing/CI_EVIDENCE.md` is now the dated automated proof record.

`what_changed.md` remains the cumulative detailed project ledger and this update is intentionally the final content write of the 2026-08-15 continuation.

---

## 122. Migration, backup, integrity and recovery hardening continuation — 2026-08-15

This continuation began from verified pre-hardening head:

`31269a99a49c43ffdceb696eac216acff452c339`

and produced final source candidate:

`f80b29d44a225a6d745529519e6c59cadbc152a8`

The source candidate is **34 focused commits ahead** of the base and contains production hardening plus targeted integration regression coverage for the P0 migration/backup/integrity/recovery workstream.

No broad analyzer suppression or release-gate weakening was used to obtain a green result.

---

## 123. Database migration runner hardened before schema-version advancement

`src/Finora.Infrastructure/DatabaseMigrationRunner.cs` was strengthened so a migration step does not merely execute DDL and then trust `CREATE TABLE IF NOT EXISTS` semantics.

Current migration behavior now additionally validates:

- the expected target table/column shape after the migration step;
- SQLite `PRAGMA foreign_key_check` state;
- SQLite integrity state;
- the target schema before the `schema.version` marker is advanced.

The version marker remains inside the same migration transaction, so malformed target state cannot be recorded as a successful schema upgrade.

This closes the observed safety gap where an already-existing but malformed schema-2 table could otherwise satisfy `CREATE TABLE IF NOT EXISTS` while lacking the expected shape.

---

## 124. Migration integration matrix expanded

New migration-focused integration files include:

- `tests/Finora.IntegrationTests/DatabaseInitializationTests.cs`;
- `tests/Finora.IntegrationTests/DatabaseMigrationDataPreservationTests.cs`;
- `tests/Finora.IntegrationTests/DatabaseMigrationForeignKeyTests.cs`;
- `tests/Finora.IntegrationTests/DatabaseMigrationRollbackTests.cs`;
- `tests/Finora.IntegrationTests/DatabaseMigrationVersionGuardTests.cs`.

The expanded suite proves:

- fresh database initialization succeeds;
- reopening a current database remains safe;
- invalid/future schema markers fail closed;
- current-schema execution is harmless;
- schema 1 → schema 2 preserves representative attachment metadata;
- intended filename backfill occurs;
- repeated migration execution is idempotent;
- malformed target schema causes rollback;
- a failed migration does not advance the schema marker;
- deliberately injected legacy foreign-key corruption is rejected.

The synthetic v1 fixture remains test data only and is not represented as a substitute for installing an actual prior released binary on each platform.

---

## 125. Encrypted backup hostile-input coverage expanded

New backup regression helpers/tests include:

- `BackupTestCipher.cs` — test-only authenticated backup rewriter;
- `BackupAttachmentFixture.cs` — reusable receipt-bearing synthetic fixture;
- `BackupCryptographicFailureTests.cs`;
- `BackupTruncationTests.cs`;
- `BackupSchemaCompatibilityTests.cs`;
- `BackupAuthenticatedGraphTamperTests.cs`;
- `BackupAttachmentPathTamperTests.cs`;
- `BackupAttachmentSizeTamperTests.cs`;
- `BackupAttachmentHashTamperTests.cs`;
- `BackupAttachmentMissingHashTests.cs`.

The test-only authenticated rewriter is important because it exercises semantically malicious/corrupt plaintext behind a valid AES-GCM envelope, not only random ciphertext damage.

Current automated cases include:

- wrong password;
- changed ciphertext/tag;
- truncation;
- authenticated unsupported/future schema;
- authenticated semantic relationship corruption;
- authenticated receipt path escape;
- authenticated receipt size drift;
- authenticated receipt SHA-256 drift;
- missing receipt checksum metadata.

---

## 126. Receipt checksum consistency defect fixed

`src/Finora.Infrastructure/BackupService.cs` previously treated receipt SHA-256 metadata as optional at backup validation time even though the integrity subsystem treats missing checksum metadata as invalid.

The portable backup boundary now requires a valid **32-byte SHA-256 checksum** for receipt metadata.

Creation, preview and restore therefore fail closed when receipt checksum metadata is absent or malformed instead of preserving/importing unverifiable attachment state.

This aligns:

- attachment metadata expectations;
- integrity diagnostics;
- backup creation validation;
- authenticated preview validation;
- restore validation.

---

## 127. Deliberate integrity-corruption regression matrix expanded

New direct integrity regression files include:

- `IntegritySplitTotalRegressionTests.cs`;
- `IntegrityCurrencyRegressionTests.cs`;
- `IntegrityMissingReceiptRegressionTests.cs`;
- `IntegrityReceiptSizeRegressionTests.cs`;
- `IntegrityReceiptHashRegressionTests.cs`;
- `IntegrityReceiptHashMetadataRegressionTests.cs`;
- `IntegrityCategoryCycleRegressionTests.cs`;
- `IntegrityForeignKeyRegressionTests.cs`.

These tests deliberately inject stored corruption and require `DataIntegrityService` to identify it without silently rewriting the underlying finance history.

Covered classes now directly include:

- split-total drift;
- transaction/account currency mismatch;
- missing receipt file;
- receipt size drift;
- changed receipt bytes/checksum drift;
- missing/invalid receipt hash metadata;
- category parent cycle;
- SQLite foreign-key violation.

This supplements existing transfer, budget, savings, recurrence, reconciliation, path-safety and privacy-safe integrity coverage.

---

## 128. Restore recovery linked-path regression coverage added

Two additional host-conditional recovery tests were added:

- `tests/Finora.IntegrationTests/RestoreRecoveryJournalLinkTests.cs`;
- `tests/Finora.IntegrationTests/RestoreRecoveryRollbackLinkTests.cs`.

The tests prove the recovery layer fails closed when:

- the recovery journal is replaced by a symbolic link/reparse-style link supported by the host;
- the verified rollback directory is a link.

The rollback-link case also proves the live receipt tree and recovery state are preserved rather than deleting trusted live data and following an unsafe external filesystem target.

These source tests complement, but do not replace, real process-kill/native-filesystem recovery injection.

---

## 129. Privacy logger rotation regression synchronized with completed writes

The enlarged integration pass exposed a race in the existing privacy logger rotation assertion.

The test could observe a newly created rotated/current file before the asynchronous append had completed.

`tests/Finora.IntegrationTests/PrivacyLoggerTests.cs` now synchronizes through the logger/export gate before asserting rotated/current file state.

This was a test-correctness fix; production privacy logging/redaction policy was not weakened.

---

## 130. Exact final source-candidate CI evidence

Final source candidate:

`f80b29d44a225a6d745529519e6c59cadbc152a8`

Finora CI run:

`31875164890`

CodeQL run:

`31875164864`

Finora CI completed successfully with:

- Structural preflight — job `94989697902` — success;
- Core tests — job `94989708606` — success;
- MAUI Windows — job `94989803961` — success;
- MAUI Android — job `94989803975` — success;
- MAUI iOS — job `94989804013` — success;
- MAUI Mac Catalyst — job `94989803934` — success.

Exact current automated result:

- Unit: **97/97 passed**;
- Integration: **141/141 passed**;
- UI-contract: **35/35 passed**;
- Total: **273/273 passed**;
- Failed: **0**;
- Skipped: **0**.

The core run used Release configuration and the repository warnings-as-errors policy.

CodeQL completed successfully on the same source candidate after restoring the app, building the Android analysis target and performing analysis.

The exact retained evidence is recorded in `docs/testing/CI_EVIDENCE.md`.

---

## 131. Exact 34-commit changed-file inventory for this hardening pass

Compared from `31269a99a49c43ffdceb696eac216acff452c339` to `f80b29d44a225a6d745529519e6c59cadbc152a8`, GitHub reports 34 commits and these changed files:

### Production source

- `src/Finora.Infrastructure/BackupService.cs`;
- `src/Finora.Infrastructure/DatabaseMigrationRunner.cs`.

### Existing regression source modified

- `tests/Finora.IntegrationTests/PrivacyLoggerTests.cs`.

### New backup regression infrastructure/tests

- `tests/Finora.IntegrationTests/BackupAttachmentFixture.cs`;
- `tests/Finora.IntegrationTests/BackupAttachmentHashTamperTests.cs`;
- `tests/Finora.IntegrationTests/BackupAttachmentMissingHashTests.cs`;
- `tests/Finora.IntegrationTests/BackupAttachmentPathTamperTests.cs`;
- `tests/Finora.IntegrationTests/BackupAttachmentSizeTamperTests.cs`;
- `tests/Finora.IntegrationTests/BackupAuthenticatedGraphTamperTests.cs`;
- `tests/Finora.IntegrationTests/BackupCryptographicFailureTests.cs`;
- `tests/Finora.IntegrationTests/BackupSchemaCompatibilityTests.cs`;
- `tests/Finora.IntegrationTests/BackupTestCipher.cs`;
- `tests/Finora.IntegrationTests/BackupTruncationTests.cs`.

### New migration regression tests

- `tests/Finora.IntegrationTests/DatabaseInitializationTests.cs`;
- `tests/Finora.IntegrationTests/DatabaseMigrationDataPreservationTests.cs`;
- `tests/Finora.IntegrationTests/DatabaseMigrationForeignKeyTests.cs`;
- `tests/Finora.IntegrationTests/DatabaseMigrationRollbackTests.cs`;
- `tests/Finora.IntegrationTests/DatabaseMigrationVersionGuardTests.cs`.

### New integrity regression tests

- `tests/Finora.IntegrationTests/IntegrityCategoryCycleRegressionTests.cs`;
- `tests/Finora.IntegrationTests/IntegrityCurrencyRegressionTests.cs`;
- `tests/Finora.IntegrationTests/IntegrityForeignKeyRegressionTests.cs`;
- `tests/Finora.IntegrationTests/IntegrityMissingReceiptRegressionTests.cs`;
- `tests/Finora.IntegrationTests/IntegrityReceiptHashMetadataRegressionTests.cs`;
- `tests/Finora.IntegrationTests/IntegrityReceiptHashRegressionTests.cs`;
- `tests/Finora.IntegrationTests/IntegrityReceiptSizeRegressionTests.cs`;
- `tests/Finora.IntegrationTests/IntegritySplitTotalRegressionTests.cs`.

### New restore-recovery regression tests

- `tests/Finora.IntegrationTests/RestoreRecoveryJournalLinkTests.cs`;
- `tests/Finora.IntegrationTests/RestoreRecoveryRollbackLinkTests.cs`.

No source file from GitHub's compare result for this 34-commit pass is omitted from this inventory.

---

## 132. Evidence-based release boundary after migration/backup/integrity hardening

Finora 0.2.0 now has automated evidence on the exact current source candidate for:

- structural preflight;
- 273/273 current automated tests;
- Windows MAUI Release source compilation;
- Android MAUI Release source compilation;
- iOS MAUI Release source compilation;
- Mac Catalyst MAUI Release source compilation;
- CodeQL;
- production migration target validation before schema-marker advancement;
- migration version guards/data preservation/idempotence/rollback/foreign-key rejection;
- hostile encrypted-backup validation;
- mandatory portable receipt checksum metadata;
- deliberate integrity-corruption detection;
- linked restore-journal and rollback-copy fail-closed behavior.

The following remain separate release gates and are **not** relabeled complete by these source/CI results:

- signed Android AAB production packaging;
- Windows MSIX generation/publisher/signing;
- iOS provisioning/signing/archive/TestFlight/App Store;
- Mac Catalyst signing/notarization/distribution packaging;
- installed prior-version upgrade testing on every target;
- actual process-kill/low-disk/locked-file restore failure injection;
- real notification/biometric/Windows Hello behavior;
- real file picker/share/receipt behavior;
- Android merged-manifest and actual backup/device-transfer behavior;
- TalkBack/VoiceOver/Narrator/keyboard/large-text/high-contrast/reduced-motion QA;
- final dependency-license/vulnerability acceptance;
- live store-policy/privacy/data-safety/external-support-link approval;
- complete absence of undiscovered defects.

Those external gates remain in `docs/NEXT_STEPS.md`, `docs/releases/RELEASE_CHECKLIST.md`, `docs/releases/STORE_READINESS.md`, and `docs/testing/NATIVE_VALIDATION_MATRIX.md`.

`docs/testing/CI_EVIDENCE.md` remains the commit/run/job evidence record.

This `what_changed.md` update is intentionally the final content write for the migration/backup/integrity/recovery hardening continuation.

---

## 133. Currency precision and local-calendar correctness continuation — 2026-08-16

This continuation began from source head:

`4053c5eae3d9644dd518e72b2dd8e69cc604c423`

Commit message:

`test(reset): preserve non-finance app settings`

That starting candidate already had concrete GitHub Actions evidence for:

- structural preflight;
- 101/101 unit tests;
- 145/145 integration tests;
- 35/35 UI-contract tests;
- 281/281 total tests;
- Windows/Android/iOS/Mac Catalyst Release source builds.

The repository documentation still pointed primarily to the older 273-test `f80b29d…` evidence, so this continuation addressed both the next automated correctness gap and that evidence drift.

The final verified runtime/source candidate for this continuation is:

`8260ac02e4f683fa9749f9371185c25d5e3043f6`

It is 13 focused source/test/documentation commits ahead of `4053c5e…` before the subsequent evidence/status/roadmap/test-plan/ledger documentation-only commits.

---

## 134. Production local-calendar bug fixed in FinanceStore

A source audit found that the shared reporting/date-filter path already used `LocalDateRange`, but `FinanceStore.GetBudgetsAsync` and the legacy `FinanceStore.GetDashboardAsync` still converted selected calendar dates by constructing UTC-midnight timestamps.

That assumption is incorrect outside UTC and can misclassify transactions near a local day boundary. For example, India UTC+05:30 has a local midnight at 18:30 UTC on the previous UTC date.

`src/Finora.Infrastructure/FinanceStore.cs` now:

- imports the shared `Finora.Shared.LocalDateRange` policy;
- accepts an optional `TimeZoneInfo` in its constructor;
- defaults that value to `TimeZoneInfo.Local` for production use;
- converts budget `StartsOn`/`EndsOn` through `LocalDateRange.ToUtc`;
- filters budget transactions with `[FromUtc, ToExclusiveUtc)`;
- converts Dashboard selected `start`/`end` through the same policy;
- filters Dashboard transactions with the same exclusive-end convention.

This is a production correctness fix rather than a test-only change.

The optional constructor timezone exists so deterministic integration tests can prove India, negative-offset, and DST behavior without changing the production default.

---

## 135. LocalDateRange automated matrix expanded

`tests/Finora.UnitTests/LocalDateRangeTests.cs` now directly covers:

- UTC local midnight as UTC midnight;
- UTC+05:30 fixed offset;
- UTC-07:00 fixed offset;
- deterministic DST-start day producing a 23-hour UTC span;
- deterministic DST-end day producing a 25-hour UTC span;
- multi-day exclusive end boundary;
- reversed range rejection.

This makes the intended local-calendar contract explicit rather than leaving UTC itself implicit.

---

## 136. FinanceStore local-calendar integration coverage added

New file:

`tests/Finora.IntegrationTests/LocalCalendarFinanceStoreTests.cs`

It proves production store behavior with deterministic zones:

- a UTC+05:30 one-day custom budget includes a transaction that falls on the selected local day even though its UTC date is the previous date;
- a later UTC timestamp belonging to the next India local day is excluded;
- a UTC+05:30 Dashboard day uses the same boundary;
- a UTC-07:00 Dashboard day excludes the previous local day and includes the selected local day;
- a deterministic DST-start Dashboard day uses the correct shortened UTC span.

These automated tests do not replace testing the actual target OS timezone database and device timezone-change behavior before release.

---

## 137. Representative 0/2/3/4-decimal currency matrix expanded

The continuation deliberately uses representative currency metadata classes rather than assuming every currency has two decimal places:

- JPY — 0 decimal places;
- INR — 2 decimal places;
- KWD — 3 decimal places;
- CLF — 4 decimal places.

`CurrencyAwareImportTests.cs` now includes INR in addition to the existing JPY/KWD/CLF precision cases.

New `CurrencyPrecisionWorkflowTests.cs` proves exact minor-unit behavior through:

- account opening/current balance;
- budget limit/planned/actual calculations;
- savings goal target/start/contribution/current/progress;
- recurring rule/occurrence/generated paid transaction;
- reconciliation preview/difference/adjustment/final balance.

All assertions use exact integer minor units at persistence/service boundaries.

---

## 138. CSV export → preview/import precision round trip added

New file:

`tests/Finora.IntegrationTests/ExportCurrencyPrecisionTests.cs`

The suite now proves that JPY/INR/KWD/CLF rows:

- are created from currency-aware values;
- export the exact stored `AmountMinor`;
- retain their currency and account identity in the generated CSV;
- pass the export preview parser;
- can be imported into a second isolated SQLite database using `AmountMinor` mode;
- preserve exact minor-unit values after the round trip.

This closes a portable-data regression gap between conversion tests and actual export/import behavior.

---

## 139. Encrypted backup multi-precision round trip added

New file:

`tests/Finora.IntegrationTests/BackupCurrencyPrecisionRoundTripTests.cs`

The test builds a synthetic finance profile with JPY/INR/KWD/CLF rows, then:

1. creates an encrypted backup;
2. previews/authenticates it;
3. deletes all finance data through `FinanceDataResetService`;
4. proves the live profile is empty;
5. restores the encrypted backup;
6. proves restored account/currency relationships;
7. proves exact restored minor-unit values;
8. runs the normal `DataIntegrityService`;
9. requires a healthy SQLite/foreign-key/finance result.

The backup path is therefore covered for currency precision itself, not only graph validity and cryptographic hostile-input cases.

---

## 140. Report precision coverage added

New file:

`tests/Finora.IntegrationTests/ReportCurrencyPrecisionTests.cs`

It creates exact JPY/INR/KWD/CLF income and expense rows and requires `AdvancedReportService.GetIncomeExpenseAsync` to return the exact stored minor-unit totals for each reporting currency.

This complements the existing currency-isolation report tests by proving representative precision classes do not drift inside report aggregation.

---

## 141. Strict analyzer failure was caught and corrected

The first combined candidate containing the new precision/calendar tests ran through Finora CI run:

`31934141986`

Structural preflight and the unit suite succeeded, but the integration project was blocked by warnings-as-errors because three new test assertions used an xUnit pattern rejected by analyzer:

`xUnit2031`

The affected assertions used:

`Assert.Single(collection.Where(predicate))`

They were corrected in two focused commits to use:

`Assert.Single(collection, predicate)`

No production rule, analyzer, or warnings-as-errors policy was weakened.

The failed intermediate run is retained as useful evidence that strict analyzer findings still stop the gated pipeline.

---

## 142. Exact 13-commit source/test continuation trail

From `4053c5eae3d9644dd518e72b2dd8e69cc604c423` through verified source candidate `8260ac02e4f683fa9749f9371185c25d5e3043f6`, the focused commits are:

1. `81da3ff1992f84fa2d460955ffc68d2d633ab058` — `test(export): cover currency precision round trips`;
2. `1b20d3817a3cb8c28c0aa629dea3951dea2944c6` — `test(backup): cover multi-precision currency round trip`;
3. `d10d9ed11b62c9bbaa71c4cd9d64b7bca61670bd` — `test(backup): assert restored account relationship`;
4. `9a64a6142a2df09a66ce818ee92b9d5188552fd1` — `test(time): cover explicit UTC calendar boundary`;
5. `12abad0677060dfa167c8aaf73a95250571c5941` — `test(import): cover two-decimal currency rounding`;
6. `1120d043a2462e08a6be53f66335759bafa22b22` — `fix(calendar): use local boundaries for budgets and dashboard`;
7. `51fcefc215f549011f4183a9b29a92522aef9dea` — `test(calendar): cover budget and dashboard local boundaries`;
8. `54fe404ada90fe08d371dfa51d6d739562131876` — `test(currency): cover core finance workflows across precisions`;
9. `f6881034598e3b90290381e88caa982442a3e90f` — `test(calendar): cover negative-offset store boundary`;
10. `6e1daa0ce2dfebc6270a565ff998de7dcf8fefb4` — `test(reports): cover currency precision classes`;
11. `cb6cedcc50051c29e7007dea0fbc0f2e730ad283` — `test(backup): satisfy strict xunit single analyzer`;
12. `8656733cb041bafdbf87b7993790c809fe188034` — `test(export): satisfy strict xunit single analyzer`;
13. `8260ac02e4f683fa9749f9371185c25d5e3043f6` — `docs(testing): document precision and local-calendar regressions`.

The source/test compare changes nine files. Only one production source file changed: `src/Finora.Infrastructure/FinanceStore.cs`; the remaining changes are regression tests plus `docs/testing/TESTING_GUIDE.md`.

---

## 143. Exact 310-test source-candidate evidence

Verified source candidate:

`8260ac02e4f683fa9749f9371185c25d5e3043f6`

Finora CI run:

`31934249592`

CodeQL run:

`31934249613`

Finora CI passed:

- Structural preflight — job `95133649345`;
- Core tests — job `95133666510`;
- Windows Release source build — job `95133762880`;
- Android Release source build — job `95133762915`;
- iOS Release source build — job `95133762871`;
- Mac Catalyst Release source build — job `95133762913`.

Exact test result:

- Unit: **102/102 passed**;
- Integration: **173/173 passed**;
- UI-contract: **35/35 passed**;
- Total: **310/310 passed**;
- Failed: **0**.

Retained core artifact:

- `core-test-results` — artifact `9260190133`;
- SHA-256 `c80fe9a24b40f033524121a75fdfc1f3a5eca173c607bf4a973b8c6c7cc42999`.

Retained native diagnostic artifacts:

- Windows — artifact `9260232838`, SHA-256 `cc753d899eac9c1ae46abfe59e15725d80ed54c2f36291650c53f335224f26b5`;
- Android — artifact `9260279323`, SHA-256 `b6f42dce4695d85614e866faa32d0a741e9232f5eb7a87c88f29b86e998f6250`;
- iOS — artifact `9260383176`, SHA-256 `098d737945ec4d1024be5425020d83809b22e3689deaa13c90d0026e724eb50d`;
- Mac Catalyst — artifact `9260224740`, SHA-256 `b691cab1e5a94ac6492b3d31bbbc9d25d38cf5e5ab5c3b56db99f95c5f92b8a3`.

CodeQL job `95133633181` completed successfully on the same exact source candidate.

---

## 144. Documentation/evidence alignment commits after the verified source candidate

After `8260ac02…` was fully green, runtime source was frozen and documentation was advanced in separate commits so evidence remained anchored to one exact source candidate.

Focused documentation commits before this ledger write:

- `0f3cb57e64ff5b5d3edb09f3a9c0c22d7aa88f33` — `docs(evidence): record 310-test precision calendar candidate`;
- `73a4840033a24183d645f2b23b91a3b4a35cbcdf` — `docs(status): advance verified precision and calendar coverage`;
- `f4fee9e7ac4af8e1667e91ad730c971ecf90df95` — `docs(roadmap): mark automated precision calendar gates`;
- `85c6bd7e690e8bcf26168ab7fdbc41705b90302f` — `docs(test): expand precision and timezone release matrix`;
- final focused ledger commit updating `what_changed.md`.

Updated documentation includes:

- `docs/testing/CI_EVIDENCE.md` — exact 310-test/run/job/artifact/digest evidence plus current/historical candidate boundaries;
- `PROJECT_STATUS.md` — current precision/calendar/reset/migration/backup/integrity status and unresolved native/store gates;
- `docs/NEXT_STEPS.md` — automated portions of currency/timezone P0 work marked complete while native UI/device validation remains open;
- `docs/TEST_PLAN.md` — expanded 0/2/3/4-decimal, CSV round-trip, backup round-trip, report, FinanceStore local-boundary, and native timezone/currency matrices;
- `docs/testing/TESTING_GUIDE.md` — practical precision/local-calendar regression guidance;
- `what_changed.md` — cumulative continuation ledger.

Documentation-only commits after `8260ac02…` do not change the runtime/test source proven by run `31934249592`; the evidence document states this boundary explicitly.

---

## 145. Commit identity observation corrects the older connector limitation for this session

Historical sections 2 and 82 remain unchanged because they accurately documented the connector capability/observability available when those continuations were written.

For the 2026-08-16 verified candidate, however, GitHub Actions run metadata for head commit `8260ac02e4f683fa9749f9371185c25d5e3043f6` exposes the commit author/committer identity as:

`Sanskar <sanskarin@outlook.in>`

Therefore the older statement that connector-created commits in this environment could not truthfully be shown to use the requested email is superseded for this continuation by concrete GitHub commit/run metadata.

This is recorded as a new historical correction rather than retroactively deleting the old limitation sections.

---

## 146. External/native release boundary after precision/calendar hardening

The 2026-08-16 source candidate now has concrete automated evidence for:

- structural preflight;
- **310/310** automated tests;
- strict analyzer/warnings-as-errors enforcement;
- Windows/Android/iOS/Mac Catalyst Release source compilation;
- CodeQL;
- representative 0/2/3/4-decimal currency conversion and workflow precision;
- CSV export/preview/re-import exact minor-unit round trip;
- encrypted backup/reset/restore exact minor-unit round trip;
- report precision;
- UTC/+05:30/-07:00/DST local-calendar conversion;
- production `FinanceStore` budget/Dashboard local-calendar boundaries;
- previously verified migration, hostile-backup, integrity-corruption, receipt-checksum, restore-recovery, privacy-log, and reset-safety behavior retained in the same source line.

The following remain separate release gates and are **not** relabeled complete:

- signed Android AAB packaging;
- Windows MSIX generation/publisher/signing;
- iOS provisioning/signing/archive/TestFlight/App Store;
- Mac Catalyst signing/notarization/distribution packaging;
- installed prior-version upgrade testing on every applicable target;
- actual process-kill/low-disk/locked-file restore failure injection;
- native JPY/INR/KWD/CLF entry/edit/display and assistive-technology QA;
- actual target-device timezone changes and OS timezone/DST behavior;
- real notification/biometric/Windows Hello behavior;
- real file picker/share/receipt behavior;
- Android merged-manifest and actual backup/device-transfer behavior;
- TalkBack/VoiceOver/Narrator/keyboard/large-text/high-contrast/reduced-motion QA;
- final exact dependency-license/vulnerability acceptance;
- live store-policy/privacy/data-safety/external-support-link approval;
- complete absence of undiscovered defects.

Those gates remain in `docs/NEXT_STEPS.md`, `docs/releases/RELEASE_CHECKLIST.md`, `docs/releases/STORE_READINESS.md`, `docs/TEST_PLAN.md`, and `docs/testing/NATIVE_VALIDATION_MATRIX.md`.

`docs/testing/CI_EVIDENCE.md` is the exact current source-candidate proof record.

This `what_changed.md` update preserves the complete prior 132-section history and appends the 2026-08-16 precision/calendar/evidence continuation rather than replacing or shortening earlier project history.

---

## 147. Database-backed transaction history paging continuation — 2026-08-18

This continuation began from `main` head:

`59e7876b283916eae63838ad8b552dd889532964`

and moved the next code-level roadmap item from bounded in-memory presentation to true database-backed interactive history paging.

Exact frozen runtime/source candidate:

`d841efb8c392860b221f331b4ced9119020b849e`

Runtime/test source was frozen at that SHA before the evidence/status/roadmap/changelog/ledger documentation series. The documentation-only commits after it do not change the runtime source proven by the exact candidate CI runs.

The implementation intentionally preserves the current local-first product boundary, schema version 2, existing transaction workflows, all five history sort modes, shared local-calendar policy, and the legacy complete-result search API for bounded workflows that still need it.

---

## 148. Paged query contract and dedicated read service

`src/Finora.Application/Contracts.cs` now defines:

- `TransactionHistorySort` with NewestFirst, OldestFirst, AmountHighToLow, AmountLowToHigh, and MerchantAscending;
- `TransactionHistoryQuery` carrying `SearchText`, `AccountId`, `CategoryId`, `Type`, `FromUtc`, `ToExclusiveUtc`, `Sort`, `Offset`, and `PageSize`;
- `TransactionHistoryPage` carrying `Items`, `TotalCount`, and `HasMore`;
- `ITransactionHistoryStore` with a dedicated paged read operation.

New production source:

`src/Finora.Infrastructure/TransactionHistoryStore.cs`

The store:

- uses the existing pooled `IDbContextFactory<FinoraDbContext>`/SQLite path;
- excludes soft-deleted transactions before count and paging;
- applies account/category/type/date filters before materialization;
- preserves free-text search across merchant, note, payment method, manual location, account name, and category name;
- validates offset >= 0;
- validates page size 1..200;
- validates exclusive end > start when both date bounds are present;
- counts the filtered query without materializing all matching transaction rows;
- applies stable supported ordering before `Skip`/`Take`;
- returns only the requested page plus total matching count and `HasMore`.

Interactive UI page size remains 50. Store maximum page size is `TransactionHistoryStore.MaximumPageSize = 200`.

Merchant A–Z ordering uses SQLite `NOCASE` collation and stable secondary ordering.

The ordering guarantee is deliberately scoped to a **fixed result set**. Offset paging is not represented as snapshot isolation across concurrent insert/delete mutations between page requests.

`IFinanceStore.SearchTransactionsAsync` remains available for existing bounded workflows that intentionally require complete result sets.

---

## 149. TransactionsViewModel no longer retains the complete matching history

`src/Finora.App/ViewModels/TransactionsViewModel.cs` was changed from an all-results cache plus in-memory slicing to the dedicated paged store.

Removed behavior:

- `_allMatches` complete matching collection in the ViewModel;
- loading every matching row before presenting the first 50;
- in-memory sort/slice as the interactive history scaling mechanism.

Current state uses:

- `_activeQuery`;
- `_totalMatches`;
- `_hasMore`;
- `PageSize = 50`.

Apply/search behavior now:

1. builds a typed `TransactionHistoryQuery`;
2. resolves local advanced date filters through shared `LocalDateRange.ToUtc` and exclusive UTC end semantics;
3. snapshots that exact query as `_activeQuery`;
4. clears the displayed rows;
5. requests offset 0 / page size 50 from `ITransactionHistoryStore`.

**Load more** requests:

- the same `_activeQuery`;
- `Offset = Transactions.Count`;
- `PageSize = 50`.

This means editing filter/sort controls without applying them cannot silently mix newly edited control state into rows appended to the last applied result set.

`HistoryStatus` now reports the visible count against the store-provided total matching count.

`MauiProgram.cs` registers `ITransactionHistoryStore` as the production paged history service.

---

## 150. Paging correctness and search regression coverage

New integration test file:

`tests/Finora.IntegrationTests/TransactionHistoryPagingTests.cs`

Current test matrix includes:

1. **120-row boundary test** — proves 50/50/20 pages, correct newest-first boundaries, 120 unique IDs, no duplicate IDs, and no missing IDs for a fixed result set;
2. **filter-before-count/page test** — proves free-text + account + category + type + date constraints are applied before total count and page materialization;
3. **sort test** — proves oldest-first, amount high-to-low, amount low-to-high, and case-insensitive merchant A–Z behavior while newest-first is also exercised by the boundary test;
4. **invalid paging/date validation** — rejects negative offset, zero page size, page size above 200, and non-increasing date range;
5. **soft-delete exclusion** — proves deleted rows are excluded from both total count and returned page;
6. **extended search fields** — proves payment method, manual location, account name, and category name remain searchable through the new database query.

`tests/Finora.UiTests/TransactionsChartOnboardingContractTests.cs` now additionally requires:

- `TransactionHistoryQuery` usage;
- `GetPageAsync` usage;
- `Offset = Transactions.Count`;
- absence of `_allMatches`.

Existing sort picker, Load more binding, `HasMore`, page-size 50, and shared local-date boundary source contracts remain.

---

## 151. Strict CI caught and corrected a paging-test analyzer regression

Intermediate candidate:

`6617a0b6b07b4cd4befcd48ae22c476ab0b917d1`

Finora CI run:

`32119961474`

The run proved the gating policy remained strict:

- structural preflight passed;
- unit tests passed;
- integration project build was blocked by analyzer `CA1861` in the newly added merchant-sort assertion;
- later native jobs were therefore not falsely interpreted as candidate proof.

The assertion originally supplied a constant array to `Assert.Equal` in a form rejected by the analyzer.

Correction commit:

`d841efb8c392860b221f331b4ced9119020b849e` — `fix(tests): satisfy analyzer for merchant sort assertion`

The correction uses `Assert.Collection` and does not suppress `CA1861`, downgrade warnings-as-errors, or weaken production behavior.

---

## 152. Exact 319-test source-candidate evidence

Exact runtime/source candidate:

`d841efb8c392860b221f331b4ced9119020b849e`

Finora CI run:

`32120115922`

CodeQL run:

`32120115965`

Dependency Review run:

`32120115912`

Finora CI jobs on that exact candidate:

- Structural preflight `95658397777` — success;
- Core tests `95658437947` — success;
- Android Release source build `95658684131` — success;
- Mac Catalyst Release source build `95658684209` — success;
- iOS Release source build `95658684277` — success;
- Windows Release source build `95658684327` — success.

Exact strict automated results:

- Unit: **102/102 passed**;
- Integration: **179/179 passed**;
- UI-contract: **38/38 passed**;
- Total: **319/319 passed**;
- Failed: **0**;
- Skipped: **0**.

Core test artifact:

- artifact ID `9318206622`;
- SHA-256 `5f324ea6d3b65ab5d8dc5a52dbdd9c4c26610333086c9b2752415738761ff4a7`.

All four MAUI Release source-build targets passed under the repository warnings-as-errors/strict XAML policy.

CodeQL and Dependency Review also completed successfully for the exact candidate.

This is source/test/build/security-workflow evidence, not signed-package/device/store evidence.

---

## 153. Exact focused commit trail for the database-paging continuation

The runtime/test/feature-documentation source sequence from the 2026-08-18 work branch is:

1. `8e5ed441e30a7a35abc94c691bbb0d25e4746969` — `feat(transactions): add paged history query contract`;
2. `25facc4748eee217bbe45ecd58d3ce25702eaf99` — `refactor(transactions): isolate paged history store contract`;
3. `e1d9598885250e77dc24078cd5eb62d54500b7d0` — `feat(transactions): implement database-backed history paging`;
4. `d1f43778037e4bacb1e9b17c4b8e19aea5b5cb89` — `feat(transactions): register paged history store`;
5. `3e719199b9115f5c74858886e9ac70676a0740d8` — `feat(transactions): page history from SQLite`;
6. `41b747348e108c3b4d9e7b95288410c63473043e` — `test(transactions): cover paged history boundaries`;
7. `7ebc00eed5b95c88414c4dbd1d879797fa248c17` — `fix(tests): use current category model in paging coverage`;
8. `528255e785d5a9d4b8f901cede054a960f50087b` — `fix(transactions): keep merchant sorting analyzer-safe`;
9. `5aac906f3987b09998358b53cb68f1a0cf2c9077` — `test(ui): enforce database-backed transaction paging contract`;
10. `884709c9c346699778a18c41e8035bd2fac5a157` — `docs(transactions): document database-backed history paging`;
11. `275f49ce4c768b3e2a10d4184ea8edaa388676f9` — `fix(transactions): expose paging limit for validation consumers`;
12. `a9a8f59fba01ebe0a1f27f72ee403335c42a22f9` — `test(transactions): exclude deleted rows from paged history`;
13. `6617a0b6b07b4cd4befcd48ae22c476ab0b917d1` — `test(transactions): preserve extended search fields in paging`;
14. `d841efb8c392860b221f331b4ced9119020b849e` — `fix(tests): satisfy analyzer for merchant sort assertion`.

The evidence/status/roadmap/changelog documentation sequence after the frozen runtime candidate is:

15. `45b0a6e79049183a4d962336565e570397590bbc` — `docs(architecture): document transaction history paging service`;
16. `45ebab7a9ac186234962539e7911245d70d3f42a` — `docs(evidence): record database paging candidate`;
17. `888ad30a65777dc6cbd57579ab4324a401f5e195` — `docs(status): advance database paging evidence`;
18. `cb24ebf9389c9ad759af37f12284a3749806f68e` — `docs(roadmap): mark database paging implemented`;
19. `b5e3a362a4a7dafbb7c45fe07ea5e728b7910220` — `docs(changelog): record database paging and evidence`;
20. final ledger commit — `docs(status): append database paging continuation ledger`.

The final ledger commit SHA is intentionally not self-inserted into the content it creates. Git history remains the authoritative exact commit identity after the write.

---

## 154. Complete changed-file inventory for this continuation

Production source/test/feature-documentation files changed by the database-paging work:

- `src/Finora.Application/Contracts.cs`;
- `src/Finora.Infrastructure/TransactionHistoryStore.cs` — new file;
- `src/Finora.App/MauiProgram.cs`;
- `src/Finora.App/ViewModels/TransactionsViewModel.cs`;
- `tests/Finora.IntegrationTests/TransactionHistoryPagingTests.cs` — new file;
- `tests/Finora.UiTests/TransactionsChartOnboardingContractTests.cs`;
- `docs/features/ACCOUNTS_AND_TRANSACTIONS.md`.

Evidence/architecture/status/roadmap/changelog/ledger files changed after runtime source was frozen:

- `docs/architecture/SERVICE_CATALOG.md`;
- `docs/testing/CI_EVIDENCE.md`;
- `PROJECT_STATUS.md`;
- `docs/NEXT_STEPS.md`;
- `CHANGELOG.md`;
- `what_changed.md`.

No source/test/documentation file from this continuation's changed-file set is intentionally omitted from this ledger.

---

## 155. Evidence-based release boundary after database-backed paging

The 2026-08-18 paging continuation now has exact automated evidence for:

- the existing structural preflight;
- **319/319** automated tests;
- warnings-as-errors/analyzer enforcement;
- Windows/Android/iOS/Mac Catalyst Release source builds;
- CodeQL;
- Dependency Review;
- database-backed interactive transaction paging;
- filter/search/sort application before page materialization;
- total count and `HasMore` behavior;
- bounded page-size validation;
- soft-delete exclusion;
- deterministic page boundaries for a fixed result set;
- 120-row 50/50/20 page coverage with no duplicate/missing IDs;
- stable last-applied-query behavior for Load more;
- retained currency/local-calendar/migration/backup/integrity/recovery regression suites from the same source line.

Roadmap item 26 is therefore implemented and automated for the current source line.

Roadmap item 27 remains open. This continuation does **not** claim completion of 10k/50k/100k performance or memory benchmarks merely because the query architecture now pages at the database.

The following external/release gates also remain open and are not relabeled complete:

- signed Android AAB production packaging and installation;
- Windows MSIX generation/publisher/signing and packaged installation;
- iOS provisioning/signing/archive/TestFlight/App Store packaging;
- Mac Catalyst signing/notarization/distribution packaging;
- complete installed prior-version upgrade testing on applicable targets;
- actual process-kill/low-disk/locked-file restore failure injection;
- physical-device notification/biometric/Windows Hello behavior;
- real file picker/share/import/export/receipt behavior;
- Android merged-manifest and real backup/device-transfer behavior;
- TalkBack/VoiceOver/Narrator/keyboard/large-text/high-contrast/reduced-motion QA;
- final exact dependency-license/vulnerability acceptance beyond the recorded automated Dependency Review run;
- live store-policy/privacy/data-safety/external-support-link approval;
- complete absence of undiscovered defects.

Those gates remain tracked in `docs/NEXT_STEPS.md`, `docs/releases/RELEASE_CHECKLIST.md`, `docs/releases/STORE_READINESS.md`, `docs/TEST_PLAN.md`, and `docs/testing/NATIVE_VALIDATION_MATRIX.md`.

`docs/testing/CI_EVIDENCE.md` is the exact current source-candidate proof record.

This `what_changed.md` update preserves all previous 146 sections and appends the 2026-08-18 database-backed transaction-history paging continuation without replacing or shortening the prior project history.

---

## 156. Large-dataset performance tooling continuation — 2026-08-18

After database-backed transaction history paging was merged into the current source line, the next repository-level P2 gap was measurable large-dataset behavior rather than another unverified feature family.

The performance work is carried by pull request **#17**, branch:

`work/performance-benchmarks-20260818`

against `main`.

The exact frozen runtime/source candidate used for the first retained performance evidence is:

`8a8e7e51a2bacecdc58405d3d5301e79f3d78c8b`

Runtime/test/tool source was frozen at that SHA before the final evidence/status/roadmap/changelog/ledger documentation series. Documentation-only commits after it do not change the runtime source proven by the exact candidate CI runs.

The work deliberately does not alter Finora's local-first product architecture, database schema, finance-domain rules, money representation, backup format, or transaction semantics merely to obtain smaller timing numbers.

---

## 157. Standalone synthetic performance harness added

New developer tooling lives under:

`tools/Finora.Performance`

The tool is a standalone `net10.0` executable and is not part of the packaged MAUI application runtime.

It uses the same production Application/Infrastructure services being measured rather than reimplementing finance logic inside the benchmark.

The harness can generate deterministic synthetic finance data including:

- four INR accounts;
- normal initialized categories;
- configurable transactions;
- budgets;
- savings goals;
- recurrence rules;
- optional bounded synthetic receipt files;
- matching SHA-256 attachment metadata.

Transaction seeding is batched so 100,000-row profiles do not require constructing the entire transaction graph in memory before persistence.

No real user finance data is read or imported by the benchmark harness.

Supported measurement families include:

- populated database startup initialization;
- first-page transaction history paging;
- deep offset transaction paging;
- common and selective free-text history search;
- amount-sorted history paging;
- income/expense reporting;
- category spending;
- merchant/payee reporting;
- account balance trends;
- budget performance;
- recurring obligations;
- savings progress;
- full CSV export;
- isolated CSV import round trip;
- full PDF export;
- encrypted backup creation;
- encrypted backup restore;
- full data-integrity checking;
- managed-heap observations;
- process working-set observations.

The CLI supports explicit operation selection and repeatable iterations. The documented primary dataset sizes are 10,000, 50,000, and 100,000 transactions.

---

## 158. Benchmark correctness gates and evidence policy

The benchmark is not designed as a stopwatch-only demo.

Correctness gates include:

- transaction history must report the expected visible transaction count;
- a valid deep page must not unexpectedly become empty;
- CSV/PDF/encrypted backup output must be non-empty when those operations are selected;
- CSV import runs against a fresh isolated benchmark database and must reproduce the exact expected transaction count;
- synthetic CSV import must report zero skipped duplicates and zero invalid/error rows;
- encrypted backup restore must succeed;
- restored transaction and attachment counts must match the expected synthetic graph;
- `DataIntegrityService` must report a healthy benchmark graph.

A correctness failure returns a nonzero process result.

Timing values are explicitly observational. Finora does not fail correctness or release CI merely because one GitHub-hosted runner is slower than an arbitrary millisecond threshold.

The harness emits machine-readable JSON containing:

- product/harness identity;
- .NET runtime description;
- OS description;
- process architecture;
- processor count;
- UTC timestamps;
- dataset counts;
- database/attachment sizes where available;
- selected operations;
- iteration count;
- elapsed milliseconds;
- managed-heap observations;
- process working-set observations;
- output sizes/item counts;
- timing/data/paging evidence-policy notes.

---

## 159. CI smoke and on-demand large-profile workflow added

`.github/workflows/ci.yml` now includes a bounded:

`Performance smoke (10k)`

job.

The normal pull-request smoke:

- restores the performance project;
- builds the complete harness in Release under repository warnings-as-errors policy;
- seeds 10,000 synthetic transactions plus bounded supporting records;
- executes `startup,history,reports,integrity`;
- uploads the JSON result.

The normal CI job intentionally does not execute the heaviest complete export/import/backup round trips on every pull request.

A separate manual workflow was added:

`.github/workflows/performance.yml`

It supports:

- 10,000 transactions;
- 50,000 transactions;
- 100,000 transactions;
- selectable operation list;
- 1, 2, or 3 iterations;
- retained JSON artifacts.

The complete `all` profile includes CSV export/import, PDF export, encrypted backup creation/restoration, startup/history/report/integrity work.

CSV benchmark selection refuses datasets above the production CSV import ceiling of 100,000 rows so tooling does not pretend the product accepts an unsupported import size.

---

## 160. Exact 319-test + 10k smoke candidate evidence

Exact source candidate:

`8a8e7e51a2bacecdc58405d3d5301e79f3d78c8b`

Required GitHub Actions runs all completed successfully:

- Finora CI `32127759802`;
- CodeQL `32127759687`;
- Dependency Review `32127759673`.

Finora CI jobs:

- Structural preflight `95682010091` — success;
- Core tests `95683208566` — success;
- Performance smoke (10k) `95683208597` — success;
- Windows Release source build `95684553116` — success;
- Android Release source build `95684553130` — success;
- iOS Release source build `95684553150` — success;
- Mac Catalyst Release source build `95684553224` — success.

Exact core results:

- Unit: **102/102 passed**;
- Integration: **179/179 passed**;
- UI-contract: **38/38 passed**;
- Total: **319/319 passed**;
- Failed: **0**;
- Skipped: **0**.

Core-test artifact:

- artifact `9321292681`;
- SHA-256 `c70c959ee19352cd67bbdb0330e99c2ba1ea8dd349c281fd517be9e67b3435f0`.

The performance project Release build completed with:

- **0 warnings**;
- **0 errors**.

The 10k smoke seeded 10,000 synthetic transactions successfully in approximately 4.15 seconds on the recorded GitHub-hosted runner.

The bounded smoke retained these one-iteration observational timings:

- `startup.initialize` — 34.049 ms;
- `history.first-page` — 49.127 ms;
- `history.deep-page` — 13.435 ms;
- `history.search-common` — 33.475 ms;
- `history.search-selective` — 18.104 ms;
- `history.amount-sort` — 10.651 ms;
- `reports.income-expense` — 44.270 ms;
- `reports.category-spending` — 270.318 ms;
- `reports.merchant` — 46.875 ms;
- `reports.account-trends` — 51.281 ms;
- `reports.budgets` — 914.281 ms;
- `reports.recurring` — 13.804 ms;
- `reports.savings` — 18.984 ms;
- `integrity.full` — 262.725 ms.

Performance JSON artifact:

- artifact `9321290557` (`performance-smoke-10k`);
- SHA-256 `97eb07bf963491e8d89d45798b21aa99d0da312b931c3ea25b17e2dae5accb46`.

Retained native diagnostic artifacts for the same exact source candidate:

- Windows `9321588237` — SHA-256 `1efc14f54404fc0ae0747a462c5a4bdfa91be12413b0abc9a287e8b600c04525`;
- Android `9321676747` — SHA-256 `43be11c2ea1abf2f7968d3df687e6ed5b83903759cb089e4833550b4b16668d6`;
- Mac Catalyst `9321864012` — SHA-256 `7588a9d80ceace999e590118f5da87822dc303c25d4ea5778a82e8cb8267db25`;
- iOS `9322174945` — SHA-256 `fefa32db111ce35be90f56e7ea1d0f1ab0da8b24805c348bb06b1f0a8a32dd49`.

These measurements are retained runner observations and are not universal performance guarantees.

---

## 161. Final source audit found no additional production-finance defect

The final source audit for this continuation rechecked whether apparently “missing” core transaction capabilities still required implementation.

The current source line already contains:

- expense/income/refund/adjustment quick add and edit;
- paired same-currency transfers;
- advanced transaction search/filter/sort;
- database-backed history paging;
- revision history;
- bulk categorization;
- duplicate review;
- transaction splits;
- tags;
- receipt attachments;
- soft delete and restore;
- selected/all CSV export;
- selected/all PDF export;
- linked transfer editing;
- mapped CSV import;
- reconciliation;
- budgets;
- goals;
- recurring workflows;
- reports;
- encrypted backup/restore;
- app lock/privacy/notifications/integrity tooling.

No new production-finance defect was identified in the final audit that justified changing core finance behavior merely to increase commit count or feature count.

The repository-level gap that could be completed safely was measurable large-dataset tooling and evidence, which is what this continuation implements.

This is not a claim that undiscovered defects cannot exist.

---

## 162. Documentation and evidence alignment after the frozen source candidate

After `8a8e7e51…` completed the required runtime/source checks, documentation-only evidence commits advanced the repository records while keeping the source evidence anchored to the exact tested candidate.

Updated files include:

- `docs/testing/PERFORMANCE_BENCHMARKING.md` — benchmark methodology, operations, correctness gates, exact 10k smoke evidence, artifact/digest, and unexecuted-profile boundary;
- `docs/testing/CI_EVIDENCE.md` — current exact source candidate, run/job IDs, 319-test counts, native artifacts, performance artifact, observed timings, and evidence policy;
- `PROJECT_STATUS.md` — performance tool/build/smoke status plus remaining full-profile/native/store gates;
- `docs/NEXT_STEPS.md` — roadmap item 27 changed from missing tooling to implemented tooling with remaining full comparison evidence;
- `docs/DOCUMENTATION_STATUS.md` — documentation coverage aligned to the new performance/evidence state;
- `CHANGELOG.md` — performance harness and exact evidence recorded under Unreleased;
- `what_changed.md` — this cumulative continuation ledger.

Focused documentation commits created in this final alignment include:

- `a494036e49bd938b2aff3dd577afc2a3a6510985` — `docs(perf): record verified 10k smoke evidence`;
- `661a00532b3ca5e6b41c98df0d93bcdb3e0dadc2` — `docs(evidence): record performance harness candidate`;
- `0e874944fbe5b6016842b3140bb90d87476b4f40` — `docs(status): advance verified performance tooling`;
- `614dc12209f3f7554ca961714d5d918db279fa55` — `docs(roadmap): mark performance tooling implemented`;
- `8df4f58bcdabb4c2d53d80ab32777d670ec2d80b` — `docs(status): align performance evidence coverage`;
- `2a857a319dbe7bf91e181296e8e2de0010781e92` — `docs(changelog): record performance harness and evidence`;
- final focused ledger commit updating this file.

The PR body was also expanded with the exact source candidate, runs/jobs, artifacts/digests, test counts, bounded smoke timings, correctness policy, and explicit evidence boundary.

---

## 163. Complete continuation changed-file inventory and remaining boundary

The performance workstream includes these benchmark/source/CI/documentation files:

### Performance tooling

- `tools/Finora.Performance/Finora.Performance.csproj`;
- `tools/Finora.Performance/PerformanceDbFactory.cs`;
- `tools/Finora.Performance/PerformanceModels.cs`;
- `tools/Finora.Performance/PerformanceOptions.cs`;
- `tools/Finora.Performance/PerformanceRunner.cs`;
- `tools/Finora.Performance/PerformanceSeeder.cs`;
- `tools/Finora.Performance/Program.cs`.

### Solution/automation

- `Finora.sln`;
- `.github/workflows/ci.yml`;
- `.github/workflows/performance.yml`.

### Performance/documentation system

- `docs/testing/PERFORMANCE_BENCHMARKING.md`;
- `docs/README.md`;
- `docs/DOCUMENTATION_STATUS.md`;
- `docs/testing/CI_EVIDENCE.md`;
- `PROJECT_STATUS.md`;
- `docs/NEXT_STEPS.md`;
- `CHANGELOG.md`;
- `what_changed.md`.

The exact PR/Git history is authoritative for every focused commit and final ordering.

The current exact executed evidence proves:

- structural preflight;
- 319/319 current automated tests;
- performance project Release compilation with zero warnings/errors;
- bounded 10k synthetic startup/history/reports/integrity smoke;
- Windows/Android/iOS/Mac Catalyst Release source builds;
- CodeQL;
- Dependency Review;
- retained finance safety/paging/precision/calendar/migration/backup/integrity/recovery suites from the same source line.

The following are still intentionally **not** claimed complete:

- runtime 10k `--operations all` performance profile;
- runtime CSV import/export performance round trip inside the harness;
- runtime PDF-export performance measurement inside the harness;
- runtime encrypted-backup create/restore performance round trip inside the harness;
- 50k `all` comparison profile;
- 100k `all` comparison profile;
- signed Android AAB production package/installation;
- Windows MSIX publisher/signing/package installation;
- iOS provisioning/signing/archive/TestFlight/App Store package;
- Mac Catalyst signing/notarization/distribution package;
- physical-device UI responsiveness/battery/thermal/low-memory behavior;
- installed prior-version upgrade evidence on every applicable target;
- process-kill/low-disk/locked-file recovery failure injection;
- native notification/biometric/Windows Hello/file-picker/share behavior;
- Android real backup/device-transfer behavior;
- TalkBack/VoiceOver/Narrator/keyboard/large-text/high-contrast/reduced-motion QA;
- final live store-policy/privacy/data-safety/external-support-link approval;
- absence of every undiscovered defect.

The current execution environment still does not provide a local .NET SDK, so the unexecuted full benchmark profiles could not be honestly converted into runtime evidence from this session. The dedicated on-demand GitHub workflow is the correct mechanism for those later runs.

Roadmap item 27 is now **implemented as tooling** with the bounded 10k smoke executed; the complete comparable 10k/50k/100k `all` evidence matrix remains the next performance-evidence task.

This update preserves the entire previous 155-section ledger and appends the performance-hardening continuation without replacing, summarizing, or shortening the earlier history.
