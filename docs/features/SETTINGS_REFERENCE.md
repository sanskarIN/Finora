# Finora Settings Reference

This reference describes the current settings exposed or stored by Finora 0.2.0. Some controls are platform-dependent or hidden developer controls.

## Default currency

- Stored through app preferences.
- Normalized uppercase.
- Validated by Domain currency rules.
- Invalid stored value falls back to `INR` in the current settings service.
- Used as default/reporting currency for applicable new records/reports.
- Changing the default currency does not convert existing account/transaction values.

## Locale

- Normalized through `CultureSettings`.
- Applied at runtime.
- Settings/onboarding expose locale-aware number/date preview behavior.
- Locale affects formatting/parsing presentation; it does not rewrite stored minor-unit money.

## Financial month start day

- Range: 1–28.
- Used by Dashboard financial-month period policy.
- 1–28 avoids invalid start-day ambiguity across short calendar months.

## Privacy mode

When enabled, passive monetary display is hidden across protected finance surfaces.

This is a display setting, not encryption-at-rest.

## Hide amounts on launch

Provides a second display-hiding condition used with privacy mode for passive money.

Current protected surfaces and report chart suppression are documented in `docs/security/APP_LOCK_AND_PRIVACY.md`.

## Theme

Current preference supports:

- System;
- Light;
- Dark.

Native visual validation is required on all targets.

## Reduced motion

A preference for minimizing optional animation/motion. Current completion/status UX is designed not to require forced animation.

Native platform behavior still requires validation.

## Larger interface

Controls larger/scalable interface behavior where implemented through shared resources/layout settings.

This complements OS text/display scaling; it does not replace native accessibility testing.

## Backup reminders

Current default: enabled in settings service.

Reminder coordination can create/remove the generic local backup reminder according to current notification settings/schedule policy.

Disabling should remove stale scheduled reminder state.

## Notifications enabled

Controls whether Finora should use local reminder features after applicable platform permission flow.

Finance core functionality must remain usable when notifications are denied/disabled.

## Onboarding complete

Tracks whether first-run onboarding has been completed.

Settings exposes a Revisit onboarding action. Revisiting should not duplicate opening/sample finance data when accounts already exist.

## Auto-lock minutes

- Stored preference is clamped to 1–60 minutes.
- Used by lifecycle inactivity lock behavior.

App-lock must be tested across suspend/resume/background transitions on each platform.

## Biometric unlock

Optional preference.

Rules:

- requires local PIN fallback;
- native availability/authentication determines actual use;
- failure/cancel does not bypass lock;
- platform error details are normalized before user display.

## Sensitive screen protection

Current settings service default: enabled.

Applies platform-supported capture protection where available. It is not a universal guarantee across Android/Windows/Apple/privileged capture paths.

## Receipt image quality

- Stored/clamped range: 40–100.
- Current default: 85.

This preference affects receipt image handling only where the current attachment workflow uses it. Original supported documents remain subject to content/size/path rules.

## Default account

Optional saved account ID used by transaction entry to preselect a preferred active account when available.

If the saved account is missing/unavailable, the UI falls back to another available account instead of creating a hidden dependency.

## Default transaction type

Stored transaction type preference.

The settings service intentionally prevents `Transfer` from becoming the generic quick-add default because transfers require the dedicated paired workflow. Invalid/Transfer values fall back to Expense.

## Last backup timestamp

Optional UTC timestamp stored as Unix seconds after successful user-triggered backup handling according to current Settings flow.

Malformed/out-of-range stored value is removed/falls back safely.

This timestamp is reminder/status metadata, not proof that the external destination still contains a usable backup.

## Dashboard card preferences

Independent booleans control visibility of current Dashboard card groups:

- balance;
- income/expense;
- budget;
- upcoming recurring;
- categories;
- goals;
- recent transactions;
- cash flow.

These settings change presentation, not stored finance records.

## Local premium demo

`LocalPremiumDemoEnabled` is a development/demo feature flag.

It is explicitly not secure commercial entitlement and must not be represented as tamper-proof licensing.

## Backup password

Not a persisted setting.

Settings uses a masked `Entry`; the password is transient input for explicit create/preview/restore operations and is cleared from the UI after handled operation paths.

## PIN

Plaintext PIN is not stored in Preferences.

Secure-storage verifier behavior is documented in `docs/security/APP_LOCK_AND_PRIVACY.md`.

Settings uses masked New PIN / Confirm PIN fields.

## About / legal / support

Current About/Settings links expose:

- packaged version/build;
- Made by the Sanskar attribution;
- technology summary;
- repository;
- creator profile;
- optional Buy Me a Coffee development-support page: https://buymeacoffee.com/sanskarIN;
- business/security contact;
- support contact;
- Privacy;
- Terms;
- License/notices;
- Contributing;
- Security;
- Support guide.

The Buy Me a Coffee action opens the external page through the system launcher. Failure uses generic user-facing text and privacy-safe logging.

Buy Me a Coffee is not a Finora setting, entitlement, subscription, premium flag, or finance feature. Supporting the project does not unlock app functionality, alter support priority, or replace store/server-backed purchase validation.

Because mobile/desktop store policies around external contribution/payment links can change, each packaged release must verify whether the target store permits this link in the intended distribution context.

## Backup/restore controls

Settings hosts the user-triggered encrypted backup/preview/restore workflow. It uses the registered crash-safe backup service and system picker/share/save boundaries.

## Full finance-data deletion

Settings routes destructive finance deletion through the dedicated complete reset service and typed confirmation.

This is not a factory reset of every preference/security value.

## Developer panel

The hidden developer area includes diagnostics/testing controls such as:

- schema information;
- feature/development state;
- sanitized data-integrity check;
- deterministic synthetic sample reset;
- reminder simulation/synchronization controls implemented by the current source.

Developer controls are not normal end-user financial workflows and should remain clearly separated.

## Preference storage boundary

Most UI preferences use MAUI Preferences. Small PIN verifier material uses OS SecureStorage. Finance records use SQLite/app-private attachments.

Do not migrate finance records into Preferences/SecureStorage to avoid schema design.

## Reset/uninstall behavior

Complete finance reset intentionally preserves application-operability preferences according to its current contract.

OS uninstall/reset behavior can remove app-private data/preferences/secure storage depending on platform behavior. Users should save a verified external encrypted backup before destructive device/app operations if data preservation matters.

## Related roadmap

See `docs/NEXT_STEPS.md` for the prioritized release-validation, store-readiness, quality, and later-version execution plan.
