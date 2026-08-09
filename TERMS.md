# Finora Terms

These terms describe the open-source Finora application and repository. They are not a substitute for jurisdiction-specific legal advice.

## Personal finance organization tool

Finora is software for recording, organizing, reviewing, importing, exporting, budgeting, savings tracking, recurring-item tracking, and backing up personal financial records.

Finora is **not** a bank, broker, lender, payment processor, accountant, tax adviser, financial adviser, investment adviser, insurance provider, or legal adviser. Information calculated/displayed by Finora is not financial, investment, tax, accounting, or legal advice.

Users remain responsible for verifying balances, imported records, exports, budgets, reports, reminders, and other financial information before relying on it for real-world decisions.

## Local-first storage

The current release stores finance data locally and does not require a Finora account/cloud service for core functionality. Device storage can fail, become corrupted, be lost, reset, or be removed during uninstall.

Users are responsible for keeping appropriate external encrypted backups. Uninstalling/resetting Finora without a separately saved backup may permanently remove local finance records and receipts.

## Backup passwords

Finora cannot recover a forgotten encrypted-backup password. Users are responsible for choosing and safely retaining their backup password.

## Imported data

CSV import can validate syntax/mapping and apply duplicate heuristics, but Finora cannot guarantee that source data supplied by a bank, user, spreadsheet, or third party is semantically correct. Users should review mapping and imported results.

## Exports and sharing

When a user exports or shares CSV, PDF, diagnostics, integrity reports, or encrypted backups through operating-system share/save interfaces, protection at the selected destination is controlled by the operating system and recipient application/location. Users should choose destinations appropriate for private financial information.

## Notifications and reminders

Budget, backup, recurring-item, and other reminders are convenience features. Operating systems can delay, suppress, revoke, or limit notifications because of permissions, power management, reboot, force-stop behavior, or platform policy. Users should not rely on Finora as the sole mechanism for time-critical bill/payment obligations.

## App lock and device security

Finora can offer PIN and supported biometric/Windows Hello protection, privacy mode, and platform screen-capture restrictions. These controls do not make a compromised/rooted/jailbroken device secure, do not prevent an external camera from photographing the screen, and cannot guarantee protection against attackers with operating-system or filesystem privileges.

## Local premium/demo state

Any current local premium flag is a development/demo capability. It is not represented as secure/tamper-proof commercial licensing and does not create a paid-service entitlement. Future commercial licensing would require separately designed store/server validation and terms.

## Open-source license

Finora source code is made available under the Apache License 2.0, subject to the repository `LICENSE` file. Third-party components remain subject to their own licenses and notices.

## No warranty

To the extent permitted by applicable law, Finora is provided on an **“AS IS”** and **“AS AVAILABLE”** basis without warranties or conditions beyond those stated in the applicable open-source licenses. No claim is made that Finora is error-free, uninterrupted, suitable for every accounting/tax system, or immune to data loss/security defects.

The Apache-2.0 license contains the controlling software warranty/liability terms for the licensed source where applicable.

## Limitation of reliance

Users should independently verify important financial records and maintain backups before uninstalling/resetting the app, testing development builds, performing migrations, or relying on exports for accounting/tax/legal purposes.

## Privacy and security

See `PRIVACY.md` and `SECURITY.md` for current local-first data practices and private vulnerability-reporting guidance.

## Support

Support is best-effort unless a separate written agreement states otherwise. See `SUPPORT.md`.

Business/security contact: `sanskarin@outlook.in`

Support: `supportramsandesh@gmail.com`

Repository: https://github.com/sanskarIN/Finora
