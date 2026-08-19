# Export artifact verification

Finora includes `scripts/verify_export_artifact.py` for privacy-safe structural checks of CSV and PDF exports.

The verifier is intended for release QA, regression testing, and support diagnostics with synthetic data. It never prints CSV row values, PDF contents, or the artifact path.

## Why this exists

A successful “export” action should produce a usable artifact, not merely a file with the expected extension. The verifier provides an independent check for common failures such as:

- empty/truncated artifacts,
- invalid CSV encoding,
- duplicate CSV headers,
- inconsistent CSV row widths,
- missing configured CSV columns,
- unexpectedly small row counts,
- malformed PDF envelope/EOF markers,
- copy corruption detected by SHA-256.

It does not make currency conversions or alter the export.

## Verify a CSV export

```bash
python scripts/verify_export_artifact.py path/to/export.csv
```

The tool reports only structural counts and diagnostics:

- byte size,
- SHA-256,
- row count,
- column count,
- generic diagnostic codes/messages.

CSV data rows are streamed rather than loaded into memory as one large document.

## Validate configured columns

Use `--require-column` for each column that the selected export configuration is expected to include:

```bash
python scripts/verify_export_artifact.py path/to/export.csv \
  --require-column Date \
  --require-column Type \
  --require-column Amount \
  --require-column Account \
  --require-column Currency
```

If a configured column is absent, the verifier reports `csv_missing_required_column` without echoing the requested column value in the diagnostic payload. This keeps arbitrary command-line values out of machine reports.

## Validate a minimum row count

```bash
python scripts/verify_export_artifact.py path/to/export.csv --min-rows 500
```

This is useful when a deterministic fixture is expected to export a known number of rows.

Remember that filters, deleted/archive behavior, transaction scope, and date ranges can intentionally change the exported row count. Configure the expected minimum for the exact test scenario.

## Verify a PDF export

```bash
python scripts/verify_export_artifact.py path/to/export.pdf
```

PDF verification is intentionally structural and memory-efficient. It checks:

- the `%PDF-` header,
- the trailing `%%EOF` marker,
- minimum artifact size,
- optional SHA-256 matching.

It does not parse financial text, render pages, or claim visual correctness. Native/manual PDF review remains necessary for pagination, fonts, accessibility, Hindi text, tables, and amount formatting.

## Force a format

If a test artifact does not use `.csv` or `.pdf`, specify the format explicitly:

```bash
python scripts/verify_export_artifact.py artifact.bin --format csv
```

or:

```bash
python scripts/verify_export_artifact.py artifact.bin --format pdf
```

## SHA-256 integrity check

Record a digest for a known artifact, then verify a copied artifact:

```bash
python scripts/verify_export_artifact.py copied-export.csv \
  --expected-sha256 <RECORDED_64_CHARACTER_SHA256>
```

A mismatch reports `sha256_mismatch` and returns a non-zero exit code.

A digest identifies a specific byte sequence. Treat it as operational metadata; the verifier never substitutes a digest check for validating the actual export scenario.

## JSON output

```bash
python scripts/verify_export_artifact.py path/to/export.csv --json
```

Example shape:

```json
{
  "passed": true,
  "format": "csv",
  "sizeBytes": 12345,
  "sha256": "<64-character digest>",
  "rowCount": 250,
  "columnCount": 13,
  "diagnostics": []
}
```

The payload intentionally excludes:

- file path/name,
- account names,
- merchant/payee values,
- transaction amounts,
- notes/locations/tags,
- CSV rows,
- PDF text.

## CSV diagnostic codes

Current CSV-related codes include:

```text
csv_blank_header
csv_column_count_mismatch
csv_duplicate_header
csv_empty
csv_min_rows_not_met
csv_missing_required_column
csv_not_utf8
csv_parse_error
```

## PDF diagnostic codes

Current PDF-related codes include:

```text
pdf_missing_eof
pdf_missing_header
```

## Shared artifact diagnostic codes

```text
missing_file
not_a_file
read_error
sha256_mismatch
too_small
```

Automated checks should key off diagnostic codes rather than English message text.

## Recommended CSV export QA

With deterministic synthetic data:

1. Generate a fixture using `scripts/generate_sample_finance_csv.py`.
2. Import it into a disposable Finora database.
3. Select the exact date/filter/export configuration being tested.
4. Export CSV through Finora.
5. Verify required columns and expected row count with this script.
6. Open the CSV in at least one independent consumer and verify representative synthetic rows.
7. Verify currency context is preserved and Finora has not silently converted mixed-currency rows.
8. Verify selected-only exports contain only the intended synthetic selections.
9. Repeat with privacy mode enabled and confirm export behavior matches the documented product policy rather than assuming UI masking automatically changes explicit exports.
10. Delete disposable artifacts after testing when they are no longer required.

## Recommended PDF export QA

1. Export a representative synthetic report/transaction set.
2. Run the structural verifier.
3. Open/render the PDF on the target platform.
4. Check page boundaries, headings, tables, negative/positive monetary values, long merchant/category names, and empty-state behavior.
5. Test English and Hindi output where the export feature is localized.
6. Test larger datasets for pagination and memory behavior.
7. Confirm no hidden/deleted records are included unless explicitly part of the export contract.

## CI

`.github/workflows/export-artifact.yml`:

- runs the export verifier unit tests,
- generates a 1,000-row deterministic CSV fixture,
- validates expected canonical columns and row count,
- validates a synthetic PDF envelope fixture.

No real financial export is committed or uploaded by this workflow.

## Boundary

A passing artifact verifier does **not** prove:

- the correct records were selected,
- every financial value is correct,
- PDF pages render perfectly,
- CSV values match database values,
- permissions/share-sheet behavior works on a native platform,
- accessibility metadata is present in PDF,
- export UI configuration itself is intuitive.

Those require application integration tests and native/manual release validation.
