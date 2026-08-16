# Finora Project Support — Buy Me a Coffee

[![Support Finora on Buy Me a Coffee](../../src/Finora.App/Resources/Images/bmc_support.svg)](https://buymeacoffee.com/sanskarIN)

> **☕ Support Finora development:** https://buymeacoffee.com/sanskarIN

Buy Me a Coffee is an optional external contribution path for people who want to support ongoing Finora development. It is deliberately separated from personal-finance data, feature access, licensing, support priority, and security reporting.

## Current user-facing placements

The same Finora-branded `bmc_support.svg` artwork is reused so project support has one consistent identity instead of several unrelated banners.

- **Settings → About** — prominent support artwork plus a clickable Buy Me a Coffee button.
- **Onboarding** — an optional support card with the artwork, an explicit contribution disclaimer, and a browser action.
- **Tablet/desktop adaptive flyout** — persistent support artwork so the project-support identity remains discoverable outside the About section.
- **Repository support guide** — prominent linked artwork and support callout.
- **Documentation hub** — prominent linked artwork and support callout.

The support artwork should remain a discovery aid, not an interruption. Finora must not place contribution prompts in transaction entry, backup/restore confirmation, app-lock prompts, error dialogs, or other sensitive finance/security workflows merely to increase visibility.

## Product and trust boundaries

A Buy Me a Coffee contribution must never:

- unlock or gate a Finora feature;
- enable the local premium demonstration flag;
- create or simulate a paid entitlement;
- change support-response priority;
- change vulnerability-reporting priority;
- influence finance calculations, reports, imports, exports, backups, reminders, or app-lock behavior;
- require access to the user's finance database or any private finance data;
- require a Finora account or background network service.

Project support remains voluntary. Core current-release finance behavior remains local-first and account-free.

## Implementation rules

- Keep the canonical URL in `Finora.Shared.AppConstants.BuyMeACoffeeUrl`.
- Reuse the packaged `Resources/Images/bmc_support.svg` artwork for in-app visual placement.
- Open the external page only after a direct user action on clickable placements.
- Use the system browser/launcher boundary rather than embedding finance data in a web request.
- Treat browser-launch failure as a generic UI error and do not expose raw platform/provider exception text.
- Keep semantic descriptions on visual placements so screen-reader users understand that the destination is external project support.
- Keep support copy clear that contributions are optional and do not create entitlement or service-level guarantees.

## Regression protection

`Finora.UiTests` contains source-contract coverage that protects:

- valid branded BMC SVG packaging;
- the Settings support-artwork and canonical-link wiring;
- support-artwork visibility in Settings, Onboarding, and adaptive Shell navigation;
- onboarding use of the canonical BMC URL;
- the optional/no-entitlement wording on the onboarding support surface.

When a support surface changes, update the contract test with the same commit instead of removing the guard.

## Store and release review

External contribution/payment-link rules vary by store, platform, app category, region, and distribution model. Before every packaged release:

1. verify the current target-store rule for external support/contribution links;
2. confirm the exact in-app placement is allowed for that release channel;
3. remove or conditionally exclude a placement when a target-store rule requires it;
4. keep the non-commercial open-source/support documentation available outside the packaged app;
5. never represent the BMC link as store-backed entitlement validation.

This document describes the Finora project-support UX and engineering boundary; it is not a claim that every current or future store will permit every placement.
