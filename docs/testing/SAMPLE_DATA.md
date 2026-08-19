# Deterministic sample finance data

Finora includes `scripts/generate_sample_finance_csv.py` for generating synthetic CSV transactions for import, performance, regression, accessibility, localization, and manual release testing.

The generator is deliberately safe for development environments:

- it does not read Finora databases or user files,
- it does not contact any network service,
- it does not contain real financial records,
- the same arguments produce deterministic output,
- transfer fixtures are emitted as complete paired rows,
- currency minor-unit output respects common 0/3/4-decimal currencies instead of assuming two decimal places.

## Generate the default fixture

From the repository root:

```bash
python scripts/generate_sample_finance_csv.py
```

This writes:

```text
artifacts/sample_finora_transactions.csv
```

The default fixture contains 250 rows and uses a fixed seed. Its default date window starts on January 1, 2024 and spans up to 730 days, keeping the default fixture historical for the 2026 release cycle so Finora's future-transaction validation does not reject synthetic rows simply because of the fixture dates.

## Choose row count and seed

```bash
python scripts/generate_sample_finance_csv.py artifacts/sample_10k.csv --rows 10000 --seed 42
```

Use a committed/documented seed when comparing performance results across branches. Changing the seed changes the generated transaction mix.

The generator accepts from 1 to 1,000,000 rows. Very large fixtures should normally remain generated artifacts rather than committed repository files.

## Choose currency

```bash
python scripts/generate_sample_finance_csv.py artifacts/sample_usd.csv --currency USD
```

Currency codes must be three alphabetic characters. The tool uppercases the output code.

For normal major-unit CSV import testing, amounts are written as decimal values appropriate to the currency's supported display precision.

## Generate minor-unit amounts

To test Finora's **Amount values are already minor units** import option:

```bash
python scripts/generate_sample_finance_csv.py artifacts/sample_minor.csv --minor-units
```

Examples of the intended behavior:

- INR/USD-style currencies: `12.35` becomes `1235` minor units.
- JPY/KRW: `12` remains `12` because these currencies use zero decimal places.
- BHD/KWD/JOD/OMR/TND: three decimal places are preserved.
- CLF: four decimal places are preserved.

Unknown three-letter currency codes use two decimal places for the synthetic fixture. Finora itself remains the source of truth for production currency handling.

## Choose a different date window

```bash
python scripts/generate_sample_finance_csv.py artifacts/sample_2023.csv --start-date 2023-01-01
```

Generated dates are distributed deterministically over the 730-day window beginning at the supplied start date.

When testing the normal transaction importer, choose a start date whose generated window does not extend into the future unless future-date rejection is the behavior being tested.

## Generated columns

The CSV contains these columns:

```text
Date
Type
Amount
Account
Currency
Category
Merchant
Note
PaymentMethod
Location
TransferGroup
CounterpartyAccount
Tags
```

The fixture deliberately contains:

- expense rows,
- income rows,
- paired internal transfers,
- multiple sample accounts,
- categories and merchant/payee values,
- payment methods,
- semicolon-delimited tags.

Synthetic values are visibly named with words such as `Sample`, `Synthetic`, `Demo`, `Test`, `Mock`, or `Fixture` so they are not easily confused with genuine user data.

## Recommended import QA

1. Generate a small fixture, such as 250 rows.
2. Open Finora's CSV Import screen.
3. Map the required Date, Type, Amount, and Account columns.
4. Map optional columns needed for the test.
5. Enable **Amount values are already minor units** only when the fixture was generated with `--minor-units`.
6. Revalidate the CSV structure before import.
7. Review invalid rows and duplicate diagnostics instead of assuming every row is importable.
8. Import into a disposable development database.
9. Verify account balances, transfer pairing, categories/tags, reports, privacy mode, and reconciliation behavior.

Do not import synthetic performance fixtures into a real personal finance database.

## Performance testing

For repeatable performance comparisons, record:

- Finora commit,
- operating system/device,
- row count,
- generator seed,
- currency,
- major-unit/minor-unit mode,
- import options,
- measured operation and timing method.

Example:

```bash
python scripts/generate_sample_finance_csv.py artifacts/sample_100k.csv --rows 100000 --seed 20260819
```

Large fixtures are useful for paging, search, import, duplicate detection, reports, backup/restore, and database performance checks.

## Automated tests

Run the generator tests with:

```bash
python -m unittest discover -s scripts/tests -p "test_generate_sample_finance_csv.py" -v
```

The tests cover:

- deterministic rows,
- deterministic CSV bytes,
- complete mirrored transfer pairs,
- common currency decimal rules,
- historical default fixture dates,
- invalid row-count and currency rejection.

The tool is a developer/QA fixture generator, not a source of financial recommendations or production user data.
