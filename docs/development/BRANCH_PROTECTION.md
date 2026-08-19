# Main Branch Protection Policy

Finora uses pull requests, automated CI/security checks, CODEOWNERS, and release-readiness tooling. GitHub branch protection or a repository ruleset should enforce those repository conventions on `main`.

## Current enforcement state

As observed again during the final repository audit on **2026-08-19**, GitHub reports `main` as **not protected**. This document defines the intended policy; it does not claim that the settings have already been enabled.

The remaining host-administration action is tracked as **GitHub issue #28 — “Repository host: enable protection/ruleset for main.”** The issue is assigned to the repository owner and contains the exact activation/verification checklist. Close it only after GitHub itself reports the rule/ruleset active and the blocking behavior has been tested.

This is a repository-host governance action, not missing Finora application source functionality.

## Recommended GitHub ruleset

Target the default branch `main` and enable these protections:

1. Require a pull request before merging.
2. Require status checks to pass before merging.
3. Require the branch to be up to date before merging when GitHub can evaluate the relevant checks on the updated head.
4. Require conversation resolution before merging.
5. Block force pushes.
6. Block branch deletion.
7. Restrict bypass access to the minimum maintainer set needed for repository recovery.

For a solo-maintainer repository, required approving-review count can remain zero while pull-request and status-check requirements are enforced. If additional trusted maintainers participate, require at least one approving review and use CODEOWNERS review where appropriate.

## Core required checks

The ruleset should require the stable checks that protect the current source line, including:

- **Finora CI** — structural preflight, unit/integration/UI-contract tests, performance smoke, and supported source-build jobs as configured by the workflow;
- **CodeQL** — static security analysis;
- **Dependency Review** — dependency-change risk review on pull requests;
- **Repository release readiness** — repository structure, policy, workflow, and tracked-artifact guardrails.

GitHub check-context names can change when workflow/job names change. After editing workflow names, update the ruleset using the exact check contexts GitHub reports for a recent pull request.

## Additional workflows

Localization, sample-data, artifact-verification, native-harness, performance, and other focused workflows remain useful evidence even when they are path-filtered or on-demand. Do not make an on-demand-only check universally required unless every PR can produce that check; doing so can make `main` impossible to merge into.

## Signed commits

The repository currently contains unsigned commits. Do not enable a required-signed-commit rule until the maintainer signing workflow is configured and verified for GitHub/API/automation commits; otherwise legitimate maintenance may be blocked.

## Administrative validation

After enabling protection/rulesets:

- open a test pull request and verify direct pushes to `main` are rejected according to policy;
- verify required checks appear and can complete;
- verify a failing required check blocks merge;
- verify force-push and deletion are blocked;
- verify the documented emergency-bypass owner, if any, is intentional;
- record the ruleset/protection evidence against the release candidate;
- close GitHub issue #28 only after the above evidence is complete.

Branch protection is repository-host configuration, not application source behavior. It should therefore be validated in GitHub itself rather than inferred from files in the repository.
