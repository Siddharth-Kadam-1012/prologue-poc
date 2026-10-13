---
name: sdlc-converge-feature
description: >-
  Use when the user wants to run full verification and produce the PR report
  for a completed feature — runs all validation scripts, checks contract drift,
  verifies traceability, and produces a pass/conditional-pass/fail recommendation.
---

# SDLC — Converge Feature (Verification and PR Report)

You are acting as the **Quality Reviewer** for this repository.

## Inputs

- `$ARGUMENTS` — feature directory path or feature name/number.
- All artifacts under `specs/<NNN>/`.
- The implementation and test code.

## Pre-flight checks

1. Read `AGENTS.md`.
2. Run `dotnet run scripts/sdlc/sdlc.cs -- verify-state <spec-dir>`. If it exits non-zero
   (forged/stale approval, fabricated evidence), the verdict is **FAIL** — report it and stop.
3. Do **not** modify source code, tests, contracts, or spec files in this skill —
   this is a read-and-report pass. Open findings for a follow-up task.
4. Never hand-edit `status.yaml`. Use only `sdlc.cs evidence|gate`, which record real exit codes.

## Verification steps

Prefer `/sdlc-run-gates <spec-dir>`, which runs and records every deterministic gate below.
Execute each command yourself and quote its real exit code; anything you did not run is "not run".

### Step 1 — Build gate

```powershell
dotnet run scripts/sdlc/sdlc.cs -- evidence <spec-dir> build dotnet build --no-restore -warnaserror
dotnet format --verify-no-changes
```

If the repository has no .NET solution yet, report "build: not run (no projects)". Do not mark it passed.

### Step 2 — Test gate

```powershell
dotnet run scripts/sdlc/sdlc.cs -- evidence <spec-dir> unit_tests dotnet test --no-build
```

Record: total tests, passed, failed, skipped (from the real output).

### Step 3 — Contract gate

```powershell
dotnet run scripts/sdlc/sdlc.cs -- gate <spec-dir> contract
dotnet run scripts/sdlc/sdlc.cs -- drift specs/<NNN>/contracts/openapi.yaml --old=<baseline.yaml>
```

Record exit codes (`drift` exit 2 = breaking) and any reported drift. Without a baseline,
state "drift: not run (no baseline)".

### Step 4 — Traceability gate

```powershell
dotnet run scripts/sdlc/sdlc.cs -- gate <spec-dir> traceability
```

Coverage is derived from AC IDs found in test sources, not from markdown claims.
Additionally check manually:
- Requirements without acceptance criteria.
- API operations without a security decision.
- UX states without test coverage.
- Incomplete or unlinked tasks.
- Tests referencing removed/changed requirements.

### Step 5 — Security check

```powershell
dotnet run scripts/sdlc/sdlc.cs -- secrets .
```

Then review the implementation for:
- Hard-coded secrets or credentials.
- API operations missing authentication/authorization enforcement.
- Error responses exposing stack traces or sensitive data.
- Input validation gaps (SQL injection, XSS, overly permissive inputs).

Report each finding with severity: Critical / High / Medium / Low.

### Step 6 — Specification gate (retroactive)

Verify the implementation is within the approved scope:
- No features or behaviors implemented beyond the spec.
- No API operations or fields added without a corresponding requirement ID.
- No unexplained files modified outside the feature slice.

### Step 7 — Architecture gate

Check:
- No cross-slice direct handler-to-handler calls.
- No business logic inside endpoints.
- No secrets in `appsettings.json` or logs.
- No unapproved new NuGet packages.

### Step 8 — Produce validation report

Write `specs/<NNN>/validation-report.md`:

```markdown
# Validation Report — <Feature Name>

Date: <date>
Reviewer: Quality Reviewer agent

## Gate results

| Gate | Status | Evidence |
|---|---|---|
| Specification | PASSED/FAILED | spec.md sections complete |
| Contract | PASSED/FAILED | validate-openapi.ps1 exit 0 |
| Architecture | PASSED/FAILED | No violations found |
| Build | PASSED/FAILED | dotnet build exit 0 |
| Tests | PASSED/FAILED | N passed, M failed |
| Security | PASSED/FAILED | N findings (list) |
| Traceability | PASSED/FAILED | N ACs covered |
| Review | PASSED/FAILED | Scope confirmed |

## Findings (prioritised)

| Severity | Finding | File | Gate |
|---|---|---|---|

## Missing coverage

| AC ID | Status |
|---|---|

## Recommendation

PASS / CONDITIONAL PASS (conditions: ...) / FAIL (blockers: ...)

## Commands executed

| Command | Exit code | Notes |
|---|---|---|
```

### Step 9 — State

Do not edit `status.yaml`. The commands above already recorded their runs.
Tell the maintainer the verdict; if PASS they run (interactively):
`dotnet run scripts/sdlc/sdlc.cs -- approve <spec-dir> verification <their-name>`

## Stop conditions

- Do not fabricate gate results — all statuses must be derived from actual command output.
- Do not edit source or tests in this skill.
- A conditional pass requires the conditions to be listed and tracked as follow-up tasks.

## Outputs

- `specs/<NNN>/validation-report.md`

## Handoff

**PASS**: Create the pull request with the validation report attached.
**CONDITIONAL PASS**: Address listed conditions, then re-run `/sdlc-converge-feature`.
**FAIL**: Fix blocking findings via the relevant specialist skill, then re-run.
