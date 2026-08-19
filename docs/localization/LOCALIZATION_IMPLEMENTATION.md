# Finora localization implementation

Finora uses runtime-loaded `.resx` resource bundles for user-facing copy. The current implementation provides neutral English resources and Hindi (`hi`) resources while preserving the app's local-first and privacy behavior.

## Architecture

`src/Finora.App/LocalizationResources.cs` discovers compiled resource bundles under `Finora.App.Resources.Strings.*Resources` and exposes them through application resources with the `Text.` prefix.

Examples:

- neutral key: `Dashboard`
- XAML key: `Text.Dashboard`
- XAML usage: `{DynamicResource Text.Dashboard}`
- code usage: `LocalizationResources.Get("Dashboard")`

The application declares English as its neutral language. The active locale is applied during startup, and changing the locale from Settings refreshes the application resource dictionary so `DynamicResource` bindings can update without rebuilding the page tree.

## Bundle layout

Feature-specific bundles are intentionally supported. This avoids one very large resource file and reduces merge conflicts between independent features.

Current bundle families include the application shell/onboarding/lock/account/dashboard copy plus feature bundles for transactions, budgets, savings, recurring workflows, account details, categories/tags, transaction workflow screens, CSV import, reports, and Settings.

Each neutral bundle must have a matching Hindi bundle:

```text
FeatureResources.resx
FeatureResources.hi.resx
```

## Key uniqueness

Resource keys are global after loading because they are inserted into the application dictionary as `Text.<Key>`.

`LocalizationResources` rejects duplicate keys across discovered bundles. New feature bundles should therefore use descriptive feature-prefixed keys when a generic key already exists.

## Automated parity protection

`tests/Finora.UiTests/LocalizationContractTests.cs` discovers neutral `*Resources.resx` files copied into the contract fixture and requires a matching `.hi.resx` bundle with the identical key set.

This protects against:

- adding an English key without Hindi coverage,
- deleting a key from only one language,
- accidentally renaming only one side,
- creating a feature bundle without a Hindi counterpart.

Additional UI contracts check that migrated primary surfaces use `DynamicResource` instead of static page titles/headings.

## XAML guidance

Use dynamic resources for user-facing strings that should refresh after the locale changes:

```xml
<Label Text="{DynamicResource Text.Dashboard}" />
```

Prefer localized semantic descriptions as well:

```xml
<Button
    Text="{DynamicResource Text.Apply}"
    SemanticProperties.Description="{DynamicResource Text.ApplyDashboardPeriodDescription}" />
```

Do not localize persisted identifiers, database values, resource keys, route names, CSV column identifiers, or internal sentinels. Localize their display representation instead.

## ViewModel and code-behind guidance

User-facing status, validation, dialog, and workflow messages should use:

```csharp
LocalizationResources.Get("SomeKey")
```

For formatted messages, combine the localized format string with the current culture:

```csharp
string.Format(
    CultureInfo.CurrentCulture,
    LocalizationResources.Get("SomeFormatKey"),
    value);
```

Internal exceptions that are never shown to users may remain technical; exceptions surfaced through `ErrorMessage`, alerts, status text, or validation UI should be localized.

## Money and privacy requirements

Localization must not weaken Finora's amount-hiding behavior.

When a row contains a label plus money:

- localize the label separately,
- keep the monetary value routed through the existing privacy-aware converter or privacy-aware ViewModel formatting,
- do not build a localized string by directly formatting raw money when privacy mode can hide that amount.

Example concept:

```text
[localized “Actual spending:”] [privacy-aware amount]
```

This is preferred over a single hard-coded formatted string that bypasses the converter.

## Enum and boolean display

Domain enum names and `true`/`false` values are internal data representations. Where they are visible to users, provide a localized display mapping rather than changing the persisted enum/value.

This is especially relevant for:

- transaction types,
- account types/states,
- recurrence status/frequency,
- theme choices,
- yes/no or archived/active labels.

## Locale input

Finora uses a BCP-47-style locale setting for culture selection and date/number formatting. Invalid locale input must fail safely and should not corrupt persisted settings.

A locale change should update:

- application text resources,
- date formatting,
- number formatting,
- localized generated status/summary text after the relevant ViewModel refreshes.

## Translation quality

Automated parity confirms structural completeness, not linguistic quality. Hindi copy still requires native-language review on real devices, especially for:

- narrow phone layouts,
- accessibility announcements,
- finance terminology,
- destructive confirmation dialogs,
- backup/security language,
- plural/count wording,
- long report and recurring-workflow messages.

Do not mark native-language QA complete solely because the resource parity tests pass.

## Manual validation checklist

For each supported platform available to the release tester:

- [ ] Launch in English and verify primary navigation.
- [ ] Switch to Hindi and verify visible text refreshes.
- [ ] Restart the app and verify the selected locale persists.
- [ ] Verify onboarding and lock/security surfaces.
- [ ] Verify Dashboard, Accounts, Transactions, Budgets, Goals, Recurring, Reports, Import, and Settings.
- [ ] Verify privacy mode still masks all monetary values in both languages.
- [ ] Verify larger-text layouts do not clip critical controls.
- [ ] Verify screen-reader semantics use localized descriptions where provided.
- [ ] Verify destructive actions remain explicit and understandable.
- [ ] Verify dates and numbers follow the selected culture without changing stored financial values.

## Adding another language

To add a culture such as `xx`:

1. Add `FeatureResources.xx.resx` alongside each neutral feature bundle that needs translated copy.
2. Preserve exactly the same resource keys.
3. Add locale selection/validation UX if the culture should be explicitly selectable.
4. Add automated parity coverage for the new culture before treating it as supported.
5. Perform native-speaker and device QA.

Do not advertise a language as fully supported until the major user workflows and critical security/privacy dialogs are translated and validated.
