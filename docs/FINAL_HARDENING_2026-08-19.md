# Finora Final Hardening — 2026-08-19

This document records the post-closure correctness audit performed against the current Finora 0.2.0 (build 2), schema-2 source line. It supplements `FINAL_REPOSITORY_CLOSURE.md`; it does not rewrite older validation evidence or imply that future defects are impossible.

## Scope

The audit focused on correctness and failure safety in money-threshold calculations, transaction construction, receipt storage, encrypted restore directory replacement, strict compiler/analyzer compatibility, issue/PR backlog state, and release-governance evidence.

No cloud account, synchronization, telemetry, automatic FX conversion, server-backed entitlement, or other later-version architecture was added.

## Correctness fixes

### Budget warning thresholds

Budget warning thresholds now use overflow-safe ceiling percentage arithmetic. Fractional thresholds therefore trigger only when the configured percentage has actually been reached. For example, an 80% threshold on 101 minor units resolves to 81, not 80.

`PercentageMath.CeilingPercentOf` validates the supported percentage range and is covered at normal, fractional, zero, invalid, and `long.MaxValue` boundaries.

### Transaction factory validity

`TransactionFactory.Create` no longer accepts `TransactionType.Transfer`. A single generic transaction constructor cannot produce Finora's required reciprocal, balanced transfer pair, so callers are directed to the dedicated transfer workflow instead of receiving an invalid domain entity.

The factory now validates the entity before returning it, which also rejects missing account identifiers, missing timestamps, and other domain-invalid construction inputs.

### Receipt MIME and extension consistency

Receipt storage now canonicalizes the supplied MIME value and derives the private stored extension from that MIME type rather than trusting a conflicting original filename extension. The user-visible original filename remains preserved separately.

This keeps stored metadata and private file representation consistent while retaining the existing MIME allow-list, size limit, generated internal name, path-safety, and SHA-256 integrity behavior.

## Encrypted restore receipt-directory safety

The audit found a failure window in the inner encrypted-restore service: after moving the live receipt directory to rollback storage, a later directory-move failure could reach cleanup code that removed rollback storage instead of restoring it.

Restore recovery now models the receipt-swap state explicitly:

- if the original live directory was moved aside and staging fails to promote, the rollback directory is returned to the live path;
- if staged receipts were promoted and the database commit fails, the promoted tree is removed and the original rollback tree is restored;
- if there was no original receipt directory and staged receipts were promoted, commit failure removes the newly promoted tree;
- if an expected rollback directory is unexpectedly missing, recovery fails closed and does not destroy the current live directory while pretending recovery succeeded;
- cancellation uses the same recovery primitive before propagating cancellation;
- when automatic rollback cannot complete, recovery data is preserved where available and the result does not falsely claim a safe complete rollback.

The existing `CrashSafeBackupService` and restore journal remain the outer process-interruption recovery layer. This change makes the inner `BackupService` failure behavior safe on its own as well.

## Regression coverage added or expanded

- `tests/Finora.UnitTests/PercentageMathTests.cs`
- `tests/Finora.UnitTests/TransactionFactoryTests.cs`
- `tests/Finora.IntegrationTests/RestoreDirectoryRecoveryTests.cs`
- `tests/Finora.IntegrationTests/AttachmentMetadataConsistencyTests.cs`

Infrastructure internals used only for focused recovery testing are exposed to `Finora.IntegrationTests` through `InternalsVisibleTo`; no production API surface was widened.

The new test source was also reviewed against Finora's repository-wide `Nullable=enable`, `TreatWarningsAsErrors=true`, and latest-recommended analyzer policy so nullable-flow warnings do not become hidden build failures.

## Repository backlog audit

At this checkpoint:

- repository issue search returns no open non-PR issues;
- the only open PR is the final-hardening PR containing this work;
- the changed-file set is limited to the intended correctness/safety implementation, tests, and documentation;
- prior debt-marker searches for TODO/FIXME/NotImplementedException/HACK/XXX did not identify a current source backlog, subject to normal search/index limitations.

This is an audit result, not a guarantee that undiscovered bugs cannot exist.

## Validation evidence boundary

The older exact verified candidate `8a8e7e51a2bacecdc58405d3d5301e79f3d78c8b` remains the repository's recorded **319/319 automated tests + four-platform Release source builds + CodeQL + Dependency Review** evidence baseline.

That historical evidence is not reused as runtime proof for this final-hardening branch. GitHub Actions runs for the advancing PR head have been observed queued with no conclusion. A queued run is neither a pass nor a failure and must remain documented as such until GitHub reports a conclusion for the exact candidate.

Native signing, package installation, physical-device behavior, accessibility, interrupted-process/low-disk failure injection, store-console policy review, and store approval remain external release evidence gates.

## Main-branch governance observation

GitHub currently reports `main` as unprotected. The repository already contains CI, CodeQL, Dependency Review, release-readiness automation, CODEOWNERS, contribution/security policies, and PR templates, but those controls are stronger when GitHub branch protection or a repository ruleset enforces them.

This connected repository interface does not expose a safe branch-protection mutation action, so protection is **not** represented as enabled by this audit. Maintainers should enable protection/rulesets in GitHub and require pull-request-based changes plus the repository's relevant status checks before treating governance enforcement as complete.

## Completion boundary

After the fixes in this audit, no additional current-scope source feature or reproducible repository defect was intentionally left open. Remaining work is release-owner/native evidence or future maintenance triggered by a reproducible defect, security/dependency advisory, platform/toolchain change, documentation correction, or deliberately approved later-version feature.
