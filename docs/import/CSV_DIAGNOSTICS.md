# Privacy-safe CSV diagnostics

Finora includes `scripts/diagnose_finora_csv.py`, an offline developer/QA preflight for Finora-style CSV files. It is designed to identify structural import problems without copying transaction values into logs.

This tool complements Finora's in-app CSV preview and transactional validation. It does **not** replace the application's importer and deliberately avoids guessing values that the application itself should validate.

## Privacy model

The diagnostics tool reports:

- diagnostic severity,
- diagnostic code,
- CSV row number where applicable,
- a generic explanation,
- aggregate counts.

It intentionally does **not** include:

- merchant/payee values,
- account names,
- transaction amounts,
- notes,
- locations,
- tags,
- complete source rows.

Machine-readable JSON output follows the same rule. This makes the report safer to attach to CI logs or sanitized bug reports when synthetic/test data is used.

Do not assume any diagnostic log is suitable for sharing when the file itself or surrounding command line contains sensitive paths or information. Prefer synthetic fixtures for reproducible reports.

## Basic usage

```bash
python scripts/diagnose_finora_csv.py path/to/import.csv
```

Exit codes:

- `0`: no structural errors were detected; warnings may still be present,
- `1`: at least one structural error was detected,
- argument/setup failures use a non-success process exit as reported by Python/argparse.

Warnings never modify or delete data.

## Minor-unit mode

When the CSV `Amount` column already contains integer minor units, match Finora's import setting:

```bash
python scripts/diagnose_finora_csv.py path/to/import.csv --minor-units
```

In this mode decimal amount text is reported as `minor_units_not_integer`.

## JSON output

```bash
python scripts/diagnose_finora_csv.py path/to/import.csv --json
```

The payload contains only sanitized summary information:

```json
{
  "passed": true,
  "rowCount": 250,
  "errorCount": 0,
  "warningCount": 0,
  "duplicateGroupCount": 0,
  "transferGroupCount": 8,
  "diagnostics": [],
  "diagnosticsTruncated": false
}
```

Limit how many diagnostics are included:

```bash
python scripts/diagnose_finora_csv.py path/to/import.csv --json --max-diagnostics 25
```

The accepted range is 1–10,000 diagnostics.

## Checks performed

### File/header checks

The preflight detects:

- an empty CSV,
- unreadable/invalid text/CSV content,
- duplicate header names,
- missing required headers.

Required canonical headers for this helper are:

```text
Date
Type
Amount
Account
```

The application importer may expose mapping UI for files whose original column names differ. Use the application's mapping/revalidation flow when testing non-canonical source headers.

### Row structure

The preflight detects:

- row/header column-count mismatch,
- blank required values,
- blank rows as warnings.

### Date

The helper expects canonical ISO dates in `YYYY-MM-DD` form. Non-ISO dates receive `invalid_iso_date`.

This is intentionally stricter than a potentially culture-aware application import path. Its purpose is to produce deterministic QA fixtures and diagnostics, not to redefine every date format the application may support.

### Transaction type

Recognized canonical values are:

- `Expense`
- `Income`
- `Transfer`
- `Refund`
- `Adjustment`

Matching is case-insensitive.

### Amount

The helper checks that amount values are finite decimal numbers. A zero amount is a warning so a tester can decide whether the row is intentional.

With `--minor-units`, amounts must also be integer text.

### Currency

When a Currency column/value is present, the helper requires a three-letter alphabetic code. It does not perform exchange-rate conversion and does not silently substitute another currency.

### Possible duplicates

A privacy-safe fingerprint is built in memory from normalized structural fields. When the same fingerprint appears more than once, the tool reports a `possible_duplicate_group` warning with the first row number and group size.

The values used to calculate that fingerprint are **not** emitted in the diagnostic output.

Possible duplicates are warnings only. Nothing is deleted or changed automatically.

### Transfers

When transfer columns are present, the helper can warn about:

- a blank `TransferGroup`,
- a blank `CounterpartyAccount`,
- transfer groups whose row count is not two.

The last rule is aimed at Finora's deterministic paired QA fixtures. Treat it as a review signal, not permission to alter production data automatically.

## Diagnostic codes

Current codes include:

```text
blank_required_value
blank_row
column_count_mismatch
counterparty_account_blank
duplicate_header
empty_file
file_read_error
invalid_amount
invalid_currency_code
invalid_iso_date
minor_units_not_integer
missing_required_header
possible_duplicate_group
transfer_group_blank
transfer_group_cardinality
unknown_transaction_type
zero_amount
```

Use codes, not English message text, when automating CI decisions.

## Recommended workflow with deterministic fixtures

Generate a reproducible fixture:

```bash
python scripts/generate_sample_finance_csv.py artifacts/import_fixture.csv --rows 10000 --seed 20260819
```

Run diagnostics:

```bash
python scripts/diagnose_finora_csv.py artifacts/import_fixture.csv
```

Then validate the same file through Finora's in-app CSV preview/mapping flow. A clean script result is not a substitute for the app's own validation.

For minor-unit testing:

```bash
python scripts/generate_sample_finance_csv.py artifacts/import_fixture_jpy.csv \
  --rows 1000 \
  --seed 20260819 \
  --currency JPY \
  --minor-units

python scripts/diagnose_finora_csv.py artifacts/import_fixture_jpy.csv --minor-units
```

## CI

`.github/workflows/csv-diagnostics.yml`:

1. runs diagnostic-engine unit tests,
2. runs deterministic-generator tests,
3. generates a representative major-unit fixture,
4. diagnoses it,
5. generates a JPY minor-unit fixture,
6. diagnoses that fixture in minor-unit mode.

This verifies the two developer tools interoperate without requiring a Finora database or real finance data.

## Important boundary

The diagnostic helper is intentionally structural and privacy-preserving. It does not:

- write to the Finora database,
- resolve/merge user accounts,
- create categories,
- apply exchange rates,
- delete duplicates,
- reconcile balances,
- restore backups,
- make financial decisions.

Those behaviors remain inside Finora's application services where transactional guarantees, revision history, privacy settings, and domain rules can be enforced.
