# Finora Data Flow

This document explains how data moves through the current Finora 0.2.0 local-first application.

## 1. High-level flow

```text
User / OS input
      ↓
MAUI Page + ViewModel
      ↓
Application contract
      ↓
Infrastructure service / domain rules
      ↓
EF Core SQLite and/or app-private files
```

Platform-only operations such as system pickers, sharing, notifications, secure storage, biometrics, and capture protection stay in the App/platform layer.

## 2. Startup flow

The normal startup boundary is intentionally ordered because database/attachment recovery must finish before finance UI consumes persisted data.

Conceptually:

1. MAUI application and dependency injection are created.
2. Application culture/settings are normalized/applied.
3. Database initialization/migration runs.
4. Interrupted restore recovery is resolved.
5. Temporary managed cache cleanup can run best-effort.
6. notification/remainder state can be reconciled according to app lifecycle.
7. onboarding/lock/finance navigation is selected.
8. Dashboard/finance pages load data through services.

Recovery is not deferred until after finance navigation because a committed database with an unfinished attachment-tree swap could expose mismatched cross-resource state.

## 3. Settings flow

Most ordinary UI preferences are stored through MAUI `Preferences` using `IAppSettingsService`.

Examples:

- default currency;
- locale;
- financial month start;
- privacy mode;
- hide amounts on launch;
- theme;
- reduced motion;
- backup reminder state;
- onboarding completion;
- auto-lock duration;
- local premium demo;
- notifications;
- biometric preference;
- sensitive-screen protection;
- receipt quality;
- larger interface;
- default account/type;
- Dashboard card preferences;
- last backup timestamp.

PIN verifier material is different: small verifier/salt values use OS secure storage, while lockout/enabled state uses bounded local preferences as implemented by the current app-lock service.

## 4. Account creation/update flow

```text
Accounts page/detail
  → parse major-unit input with decimal
  → convert using Money/CurrencyMinorUnits
  → validate DomainRules
  → account management/finance store
  → EF Core FinoraDbContext
  → persistence-boundary validation
  → SQLite transaction/write
  → reload account summary
```

Account currency changes are rejected once dependent transaction/recurrence relationships make the currency part of historical finance truth.

## 5. Normal transaction flow

```text
Transaction UI
  → local date/time + decimal amount
  → UTC timestamp + integer minor units
  → TransactionFactory/domain validation
  → IFinanceStore or maintenance service
  → account/category relationship validation
  → transaction/revision write
  → EF persistence validation
  → SQLite commit
```

For edits, the maintenance service creates a pre-change revision record before replacing ordinary transaction values.

## 6. Transfer flow

Transfers are not routed through the normal one-row transaction save path.

```text
Transfer UI
  → validate distinct source/destination + positive magnitude
  → require same currency
  → dedicated transfer service/store method
  → create two reciprocal transaction rows
  → one DB transaction
  → commit pair atomically
```

Later edit/delete/restore operations also preserve the pair.

## 7. Split transaction flow

Splits are validated as a set:

1. parent transaction sign/type is established;
2. each split is nonzero and has the parent sign;
3. categories are resolved/validated;
4. checked split sum must equal parent amount;
5. transaction and splits persist together.

Reporting uses split allocations instead of attributing the full parent amount again.

## 8. Transaction search flow

```text
Search/filter UI
  → optional text/account/category/date inputs
  → LocalDateRange converts local dates to UTC bounds
  → FinanceStore query excludes soft-deleted rows
  → matching rows ordered from persistence
  → ViewModel applies selected sort
  → display first 50
  → Load more appends next 50 from loaded result
```

Passive monetary display uses currency-aware privacy conversion at the UI boundary.

## 9. Reconciliation flow

```text
Account + statement balance + local statement date
  → decimal-to-minor conversion
  → local statement date → UTC end-exclusive boundary
  → reconciliation service computes book balance
  → checked difference
  → preview
  → optional explicit adjustment
  → reconciliation history + adjustment in atomic workflow
```

No difference is silently hidden.

## 10. Budget flow

```text
Budget definition
  → DomainRules validation
  → explicit/generated period resolution through BudgetPeriodPolicy
  → spending query restricted to budget currency/window
  → descendant/split allocation
  → checked actual/variance
```

Custom budgets require explicit periods. Explicit-period replacement is transactional.

## 11. Savings goal flow

```text
Goal input
  → decimal major-unit conversion
  → target/starting validation
  → persisted goal

Contribution/withdrawal
  → selected goal currency conversion
  → optional linked transaction validation
  → checked running history
  → persisted contribution
  → recompute progress/completion
```

The reporting service independently reconstructs current progress from valid contribution history.

## 12. Recurring flow

```text
Recurring rule
  → validate frequency/account/category/currency/date rules
  → persist rule

Due processing
  → determine due date(s)
  → persist unique occurrence(s)
  → NO finance transaction yet

User marks paid/partial
  → validate occurrence state
  → create one normal transaction or linked transfer pair
  → link generated transaction to occurrence
  → update occurrence state
```

