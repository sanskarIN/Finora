# Budgets, Savings Goals, and Recurring Items

This document describes the planning workflows implemented in the current Finora 0.2.0 source line.

## Budgets

Finora supports three budget kinds:

- overall;
- category;
- subcategory.

Supported cadence behavior includes:

- weekly;
- monthly;
- custom explicit periods.

Budgets carry currency, planned amount, warning threshold, optional category relationship, rollover behavior where supported, and persisted/derived period information.

### Shared budget period policy

`BudgetPeriodPolicy` is the source of truth for effective budget windows.

Current semantics:

- generated weekly windows run Monday through Sunday;
- generated monthly windows use calendar months;
- explicit periods take precedence;
- custom-cadence budgets are active only inside explicit configured periods;
- explicit periods cannot overlap;
- rollover changes the effective planned amount only when enabled;
- the resulting effective plan must remain positive;
- replacing explicit periods is transactional so failed replacement does not erase the prior valid period set.

### Actual spending

Budget actuals use expense transactions in the budget currency and exclude transfer movement from spending totals.

Category/subcategory budgets support recursive category descendants where appropriate. If a transaction has splits, budget accounting uses split allocations rather than double-counting the parent amount.

### Warning thresholds

Budget warning thresholds are percentage-based and use checked arithmetic. Notification coordination may create a generic local reminder when the configured condition is met and notifications are enabled.

Stale budget reminders are removed when the source condition no longer requires them.

## Savings goals

Savings goals model a local target and contribution history.

Current fields/behavior include:

- name;
- target amount;
- starting amount;
- currency;
- optional target date;
- icon;
- note;
- contribution/withdrawal history;
- optional linked transaction;
- progress;
- milestones;
- contribution forecast;
- completion state.

### Monetary rules

Target amount must be positive. Starting amount cannot be negative or exceed the target during creation. Contributions and withdrawals use signed integer minor units and checked arithmetic.

Running goal history cannot validly fall below zero.

A linked transaction, when present, must exist and use the goal currency.

### Completion state

New goals initialize completion state from starting progress. Existing data can be safely normalized at startup only when underlying contribution history validates; corrupt history is left for integrity diagnostics rather than silently rewritten.

### Forecast and milestones

The UI can show achieved/next milestone percentages and an approximate monthly contribution needed to reach a future target date.

Forecast amount display respects privacy/hide-on-launch settings.

## Recurring rules

Recurring rules represent repeated obligations or expected transactions.

Supported transaction types include:

- Expense;
- Income;
- Transfer;
- Refund.

Supported frequencies include daily, weekly, monthly, yearly, and custom interval behavior provided by the current domain rule model.

Rule fields include:

- name;
- type;
- frequency;
- interval;
- source account;
- destination account for transfer;
- optional category for non-transfer types;
- amount and currency;
- merchant/payee;
- note;
- start date;
- optional end date;
- next due date;
- grace period;
- reminder lead time;
- lifecycle status.

### Lifecycle

Current rule lifecycle includes Active, Paused, Completed, and Archived states.

- Active rules can prepare due occurrences.
- Paused rules stop future generation while preserving history.
- Resume revalidates rule timing plus active account/category/currency dependencies.
- Completed rules retain history without active generation.
- Archived rules are removed from active rule lists while preserving occurrence history.

### Occurrence-first model

Recurring processing is deliberately occurrence-first.

A due processor creates or maintains a persisted occurrence for a due date. It does **not** automatically create a finance transaction merely because the scheduler ran.

A finance transaction is generated when the occurrence is explicitly marked paid or partially paid.

This prevents repeated startup/scheduler runs from duplicating financial activity.

### Occurrence states

Current occurrence workflow includes:

- Pending;
- Paid;
- PartiallyPaid;
- Skipped;
- Postponed.

Skipped occurrences can be explicitly reopened when valid.

Paid/partial states retain generated transaction/payment information. Repeated full-payment action is idempotent and does not create a second transaction.

A paid occurrence may retain a valid historical postponed date so the actual due history is not destroyed. Unpaid states cannot silently carry incompatible payment data.

### Recurring transfers

A recurring transfer uses the same linked same-currency transfer model as manual transfers. Source and destination accounts must be available and currency-compatible. Generated transfer pairs are validated for reciprocal linkage and equal/opposite amounts.

### Dependency safety

Active recurrence references active/available accounts and valid categories where required. An account used by an active rule cannot be archived until the rule no longer actively depends on it.

Resume fails closed when account/category/currency/end-date dependencies are no longer valid.

### Backlog protection

Recurring processing is bounded so a stale rule cannot generate an unbounded backlog in one operation.

## Notifications

Recurring reminders are local and permission-gated. Notification title/body remain generic because notifications can be visible outside Finora's app lock.

Reminder synchronization cancels stale recurring schedules when a rule is paused, completed, archived, or otherwise no longer active.

## Reporting

Budget performance reports expose planned, actual, variance, and currency using the same budget-period policy.

Recurring-obligation reports expose current non-archived rule name/type/status/amount/currency/next due/end date.

Savings-progress reports expose current checked contribution-derived amount, target, progress, target date, currency, and completion state.

Unlike currencies remain separate. Finora does not invent exchange rates.

## Privacy-mode behavior

Passive budget, savings, recurring, and report amounts use currency-aware formatting and are masked when privacy mode or hide-on-launch is active. Quantitative charts are suppressed where their bar magnitude would reveal hidden amounts indirectly.

## Integrity diagnostics

The integrity checker validates planning relationships including:

- budget configuration and category relationship;
- explicit period overlap/custom cadence state;
- savings contribution running history and completion state;
- linked goal transaction relationship/currency;
- recurrence account/category/currency dependencies;
- duplicate occurrence keys;
- paid/generated transaction state;
- generated recurring transfer integrity.

Sanitized diagnostics return codes/counts, not private finance contents.