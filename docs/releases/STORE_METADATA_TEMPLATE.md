# Finora Store Metadata Template

This file is a release-preparation template. Store character limits, required declarations, screenshots, rating forms, privacy forms, payment/external-link policies, and policy wording must be verified in each current store console before submission.

## Canonical product identity

- Product name: **Finora**
- Attribution: **Made by the Sanskar**
- Package/Application ID: `in.sanskar.finora`
- Current version: 0.2.0
- Current build: 2
- Repository: https://github.com/sanskarIN/Finora
- Creator profile: https://www.github.com/sanskarIN
- Optional project support: https://buymeacoffee.com/sanskarIN
- Business/security contact: `sanskarin@outlook.in`
- Support: `supportramsandesh@gmail.com`
- License: Apache-2.0

## Buy Me a Coffee boundary

The current source exposes `https://buymeacoffee.com/sanskarIN` as an optional external project-support link in About.

It must not be described as:

- an in-app purchase;
- a subscription;
- premium entitlement;
- a feature unlock;
- a requirement for support;
- a guarantee of faster support;
- a secure license token.

Before every store submission, verify the target store's current rules for external contribution/payment links and the intended region/distribution model. If the store requires the link to be removed or altered in the packaged app, follow the current store rules without misrepresenting Finora's entitlement model.

## Short description draft

Local-first personal finance for accounts, transactions, budgets, goals, recurring items, reports, CSV/PDF export, and encrypted user-controlled backups.

## Long description draft

Finora is a local-first personal-finance application designed to help you organize accounts, expenses, income, budgets, savings goals, recurring items, reports, and local backups without requiring a Finora account.

Current features include account and transaction management, same-currency transfers, categories and tags, transaction splits, receipt attachments, account reconciliation, weekly/monthly/custom budgets, savings goals, recurring obligations, local reports, mapped CSV import, CSV/PDF export, privacy mode, optional local app PIN/biometric unlock, local reminders, and password-encrypted backup/restore.

Financial records are stored locally in app-private storage in the current release. Finora does not automatically upload your finance database or backups. Exporting or sharing a file is an explicit user action, and the destination you choose controls that copy.

Finora does not silently combine different currencies or invent exchange rates. Current transfers are same-currency only.

## Privacy highlights for store copy

Use only claims that remain true in the final release build:

- no Finora account/login required;
- core finance functionality works without internet;
- no automatic Finora cloud synchronization;
- no automatic backup upload;
- no background location collection;
- optional transaction location is manually entered text;
- local notifications use generic content;
- Android ordinary automatic backup/device-transfer paths are explicitly excluded by package configuration;
- encrypted portable backup is user-triggered;
- no default analytics/advertising telemetry dependency in current source line.

Store privacy/data-safety forms must be answered from the final built binary/dependency graph, not copied blindly from this template.

## Financial disclaimer direction

Store copy must not claim:

- guaranteed savings/profit/returns;
- investment advice;
- tax/legal/accounting advice;
- bank-grade certification without evidence;
- guaranteed bug-free/data-loss-free operation;
- universal screenshot prevention;
- guaranteed notification delivery under every OS state;
- automatic cloud recovery;
- forgotten encrypted-backup password recovery;
- automatic FX conversion;
- tamper-proof local premium licensing.

Use the repository Terms/Privacy documents for the actual current legal/product boundary.

## Feature bullets

Possible store bullets:

- Local-first accounts and transaction history
- Expense, income, refund, adjustment, and same-currency transfers
- Categories, tags, splits, and receipt attachments
- Account reconciliation with explicit adjustments
- Weekly, monthly, category, subcategory, and custom budgets
- Savings goals with contributions, milestones, and forecasts
- Recurring expenses, income, transfers, and refunds
- Currency-scoped reports with accessible text/table equivalents
- Mapped CSV import and CSV/PDF export
- Password-encrypted user-controlled backup and restore
- Optional privacy mode and local app lock
- Local reminder workflows

## Screenshot rules

