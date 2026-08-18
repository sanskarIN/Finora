# Finora Performance Benchmarking

Last updated: **2026-08-18**

This guide documents the reproducible synthetic performance harness in `tools/Finora.Performance`.

Performance measurements are **observational evidence**, not correctness guarantees. The harness fails on invalid finance state, failed operations, invalid encrypted backup output, failed CSV round trips, failed encrypted restore round trips, or integrity errors. It does not fail merely because a timing is slower than an arbitrary threshold.

## Goals

The harness exists to make large-data behavior measurable without reading real user finance data.

It can measure:

- populated-database startup initialization;
- transaction-history first-page paging;
- deep offset paging;
- common and selective free-text search;
- amount-sorted paging;
- income/expense reporting;
- category spending;
- merchant/payee reporting;
- account balance trends;
- budget performance;
- recurring obligations;
- savings progress;
- full CSV export;
- full CSV import into an isolated synthetic database;
- full PDF export;
- encrypted backup creation;
- encrypted backup restore with restored graph-count verification;
- full data-integrity checking.

## Synthetic dataset

The harness generates app-private temporary test data only.

The configurable graph includes:

- four INR accounts;
- current default categories created by the normal database initializer;
- deterministic-pattern income and expense transactions;
- account/category relationships;
- merchant, note, payment-method and manually entered location search text;
- budgets;
- savings goals;
- recurrence rules;
- optional receipt files with matching SHA-256 metadata.

Transaction seeding is batched so a 100,000-row run does not require building the complete input graph in memory at once.

The generated data is deliberately synthetic and contains no imported user finance content.

## Supported dataset sizes

The primary comparison sizes are:

- 10,000 transactions;
- 50,000 transactions;
- 100,000 transactions.

The CLI allows other bounded sizes for targeted investigation. The normal CI smoke run uses 10,000 transactions.

## Run locally

Restore and build:

```bash
dotnet restore tools/Finora.Performance/Finora.Performance.csproj
dotnet build tools/Finora.Performance/Finora.Performance.csproj -c Release --no-restore
```

Run the standard 10k smoke profile:

```bash
dotnet run --project tools/Finora.Performance/Finora.Performance.csproj \
  -c Release --no-build -- \
  --transactions 10000 \
  --attachments 25 \
  --recurrences 50 \
  --budgets 25 \
  --goals 25 \
  --operations startup,history,reports,integrity \
  --output artifacts/performance/local-10k.json
```

Run a complete 10k round-trip profile, including CSV import/export, PDF export and encrypted backup create/restore:

```bash
dotnet run --project tools/Finora.Performance/Finora.Performance.csproj \
  -c Release --no-build -- \
  --transactions 10000 \
  --attachments 25 \
  --recurrences 50 \
  --budgets 25 \
  --goals 25 \
  --operations all \
  --output artifacts/performance/local-10k-all.json
```

Run a complete 50k profile:

```bash
dotnet run --project tools/Finora.Performance/Finora.Performance.csproj \
  -c Release --no-build -- \
  --transactions 50000 \
  --attachments 100 \
  --recurrences 250 \
  --budgets 100 \
  --goals 100 \
  --operations all \
  --output artifacts/performance/local-50k.json
```

Run a complete 100k profile:

```bash
dotnet run --project tools/Finora.Performance/Finora.Performance.csproj \
  -c Release --no-build -- \
  --transactions 100000 \
  --attachments 100 \
  --recurrences 250 \
  --budgets 100 \
  --goals 100 \
  --operations all \
  --output artifacts/performance/local-100k.json
```

Use `--help` for the complete option list.

## Operations

`--operations` accepts a comma-separated list of:

- `startup`;
- `history`;
- `reports`;
- `csv`;
- `pdf`;
- `backup`;
- `integrity`.

`all` selects every operation.

The `csv` operation records both `export.csv.all` and `import.csv.all`. Import runs against a fresh isolated SQLite fixture created under the synthetic benchmark root, so it cannot double the primary benchmark dataset or contaminate later measurements.

The `backup` operation records both `backup.create.encrypted` and `backup.restore.encrypted`. The restore uses only the synthetic benchmark database/receipt tree and verifies transaction and attachment counts after restore.

`--iterations` accepts 1–20 repetitions. Expensive full export/import/backup/restore runs should normally start at one iteration.

## Output format

The harness writes indented JSON containing:

- product/harness identity;
- .NET runtime description;
- operating-system description;
- process architecture;
- processor count;
- UTC start/completion timestamps;
- dataset row/file counts;
- database and attachment byte sizes where available;
- selected operations;
- iteration count;
- per-measurement elapsed milliseconds;
- managed-heap observations before/after;
- process working-set observations before/after;
- output byte counts where applicable;
- item counts where applicable;
- explicit timing/data/paging policy notes.

Do not compare results from dissimilar runner hardware as if they were the same benchmark environment.

## Correctness checks during measurement

The harness does more than start a stopwatch.

Examples of correctness gates include:

- transaction history must report the expected visible transaction count;
- a valid deep page must not unexpectedly become empty;
- CSV/PDF/backup output must be non-empty;
- CSV import must succeed into its isolated database with the exact expected transaction count and no skipped/invalid rows for the generated export;
- encrypted backup restore must succeed and preserve the expected transaction and attachment counts;
- the full synthetic dataset must pass `DataIntegrityService`.

A failed correctness gate returns a nonzero process exit code.

## Memory interpretation

Managed heap and working set are observations around each measured operation. They are useful for spotting large regressions but are not exact per-operation allocation counters.

Reasons include:

- asynchronous continuations may execute on different threads;
- runtime/JIT caches persist;
- SQLite native memory is not identical to managed heap;
- operating systems account working-set pages differently;
- garbage collection timing affects post-operation heap size.

Use repeated measurements and comparable runner environments before drawing conclusions.

## Transaction paging interpretation

The history benchmark uses the production `ITransactionHistoryStore`.

Measured history scenarios include:

- first 50-row page;
- a page beginning around the middle of the dataset;
- broad free-text search;
- selective merchant search;
- amount-high-to-low page.

Offset paging is measured against a **fixed synthetic dataset** with no concurrent inserts/deletes. These numbers do not claim snapshot-isolated pagination across concurrent mutations.

## CSV round-trip interpretation

CSV export is measured against the primary synthetic benchmark database. For import measurement, the already-generated CSV is fed to a freshly initialized isolated database containing matching synthetic accounts and the normal default categories.

The import measurement includes CSV parsing, mapping validation, transaction creation, database transaction work and a final transaction-count correctness query. Fixture creation and the preparatory export used as import input are outside the import stopwatch so the result represents the import path rather than setup.

The generated benchmark data intentionally contains no transfer rows or user-supplied files, so this is a large-volume import benchmark, not a substitute for the broader CSV import correctness test suite.

## Backup round-trip interpretation

The backup measurement writes the encrypted synthetic backup to a temporary file under the benchmark root, then measures production restore against the same synthetic finance graph. Restore correctness verifies the expected transaction and attachment counts before later operations continue.

This exercises the real encrypted backup serialization/encryption and restore graph replacement path. It does not emulate process interruption, storage exhaustion, removable-media failure, mobile document-provider behavior, or signed-package filesystem constraints; those remain separate recovery/native validation work.

## CI smoke gate

`.github/workflows/ci.yml` includes `Performance smoke (10k)`.

It:

1. restores the performance project;
2. builds it in Release under the repository warnings-as-errors policy;
3. seeds 10,000 synthetic transactions plus bounded supporting records;
4. runs startup/history/report/integrity scenarios;
5. uploads the JSON result as `performance-smoke-10k`.

The smoke build compiles the full harness, including CSV import and encrypted backup restore benchmark paths. The current CI smoke command deliberately keeps its executed operation list bounded; use an `all` profile for runtime round-trip evidence covering those heavier paths.

The smoke job is a correctness/reproducibility gate. It deliberately does not fail on an arbitrary elapsed-time limit other than the overall CI job timeout.

## On-demand large benchmark workflow

`.github/workflows/performance.yml` supports manually selected:

- 10,000 transactions;
- 50,000 transactions;
- 100,000 transactions;
- operation list;
- 1, 2 or 3 iterations.

The workflow uses synthetic data and uploads JSON evidence for 30 days.

A complete release/performance review should run comparable 10k, 50k and 100k `all` profiles on the same runner class before claiming a trend or regression.

## Benchmark hygiene

For meaningful comparison:

1. use Release configuration;
2. keep runtime/SDK versions comparable;
3. keep runner OS/architecture comparable;
4. keep dataset shape and operation list identical;
5. avoid unrelated heavy workloads on local machines;
6. record the exact Finora commit SHA with the JSON artifact;
7. retain multiple runs when investigating noise;
8. distinguish seeding/fixture preparation duration from product-operation measurements;
9. never insert real finance data into benchmark fixtures.

## Performance change policy

When a benchmark reveals a problem:

1. identify the specific operation and dataset size;
2. preserve a result artifact showing the issue;
3. add or adjust a targeted benchmark scenario if needed;
4. profile/query-inspect before changing architecture;
5. preserve finance/privacy correctness while optimizing;
6. rerun the same profile after the change;
7. record both evidence points and runner differences.

Do not trade away:

- signed minor-unit money correctness;
- transfer atomicity;
- local-calendar correctness;
- schema validation;
- encrypted-backup validation;
- privacy boundaries;
- integrity checking

merely to make a benchmark number smaller.

## Release boundary

A fast benchmark does not prove:

- signed package correctness;
- native UI responsiveness on every device;
- low-memory device behavior;
- battery/thermal behavior;
- accessibility;
- actual process-kill recovery;
- store approval;
- absence of undiscovered defects.

Performance artifacts complement, rather than replace, the release evidence in `CI_EVIDENCE.md`, `NATIVE_VALIDATION_MATRIX.md`, `../NEXT_STEPS.md`, and `../releases/RELEASE_CHECKLIST.md`.
