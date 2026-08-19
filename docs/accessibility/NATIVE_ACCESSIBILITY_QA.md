# Finora native accessibility QA matrix

Automated source contracts protect important accessibility invariants, but they cannot prove platform accessibility behavior. Run this matrix on release candidates for every supported platform available to the release team.

## Principles

Finora accessibility validation should protect:

- understandable financial context,
- complete keyboard and screen-reader access,
- privacy-safe announcements,
- larger-text usability,
- reduced-motion preferences,
- text equivalents for quantitative visuals,
- safe focus behavior around destructive actions,
- localized semantics in supported languages.

Never use real personal financial data during accessibility testing.

## Android

### TalkBack

- [ ] Launch Finora with TalkBack enabled.
- [ ] Navigate the onboarding flow without touch exploration dead ends.
- [ ] Verify Dashboard cards announce their heading/context before monetary values.
- [ ] Verify privacy mode masks values visually and in accessibility output.
- [ ] Verify Accounts list items announce account name/type/currency without confusing duplicated text.
- [ ] Verify transaction amount, date, account, category, merchant/payee, and save controls have meaningful labels.
- [ ] Verify advanced transaction filters can be opened, changed, applied, and cleared.
- [ ] Verify Budgets and Savings goals expose useful progress/context.
- [ ] Verify recurring rule/occurrence controls announce selected rule, state, due date, and action.
- [ ] Verify Reports chart summaries are available as text/list equivalents.
- [ ] Verify backup/restore, import/export, PIN/biometric, and destructive Settings actions are distinguishable.
- [ ] Verify error/status messages are discoverable after an action fails or succeeds.

### Font and display scaling

- [ ] Test at the largest practical Android font scale.
- [ ] Test increased display size.
- [ ] Confirm primary actions remain visible or scrollable.
- [ ] Confirm long Hindi labels do not overlap controls.
- [ ] Confirm monetary values do not clip important currency context.

### Switch/accessibility navigation

- [ ] Verify controls are reachable in a logical order.
- [ ] Verify no essential action depends only on a gesture.
- [ ] Verify dialogs return focus predictably.

## Windows

### Narrator

- [ ] Navigate shell/flyout navigation with Narrator.
- [ ] Verify PIN and Windows Hello fallback behavior is announced clearly.
- [ ] Verify transaction and reconciliation form controls have meaningful names.
- [ ] Verify reports announce text equivalents, not only chart visuals.
- [ ] Verify destructive confirmations include the consequence before the confirm action.
- [ ] Verify external support/legal links are identifiable as actions.

### Keyboard-only

- [ ] Reach every primary navigation destination with keyboard input.
- [ ] Traverse each form in a logical focus order.
- [ ] Activate buttons, toggles, pickers, and list selections without a pointer.
- [ ] Escape/cancel modal dialogs without triggering destructive actions.
- [ ] Verify focus does not disappear after refresh, import validation, backup actions, or transaction edits.
- [ ] Verify the selected transaction/account can be opened from keyboard navigation where supported by MAUI controls.

### Text scaling / high contrast

- [ ] Test Windows text scaling above 100%.
- [ ] Test a high-contrast theme where available.
- [ ] Verify danger/success state is not communicated by color alone.
- [ ] Verify focus indicators remain visible.

## iOS / macOS

Where these targets are part of the release being validated:

- [ ] Test VoiceOver navigation and action labels.
- [ ] Test Dynamic Type / larger text.
- [ ] Test keyboard navigation on macOS/iPad hardware where applicable.
- [ ] Test reduced-motion preference behavior.
- [ ] Verify file picker/share sheet handoff has understandable return focus.
- [ ] Verify biometric fallback copy remains clear and PIN remains available.

## Reduced motion

- [ ] Enable the operating-system reduced-motion setting where supported.
- [ ] Enable Finora's reduced-motion preference.
- [ ] Verify goal completion does not require animation to convey success.
- [ ] Verify navigation and status changes remain understandable without motion.
- [ ] Verify no critical information is conveyed only by animation.

## Privacy mode accessibility

Privacy mode must protect assistive-technology output as well as pixels.

- [ ] Enable privacy mode.
- [ ] Verify Dashboard amounts are masked.
- [ ] Verify transaction/account lists that use privacy-aware converters are masked.
- [ ] Verify report chart values and textual summaries are hidden consistently.
- [ ] Verify reconciliation previews do not speak hidden money values.
- [ ] Verify savings forecasts do not expose hidden contribution estimates.
- [ ] Disable privacy mode and confirm values return without requiring a data reload that changes persisted values.

## Localization accessibility

Run the primary flow in English and Hindi:

- [ ] Navigation labels are translated consistently.
- [ ] Semantic descriptions use the active locale where localized resources exist.
- [ ] Dialog text is understandable and not clipped.
- [ ] Dates/numbers follow the selected culture.
- [ ] Screen-reader pronunciation remains usable for mixed technical terms such as CSV, PDF, PIN, and Windows Hello.
- [ ] Locale switching does not cause duplicate focus, inaccessible controls, or stale English headings.

## Charts and quantitative information

For every quantitative chart:

- [ ] A screen-reader user can obtain equivalent values from text or a list/table.
- [ ] Signed values preserve the meaning of positive/negative changes.
- [ ] Zero baseline behavior is consistent with the textual values.
- [ ] Privacy mode hides the quantitative chart when the equivalent values are hidden.
- [ ] Chart accessibility descriptions do not claim values that differ from the listed data.

## Destructive and security flows

Validate with accessibility tools enabled:

- [ ] Soft-delete / restore transaction flow.
- [ ] Category/tag archive and restore.
- [ ] Recurring rule archive/pause/resume and occurrence skip/reopen.
- [ ] Finance-data deletion confirmation.
- [ ] Developer-only full reset confirmation when explicitly enabled.
- [ ] PIN setup/removal.
- [ ] Biometric/Windows Hello enable/disable and PIN fallback.
- [ ] Encrypted backup restore confirmation/error handling.

The destructive consequence and the safe/cancel option must be understandable before activation.

## Evidence to record for release sign-off

For each platform tested, record:

- Finora commit/release candidate,
- OS/device version,
- assistive technology and version,
- locale,
- text/display scale,
- privacy-mode state,
- pass/fail for the sections above,
- sanitized defect links for failures.

A source-contract pass is not a substitute for this native validation.