Every store screenshot must use synthetic data only.

Do not expose:

- real names tied to finance records;
- real account identifiers;
- real merchant/location history;
- real receipt images;
- real backup filenames containing personal data;
- PIN/password/security secrets;
- development signing credentials.

Suggested screenshot set:

1. Dashboard with synthetic account/budget/goals
2. Transactions search/filter/sort
3. Account detail/reconciliation
4. Budget and savings planning
5. Recurring obligations
6. Reports with category/month/year chart + table
7. CSV import mapping/preview
8. Settings privacy/backup section

For screenshots intended to show privacy mode, use synthetic finance values underneath masking as well.

## Android / Google Play checklist

Verify current Play Console requirements at submission time.

Prepare:

- app name/short/full description;
- icon/feature graphics/screenshots;
- target audience/content rating;
- data safety form based on final binary;
- notification/biometric permission justification where required;
- privacy-policy URL/source as required;
- support contact;
- signed AAB;
- package/version code alignment;
- release notes;
- backup/device-transfer behavior validation;
- no unexpected SDK data collection from dependencies;
- current policy review for the optional Buy Me a Coffee external support link.

## Apple App Store checklist

Verify current App Store Connect requirements at submission time.

Prepare:

- app name/subtitle/description/keywords;
- screenshots per required device family;
- app privacy questionnaire from final binary;
- support/privacy URLs as required;
- Face ID purpose string aligned with behavior;
- notification behavior/declarations;
- signed/provisioned archive;
- export/compliance/crypto declarations as applicable to current rules;
- review notes explaining local-first encrypted backup if helpful;
- current policy review for the optional external Buy Me a Coffee support link.

## Mac distribution checklist

Depending on distribution channel verify:

- App Store vs notarized/direct distribution requirements;
- signing/notarization;
- finance category metadata;
- privacy/support links;
- screenshots;
- LocalAuthentication/UserNotifications behavior;
- file/share behavior;
- external support-link policy for the chosen channel.

## Windows store/package checklist

Verify current Microsoft Store requirements if distributing there.

Prepare:

- final package identity/publisher;
- signed package;
- app listing metadata/screenshots;
- privacy/support links;
- Windows Hello/toast behavior;
- capabilities/permissions;
- package upgrade test;
- accessibility notes where required;
- external support-link policy for the chosen distribution channel.

## Release notes template

### Finora <version>

- Added: <user-visible capabilities>
- Improved: <reliability/privacy/accessibility>
- Fixed: <correctness issues>
- Database schema: <version>
- Backup compatibility: <status>
- Known limitations: <platform/product boundaries>

Never write release notes implying a native/platform behavior was tested unless actual release evidence exists.

## Review notes template

Finora is local-first and does not require a Finora account. Core finance records are stored locally in app-private storage. Encrypted backup/export actions are initiated by the user. Optional biometric unlock is used only when the user enables it and a local PIN fallback remains available. Transaction location is manually entered only; the app does not request background location for this feature.

If the Buy Me a Coffee link remains in the submitted build, describe it only if the store requires clarification: it is an optional external project-support link and does not unlock app functionality or represent an in-app purchase/premium entitlement.

Adjust this note to the exact final binary/permissions before submission.

## Contact/footer

- Repository: https://github.com/sanskarIN/Finora
- Creator profile: https://www.github.com/sanskarIN
- Optional project support: https://buymeacoffee.com/sanskarIN
- Support: `supportramsandesh@gmail.com`
- Business/security: `sanskarin@outlook.in`
- Attribution: Made by the Sanskar

## Roadmap reference

See `docs/NEXT_STEPS.md` for the prioritized release-blocker, release-candidate, quality, and later-version roadmap. Store publication should follow the P0/P1 evidence gates before P2/P3 expansion.

## Final warning

This is a documentation template, not current store-policy advice. Store policies, SDK requirements, privacy forms, fees, signing rules, external contribution/payment link rules, screenshot dimensions, and declaration wording can change. Verify the live store consoles/toolchain before each submission.
