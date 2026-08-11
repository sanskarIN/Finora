# Reports, CSV Import, and Export

This document describes the current reporting, CSV import, CSV export, and PDF export behavior implemented in Finora 0.2.0.

## Reporting principles

Finora reporting follows several financial correctness rules:

- aggregate amounts are currency-scoped;
- unlike currencies are not silently added together;
- no exchange rate is invented or fetched automatically;
- transaction splits are used for category allocation when present;
- local calendar dates are converted to UTC through shared date-boundary logic;
- current month/year comparison stops at today rather than including future-dated rows;
- monetary arithmetic uses checked integer minor units;
- chart meaning has a text/tabular equivalent;
- signed net charts use a real zero baseline;
- privacy mode masks monetary rows and suppresses quantitative chart magnitude.

## Report date range

The Reports screen accepts a local From date and Through date. The selected inclusive local calendar range is converted to a UTC start-inclusive/end-exclusive interval with `LocalDateRange`.

A Through date earlier than the From date is rejected.

## Reporting currency

The configured default currency acts as the reporting currency for aggregate reports that require one currency. Finora displays an explicit notice that unlike currencies are not automatically converted.

Account, budget, recurring, and savings rows that naturally belong to another currency retain their own currency instead of being relabeled as the reporting currency.

## Spending by category

Category spending includes expense transactions in the reporting currency.

When a transaction has splits, split allocations are used. Otherwise the transaction category is used. Uncategorized values are retained as an explicit bucket.

## Income versus expense

Income/expense reporting excludes transfer movement and calculates:

- positive non-transfer values as income;
- negative non-transfer magnitude as expense.

The UI derives net from checked income minus expense.

## Account balance trends

Account balance trend reports are returned per account with the account's own currency.

Trend boundaries use local calendar meaning. Short periods can emit daily points; longer periods use month-end style boundaries according to the current implementation.

The balance is opening balance plus checked transaction movement before each boundary.

## Budget performance

Budget performance uses `BudgetPeriodPolicy.TryResolve` for the selected date.

For each active budget, the report exposes:

- budget name;
- currency;
- effective planned amount;
- actual spending;
- variance.

Category/subcategory budget actuals respect recursive descendant categories and transaction splits.

## Merchant / payee report

Merchant reporting groups non-transfer transaction activity by normalized merchant/payee label within the selected reporting currency and date range.

Rows expose transaction count, expense, and income.

Blank merchant/payee values are represented as an explicit unknown group rather than silently dropped.

## Monthly comparison

Monthly comparison returns a configured trailing number of local calendar months.

For the current month, the query ends at today. Future-dated imported rows are not counted before their local date arrives.

Rows expose year, month, income, expense, and net.

The default Reports presentation currently shows a 12-month net comparison.

## Yearly comparison

Yearly comparison returns a configured trailing number of calendar years.

For the current year, the query ends at today rather than December 31. This prevents future-dated rows from entering current-year totals early.

Rows expose year, income, expense, and net.

The default Reports presentation currently shows a five-year comparison.

## Recurring obligations

The recurring-obligation report exposes non-archived rule information:

- rule name;
- transaction type;
- lifecycle status;
- amount;
- currency;
- next due date;
- optional end date.

This is obligation state, not proof that a transaction has been paid.

## Savings progress

Savings reporting recalculates current progress from starting amount plus checked contribution history.

Rows expose:

- goal name;
- currency;
- current amount;
- target amount;
- progress percentage;
- target date;
- completion state.

Invalid running histories fail closed rather than being silently normalized by the report.

## Tag reporting

Tag reporting is provided by the category/tag service and requires explicit currency scope. Values for the same tag in different currencies are not merged into a false total.

## Chart behavior

The current `ReportBarChartView` is dependency-free MAUI drawing.

Its scale supports positive and negative values around zero. When data contains both signs, positive bars extend above the zero baseline and negative bars extend below it.

Every chart is paired with text or tabular values so the chart is not the only carrier of financial meaning.

When amounts are hidden, quantitative chart data is withheld so bar magnitude cannot leak a hidden value.

## Privacy-aware report display

When privacy mode or hide-on-launch is enabled:

- formatted monetary cells become `••••`;
- report summaries state that amounts are hidden;
- category/income-expense textual summaries do not include hidden values;
- quantitative chart collections are cleared/suppressed;
- non-monetary labels, counts, statuses, dates, and currency context may remain visible.

## CSV import overview

CSV import is explicit-user-only. The user selects a file through the system picker and reviews mapping/validation before committing accepted rows.

The importer is designed to avoid silent guessing.

## CSV limits and encoding

Current importer controls include:

- UTF-8 validation;
- bounded file size;
- bounded row count;
- explicit header mapping;
- preview before write;
- transactional import.

The current documented release boundary uses 50 MB / 100,000 rows as the importer limits exposed by the project documentation/source line.

## Required CSV mapping

Required mapping includes:

- Date;
- Type;
- Amount;
- Account.

## Optional CSV mapping

Optional fields include:

- Currency;
- Category;
- Merchant/payee;
- Note;
- Payment method;
- Manual location;
- Transfer group;
- Counterparty account;
- Tags.

## Amount modes

The import UI supports an option indicating that amount values are already integer minor units.

When values are major units, Finora converts them with decimal arithmetic and currency-specific precision. JPY-style zero-decimal and KWD-style three-decimal behavior has explicit regression coverage.

`long.MinValue` is rejected before any sign normalization that would require unsafe negation.

## Account and category resolution

Import supports:

- account lookup from mapped value;
- fallback account for blank/unknown account rows;
- optional creation of missing categories;
- validation that referenced accounts/categories are usable.

## Duplicate protection

Duplicate skipping can detect likely duplicates against existing data and protects against duplicates repeated within the same import batch.

Duplicate handling does not silently delete existing finance records.

## Transfers in CSV

Transfer import requires paired/counterparty information sufficient to validate the linked transfer model. Transfer rows must respect same-currency account requirements and balanced pairing.

## Tags in CSV

Mapped tag data can be linked to imported transactions. Transfer rows use the same validated tag application path when supported by the importer.

## Preview and invalid rows

The import preview surfaces parsed fields plus row-level validation errors. Invalid-row count is tracked once per invalid row rather than double-counting separate parse branches.

## Transactional import

Accepted rows are persisted transactionally so a failing commit does not intentionally leave an arbitrary partially imported batch.

## CSV export

CSV export is generated locally from supported selected/all transaction workflows. Export is user-triggered and does not automatically upload anywhere.

## PDF export

PDF export is generated locally by the dependency-free project implementation and supports multipage output for larger transaction sets.

## Share/save boundary

Generated CSV/PDF files may be written to Finora cache before invoking system share/save UI.

Finora's startup temporary-artifact cleanup removes only known managed share-copy patterns after the grace period. It does not treat exported files as durable system-of-record finance storage.

Once the user saves/shares a file to another application/location, the destination controls retention/security. Finora cannot automatically revoke that copy.

## Release validation

Before release, verify with synthetic data:

- local-date boundaries around timezone offsets/DST-capable zones;
- mixed-currency isolation;
- split/category allocation;
- negative net chart rendering;
- privacy chart suppression;
- future-dated row exclusion in current month/year;
- import limits/encoding/quoted CSV behavior supported by parser;
- currency precision;
- duplicate protection;
- transfer-pair import;
- transactional failure rollback;
- CSV/PDF export/open/share behavior on each target platform.