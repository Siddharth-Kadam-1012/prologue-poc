---
name: sdlc-run-gates
description: >-
  Use to run the deterministic gates for a feature (spec, contract, UX,
  traceability, build, tests, secrets, workflow state) and record their real
  exit codes in status.yaml. Reports anything not run as "not run".
---

# SDLC — Run Deterministic Gates

Only tool output proves a gate passed. Run each command, quote its real exit code,
and report anything you could not or did not run as **not run (reason)**.

## Inputs

- `$ARGUMENTS` — feature directory; if empty it is resolved from `.specify/feature.json`.
- Optional: which gates to run (default: all that apply to the feature's current stage).

## Gates

Run from the repository root. `<d>` = the feature directory.

| Gate | Command | Applies when |
|---|---|---|
| State integrity | `dotnet run scripts/sdlc/sdlc.cs -- verify-state <d>` | always (detects forged/stale approvals) |
| Specification | `dotnet run scripts/sdlc/sdlc.cs -- gate <d> specification` | `spec.md` exists |
| Clarification | `dotnet run scripts/sdlc/sdlc.cs -- gate <d> clarification` | before approving clarification (no open questions) |
| Contract | `dotnet run scripts/sdlc/sdlc.cs -- gate <d> contract` | `contracts/openapi.yaml` exists |
| Contract drift | `dotnet run scripts/sdlc/sdlc.cs -- drift <d>/contracts/openapi.yaml --old=<baseline>` | a baseline exists (exit 2 = breaking) |
| UX | `dotnet run scripts/sdlc/sdlc.cs -- gate <d> ux` | `ux/component-map.yaml` exists |
| Traceability | `dotnet run scripts/sdlc/sdlc.cs -- gate <d> traceability` | tests exist |
| Build | `dotnet run scripts/sdlc/sdlc.cs -- evidence <d> build dotnet build --no-restore -warnaserror` | a .sln/.csproj exists |
| Unit tests | `dotnet run scripts/sdlc/sdlc.cs -- evidence <d> unit_tests dotnet test --no-build` | a test project exists |
| Secrets | `dotnet run scripts/sdlc/sdlc.cs -- secrets .` | always |

`gate` and `evidence` write the real result into `status.yaml` themselves. Never edit it by hand.

## Output format

```
Gate            Result      Exit  Note
state           PASS        0
specification   PASS        0
contract        NOT RUN     -     no contracts/openapi.yaml yet
build           NOT RUN     -     no .NET projects in repository
```

Then state which stage can be put to the user for approval next. Ask the user in chat; only after their explicit yes,
record it with `dotnet run scripts/sdlc/sdlc.cs -- approve-chat <d> <stage> "<name>" "<verbatim reply>"`.
The tool refuses unless the required passing runs are recorded.

## Stop conditions

- Any failing gate: show the output, do not proceed, do not retry with weaker inputs.
- Never mark something passed because a previous run or a document says so.
