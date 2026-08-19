# Finora Security Acceptance Evidence

Last reviewed: 2026-08-19

This document records what Finora can prove automatically from repository source and what still requires native-device validation. It intentionally does not describe a source build as equivalent to a signed, installed production application.

## Automated source contracts

The `Finora.UiTests` project copies selected production source/XAML files into the test output as read-only contract inputs. The security contract suite protects the following release requirements from silent regression:

### Local PIN lock

- The local PIN verifier is derived with PBKDF2-SHA256 and a random salt.
- PIN comparison uses fixed-time comparison.
- PIN verifier material is stored through platform secure storage rather than normal preferences.
- Temporary cryptographic byte buffers are cleared where managed APIs permit.
- Invalid PIN input is restricted to 4–12 ASCII digits.
- An enabled lock fails closed when secure storage is temporarily unavailable.
- App startup routes through the lock when a PIN exists.
- App activation re-locks after the configured inactivity interval.

### Biometric / Windows Hello unlock

- The lock screen always retains a masked numeric PIN fallback.
- When biometric unlock is disabled, the lock view model does not invoke the native availability probe.
- Biometric unlock is exposed only when the user preference is enabled and the native service reports availability.
- A transient exception from the native availability probe is contained and degrades to the PIN path instead of leaving an unobserved fire-and-forget task fault.
- Authentication failure does not navigate past the lock screen.
- Android uses the platform biometric prompt and exposes an explicit `Use PIN` fallback.
- Apple platforms use LocalAuthentication biometrics.
- Windows uses Windows Hello through `UserConsentVerifier`.
- Unsupported platforms return an explicit unsupported result rather than pretending authentication succeeded.

### Sensitive screen capture protection

- Android uses `FLAG_SECURE` through `WindowManagerFlags.Secure`.
- Windows uses `SetWindowDisplayAffinity` with `WDA_EXCLUDEFROMCAPTURE`.
- The preference is reapplied at startup and app activation.
- Platforms without a reliable supported blocking API are reported as unsupported; Finora does not claim universal screenshot prevention.

### Local premium/demo boundary

- The premium flag remains a local preference-backed development/demo capability.
- The control stays inside the hidden developer panel.
- Product copy explicitly states that the local flag is not tamper-proof commercial licensing.
- Product copy states that reliable paid entitlement validation requires a future store/server integration.
- Buy Me a Coffee support is explicitly separated from feature entitlement.

## Native validation still required

Source-contract tests cannot prove the behavior of OS security prompts or capture APIs on every device/OS revision. Before a production release, validate at minimum:

1. Android biometric success, failure, cancellation, lockout, PIN fallback, app backgrounding, and screenshot/screen-recording blocking on supported OS versions.
2. iOS and Mac Catalyst LocalAuthentication success/failure/cancellation and fallback behavior; confirm and document the platform screenshot-protection limitation.
3. Windows Hello success/failure/cancellation and `SetWindowDisplayAffinity` behavior in the packaged app.
4. Secure-storage behavior after device credential changes, application upgrade, reinstall, backup/restore, and OS migration where applicable.
5. Auto-lock timing after suspend/resume, window deactivation/reactivation, device sleep/wake, and clock changes.
6. Accessibility behavior of the lock screen with screen readers, keyboard focus, large text, high contrast, and reduced-motion settings.

## Evidence rule

A passing source contract means the intended defensive wiring remains present in the reviewed source. It is not a substitute for a signed-package test or physical-device test. Native results should be recorded with the tested app commit, app version/build, OS/device version, expected outcome, actual outcome, and any platform limitation.
