# Native UI smoke automation

Finora includes dependency-free native smoke harnesses for Android and Windows. These are intentionally small building blocks for release validation; they complement, rather than replace, unit/integration/UI-contract tests and manual accessibility checks.

## Privacy-first design

Both harnesses are designed for a personal-finance application:

- screenshots are not captured automatically,
- the Android UI hierarchy is not persisted,
- the Windows harness does not print the full accessible-name tree,
- optional reports contain only counts and failed expectation messages,
- neither harness clears application data or invokes destructive Finora actions,
- neither harness sends data over the network.

Use synthetic/disposable finance data for native automation. The deterministic fixture generator in `scripts/generate_sample_finance_csv.py` is intended for that purpose.

## Android: ADB/UIAutomator hierarchy smoke test

Script:

```text
scripts/android_ui_smoke.py
```

Requirements:

- Android SDK platform-tools with `adb` on `PATH`,
- one connected emulator/device, or an explicit `--serial`,
- a debug/test Finora build already installed,
- the device unlocked and ready for UI automation.

The harness launches the requested package, asks Android's `uiautomator` command for the accessibility hierarchy, parses it in memory, checks expectations, removes the temporary device-side XML file, and exits.

### Basic launch validation

```bash
python scripts/android_ui_smoke.py --package <FINORA_APPLICATION_ID>
```

The application ID is intentionally supplied by the caller so the harness does not hard-code a package value that may differ between debug/release/flavor builds.

### Validate visible/accessibility content

```bash
python scripts/android_ui_smoke.py \
  --package <FINORA_APPLICATION_ID> \
  --expect-text "Dashboard" \
  --expect-description "Create" \
  --expect-id "add"
```

Matching is case-insensitive substring matching.

### Choose a specific device

```bash
python scripts/android_ui_smoke.py \
  --serial emulator-5554 \
  --package <FINORA_APPLICATION_ID> \
  --expect-text "Dashboard"
```

### Launch a specific activity

```bash
python scripts/android_ui_smoke.py \
  --package <FINORA_APPLICATION_ID> \
  --activity <FULL_ACTIVITY_COMPONENT>
```

Without `--activity`, the harness uses Android's launcher intent through `monkey` for one launch event only; it does not perform random interaction sequences.

### Privacy assertions

`--forbid-text` can fail a smoke check when a known value must not appear in visible/accessibility text. This is useful in a disposable fixture when validating amount-masking behavior:

```bash
python scripts/android_ui_smoke.py \
  --package <FINORA_APPLICATION_ID> \
  --expect-text "Dashboard" \
  --forbid-text "KNOWN_SYNTHETIC_AMOUNT"
```

Use only synthetic values that are safe to place in logs/arguments.

### Small JSON report

```bash
python scripts/android_ui_smoke.py \
  --package <FINORA_APPLICATION_ID> \
  --expect-text "Dashboard" \
  --report artifacts/android-ui-smoke.json
```

The report intentionally contains only:

- pass/fail,
- accessibility-node count,
- error count,
- failed expectation messages.

It does **not** include the full UI hierarchy.

## Windows: built-in UI Automation smoke test

Script:

```text
scripts/windows_ui_smoke.ps1
```

Requirements:

- Windows,
- PowerShell,
- a built Finora executable,
- built-in Windows UI Automation assemblies.

The harness starts Finora, waits for a native main window, reads Windows UI Automation names/automation IDs in memory, validates requested expectations, prints a sanitized JSON summary, and stops the process unless `-KeepRunning` is specified.

### Basic window smoke test

```powershell
pwsh -File scripts/windows_ui_smoke.ps1 `
  -Executable "<PATH_TO_FINORA_EXE>"
```

### Validate accessible names

```powershell
pwsh -File scripts/windows_ui_smoke.ps1 `
  -Executable "<PATH_TO_FINORA_EXE>" `
  -ExpectName "Dashboard","Transactions"
```

### Validate automation IDs

```powershell
pwsh -File scripts/windows_ui_smoke.ps1 `
  -Executable "<PATH_TO_FINORA_EXE>" `
  -ExpectAutomationId "SomeStableAutomationId"
```

Use automation IDs only when the application intentionally exposes stable IDs for test/accessibility use. Do not make tests depend on incidental framework-generated IDs.

### Keep Finora running after validation

```powershell
pwsh -File scripts/windows_ui_smoke.ps1 `
  -Executable "<PATH_TO_FINORA_EXE>" `
  -KeepRunning
```

By default the launched process is stopped after validation to keep automated environments clean.

## What these harnesses should validate

Good smoke expectations focus on stable user outcomes:

- app launches,
- primary navigation is discoverable,
- a critical heading/action is accessible,
- localized headings appear in the active language,
- privacy mode does not expose a known synthetic amount,
- a high-risk confirmation dialog presents the expected consequence and safe action,
- an import/backup/error state is announced.

Avoid asserting every label on every page. The source-contract tests already cover many static localization/accessibility invariants; native smoke tests should catch platform wiring failures.

## What these harnesses do not prove

They do not replace:

- TalkBack/Narrator/VoiceOver manual QA,
- keyboard/focus-order validation,
- touch-target evaluation,
- Android/iOS/macOS/Windows packaging validation,
- biometric/Windows Hello real-device checks,
- notification permission delivery checks,
- file picker/share sheet checks,
- full end-to-end transaction correctness tests.

Use `docs/accessibility/NATIVE_ACCESSIBILITY_QA.md` for the native accessibility release matrix.

## Automated harness tests

The Android parser/expectation code can be tested without a device:

```bash
python -m unittest discover -s scripts/tests -p "test_android_ui_smoke.py" -v
```

CI also parses the Windows PowerShell script without launching it, catching PowerShell syntax errors on non-Windows runners. Real Windows UI Automation execution remains a release/native validation task unless a Windows runner with the built app is configured.

## Suggested release smoke sequence

1. Build/install a release candidate using disposable data.
2. Run the Android or Windows launch smoke harness.
3. Validate primary navigation/accessibility labels in English.
4. Switch Finora to Hindi and validate localized headings.
5. Enable privacy mode and validate a known synthetic amount is not exposed.
6. Run the native accessibility QA matrix.
7. Record commit, platform, OS/device, locale, and pass/fail evidence in the release checklist.

Never place real account names, balances, transaction descriptions, PINs, backup passwords, or personal information in smoke-test arguments or logs.