Pause/archive stops future generation without deleting occurrence history. Resume revalidates dependencies.

## 13. Report flow

```text
Local report date range + reporting currency
  → LocalDateRange UTC bounds
  → AdvancedReportService
  → currency-scoped SQL/data aggregation
  → checked finance arithmetic
  → display DTO formatting
  → text/table + optional chart
```

Account/budget/recurring/savings rows retain their own currency where naturally applicable. Aggregate reports never combine unlike currencies without an explicit future FX design.

Monthly/yearly comparison converts persisted timestamps back to local calendar dates before grouping and stops current periods at today.

## 14. CSV import flow

```text
System file picker
  → selected stream/file
  → UTF-8/size/row validation
  → parse headers
  → user mapping
  → preview/row validation
  → currency-aware amount conversion
  → account/category/tag/transfer resolution
  → duplicate protection
  → transactional DB import
```

Invalid rows are surfaced; Finora does not intentionally guess missing finance semantics silently.

## 15. CSV/PDF export flow

```text
User export action
  → query selected/all supported transactions
  → local CSV/PDF generation
  → Finora cache share-copy file
  → system share/save UI
  → user-selected destination
```

Once a destination receives the file, Finora no longer controls that copy.

Stale managed cache copies are eligible for best-effort startup cleanup after the grace period.

## 16. Receipt attachment flow

```text
System picker
  → user-selected source
  → content/size validation
  → generated safe internal filename
  → app-private attachment path validation/no-link checks
  → asynchronous copy
  → size + SHA-256
  → attachment metadata in SQLite
```

Open/delete/backup/integrity operations resolve the stored relative path through the same confinement/no-link policy.

## 17. Encrypted backup creation flow

```text
User enters masked backup password
  → IBackupService (CrashSafeBackupService registration)
  → serialize backup operation with semaphore
  → read validated finance graph + attachment bytes
  → validate IDs/relationships/path/size/hash
  → serialize snapshot
  → PBKDF2-SHA256 derive key
  → AES-GCM encrypt/authenticate
  → local backup metadata/audit
  → encrypted bytes returned to UI
  → write temporary cache copy
  → system share/save UI
  → clear managed sensitive byte buffers where practical
```

No automatic upload is performed.

## 18. Encrypted restore flow

```text
User selects backup + enters password
  → preview decrypt/authenticate/validate
  → user confirms restore
  → crash-safe wrapper snapshots current attachment tree
  → durable recovery journal + pending DB marker
  → raw backup service stages new attachment tree
  → database replacement transaction
  → attachment swap/rollback handling
  → DB commit
  → wrapper/recovery finalizes journal/marker/filesystem state
```

If the process exits mid-restore, startup recovery decides whether old or new attachment state belongs with the database based on durable marker/journal state.

## 19. Integrity diagnostics flow

```text
Developer/user-triggered integrity check
  → SQLite integrity/FK checks
  → finance graph checks
  → attachment metadata/file checks
  → sanitized issue codes + affected counts
  → optional sanitized report export
```

The report must not include account names, merchants/payees, notes, amounts, manual location text, receipt filenames/contents, PINs, backup passwords, keys, or signing secrets.

## 20. Privacy logger flow

```text
Unexpected exception/event
  → Safe error handling / AppExceptionCoordinator / AsyncCommand hook
  → IPrivacyLogger
  → sanitized bounded event token + exception type
  → bounded local diagnostic file
```

Exception message, stack trace, arbitrary finance properties, and raw provider/file paths are deliberately not serialized.

## 21. Local notification flow

```text
Reminder source (backup/budget/recurrence)
  → ReminderCoordinator
  → ILocalNotificationService
  → persist/dedupe schedule state
  → IPlatformNotificationGateway
  → OS local notification API
```

For replacement, the new OS reminder is accepted before old DB state is disabled; stale OS cancellation follows database commit. Reconciliation retries drift cleanup.

Notification payloads remain generic.

## 22. Privacy display flow

Passive monetary display surfaces route minor units plus currency through a shared privacy-aware formatter or ViewModel equivalent.

When privacy/hide-on-launch is active:

- passive money text becomes `••••`;
- report summaries omit amounts;
- quantitative report charts are suppressed;
- non-monetary labels/status/date context can remain visible.

This prevents a chart or raw minor-unit label from bypassing a hide-amount setting.

## 23. Full finance reset flow

```text
Settings typed confirmation
  → IFinanceDataResetService
  → one transactional deletion of supported finance records
  → preserve intended app preferences/schema metadata
  → commit
  → best-effort attachment orphan cleanup
  → refresh UI state
```

Exported/shared copies outside Finora are not affected.

## 24. Synthetic sample reset flow

The hidden developer action requires a separate typed confirmation and intentionally destroys current finance records before creating deterministic synthetic sample content. It is a development/testing tool, not a backup or merge operation.