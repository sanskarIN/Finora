# Accessibility and Localization

This document describes the current Finora 0.2.0 accessibility/localization source capabilities and the native validation still required before release.

## Accessibility goals

Finora should remain understandable and operable without relying on color, chart shape, animation, or a single pointer interaction.

Current source supports accessibility through a combination of:

- semantic headings/descriptions on important screens;
- text/tabular equivalents for report charts;
- scalable/minimum control targets;
- larger-interface preference;
- reduced-motion preference;
- light/dark/system theme;
- adaptive phone/tablet/desktop navigation;
- masked secret controls with descriptive semantics;
- screen-reader descriptions on key lock/recurring/settings/onboarding controls.

## Chart accessibility

A financial chart must not be the only representation of value.

Current report charts are accompanied by textual/tabular equivalents.

Signed net charts use a true zero baseline so negative data is not visually misrepresented as a positive magnitude.

When amounts are hidden by privacy settings, quantitative chart data is suppressed rather than leaving value magnitude visible.

## Privacy and accessibility

Screen-reader semantics must not expose a value that visual privacy mode hides.

When adding semantics to monetary controls:

- do not put the real amount in `SemanticProperties.Description` while visual text is `••••`;
- avoid accessible hidden labels that bypass the privacy setting;
- test with TalkBack/VoiceOver/Narrator while privacy mode is active.

## Secret-entry accessibility

Backup password, new PIN, confirm PIN, and lock-screen PIN controls are masked.

Accessibility descriptions should identify the purpose of the field without reading the secret value aloud or storing it in diagnostic text.

## Keyboard/focus

Desktop targets must be usable with keyboard focus/navigation for primary finance workflows.

Validate:

- focus order follows visual/task order;
- focus does not disappear after modal/file picker return;
- flyout navigation can be reached;
- forms can be completed without mouse-only gestures;
- destructive actions are not triggered by accidental default focus;
- dialog confirmation has understandable button order.

## Touch targets

Shared styles enforce scalable/minimum control sizing. Native font/display scaling can still change layout, so test touch targets with large text and device display scaling.

## Reduced motion

Finora exposes a reduced-motion preference. Optional celebrations/transitions must not require animation to communicate completion.

Do not make a financial state change understandable only through motion.

## Theme and contrast

Finora supports system/light/dark theme behavior through MAUI resources/preferences.

Release QA must validate:

- text/background contrast;
- disabled control readability;
- error/warning distinction without color alone;
- chart/zero baseline visibility;
- focus indicator visibility;
- privacy masked values;
- system high-contrast behavior on Windows.

## Adaptive layout

Primary navigation adapts between mobile tabs and desktop/tablet flyout hierarchy.

Validate accessibility during resize/idiom changes:

- current section remains understandable;
- focus does not land on hidden hierarchy;
- screen reader does not enumerate hidden navigation items;
- wide/narrow layouts do not clip form labels;
- collection cards remain readable at large text sizes.

## Current localization architecture

Finora has an English baseline and initial Hindi common-string resource structure. Runtime culture is normalized/applied through `CultureSettings` and Settings/onboarding expose locale configuration.

The current repository is localization-ready in architecture, but it does **not** claim complete screen-by-screen Hindi translation.

Many XAML strings remain English and must be extracted into resources before claiming full localization.

## Locale vs currency

Locale and currency are separate concepts.

- Locale controls presentation conventions such as number/date formatting.
- Currency controls monetary precision/code.
- Changing locale does not convert currency.
- Changing default currency does not rewrite existing account/transaction currency.

## Runtime culture

`CultureSettings` validates/normalizes a requested culture and applies it to runtime culture state according to current source behavior.

Tests that mutate process-wide culture are serialized to avoid cross-test interference.

## Number/date preview

Settings shows locale-aware number/date preview so users can see presentation effect before/after changing locale/default currency.

Currency-specific decimal places still come from `Money`/`CurrencyMinorUnits`, not from assuming the locale's common currency.

## Localized parsing

Major-unit inputs attempt current culture parsing and, in several workflows, invariant fallback. Contributors must keep parsing behavior explicit and avoid silently treating thousands/decimal separators from one locale as another locale's amount without tests.

## Local calendar dates

Localization does not change the stored UTC timestamp model.

User-selected calendar dates are interpreted in local timezone and converted with `LocalDateRange`. Display uses the current culture where appropriate.

## String extraction guidelines

When localizing a new UI surface:

1. put user-visible stable strings in resource files rather than hard-coding XAML/C# where practical;
2. keep error codes/internal diagnostic tokens unlocalized when they are machine identifiers;
3. localize user-facing validation messages carefully;
4. preserve format placeholders/parameter order;
5. test long translations at phone width;
6. test right-to-left layout readiness even if a target RTL translation is not complete;
7. do not localize currency codes/technical identifiers incorrectly;
8. update accessibility semantics with the same localized meaning.

## Current RTL readiness

Android manifest has `supportsRtl="true"`. This is not proof that every screen is fully RTL-ready. Validate mirrored layouts/focus/order before advertising an RTL locale as fully supported.

## Platform accessibility validation

### Android

Use TalkBack and test:

- onboarding;
- Dashboard period picker;
- transaction form/history/paging;
- budgets/goals/recurring;
- reports/table/chart description;
- Settings secrets/reset/About;
- lock/biometric flow;
- font/display scaling.

### Windows

Use Narrator/keyboard/high contrast and test:

- flyout navigation;
- forms/collections;
- file picker return;
- Settings/lock;
- resize/DPI;
- report tables.

### iOS

Use VoiceOver/Dynamic Type/reduced motion and test:

- onboarding;
- navigation;
- forms;
- report equivalents;
- LocalAuthentication fallback;
- file picker/share.

### Mac Catalyst

Use VoiceOver/keyboard/mouse/resizable windows and test equivalent desktop flows.

## Accessibility release evidence

A release should record:

- platform/device/OS;
- screen reader used;
- text/display scaling;
- theme/high-contrast state;
- workflows tested;
- known issues/waivers;
- exact release commit.

Source semantics alone are not a passing native accessibility result.

## Localization completion definition

Do not claim a language is fully supported until:

- all normal user-facing screens are translated;
- validation/empty/error states are translated;
- Settings/About/legal navigation is translated where appropriate;
- plural/format rules are reviewed;
- locale-specific input/display tested;
- layout truncation tested;
- accessibility semantics translated;
- native QA completed.

The current source line should be described as English-first/localization-ready with an initial Hindi resource structure unless/until those gates are met.