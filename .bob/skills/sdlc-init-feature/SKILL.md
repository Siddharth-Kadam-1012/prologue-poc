---
name: sdlc-init-feature
description: >-
  Use right after /speckit-specify to initialize a feature's workflow state —
  creates specs/<NNN>/status.yaml and traceability.md via the sdlc.cs tool and
  verifies the result. Never create these files by hand.
---

# SDLC — Initialize Feature State

## Inputs

- `$ARGUMENTS` — the feature directory (e.g. `specs/001-orders`). If empty, the tool resolves the
  current feature from `$SPECIFY_FEATURE_DIRECTORY` or `.specify/feature.json`.

## Steps

1. Confirm `<spec-dir>/spec.md` exists (created by `/speckit-specify`). If not, stop and say so.
2. Run:
   ```powershell
   dotnet run scripts/sdlc/sdlc.cs -- init-feature <spec-dir>
   ```
   It is idempotent: existing `status.yaml` / `traceability.md` are left untouched.
3. Run:
   ```powershell
   dotnet run scripts/sdlc/sdlc.cs -- verify-state <spec-dir>
   ```
4. Report both real exit codes. If either is non-zero, show the output and stop.

## Rules

- Do not write or edit `status.yaml` yourself. It is managed by `sdlc.cs`; approvals are
  hash-verified and hand edits are detected as tampering by `verify-state`.
- Do not approve any stage. Approval is a human action: `sdlc.cs approve` refuses to run
  non-interactively.

## Handoff

`/speckit-clarify` (if questions remain) → `/speckit-plan`. The specification gate is
`dotnet run scripts/sdlc/sdlc.cs -- gate <spec-dir> specification`.
