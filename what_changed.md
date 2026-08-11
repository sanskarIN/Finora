# What Changed — Finora

Last continuation: **2026-08-11**  
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
