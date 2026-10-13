---
name: sdlc-contracts
description: >-
  Use when the user wants to design or validate the OpenAPI contract for a
  feature — produces specs/<NNN>/contracts/openapi.yaml from the approved spec,
  runs structural validation, checks for breaking changes, and verifies the
  contract gate.
---

# SDLC — Design and Validate API Contract

You are acting as the **Contract Engineer** for this repository.

## Inputs

- `$ARGUMENTS` — path to the feature spec directory (e.g., `specs/001-orders`)
  **or** the feature name/number. If empty, resolve the current feature from
  `.specify/feature.json`.

## Pre-flight checks

1. Read `AGENTS.md` and `.bob/instructions/api-contracts.md`.
2. Run `dotnet run scripts/sdlc/sdlc.cs -- verify-state <spec-dir>` (must exit 0) and confirm
   `stages.specification.status` is `approved` in `status.yaml`. If not, STOP: the
   maintainer must approve the specification first.
3. Check for an existing `specs/<NNN>/contracts/openapi.yaml`. If one exists,
   treat it as the current approved contract; compare your proposal against it.

## Contract design steps

### Step 1 — Identify operations

From the acceptance criteria and functional requirements, enumerate all API
operations. For each:
- HTTP method + path
- Who calls it (actor/role)
- Authentication required?
- What it creates/reads/modifies/deletes

### Step 2 — Draft OpenAPI YAML

Create `specs/<NNN>/contracts/openapi.yaml` following `.bob/instructions/api-contracts.md`.

Every operation **must** include:
- `operationId` (camelCase, globally unique)
- `summary`
- All path/query/header parameters with `schema`, `required`, `description`
- `requestBody` with schema and `required: true` where applicable
- Responses: `200`/`201`, `400`, `401`, `403`, `404`, `409`, `500`
- `security` object
- At least one `x-example` or inline example for the success response
- At least one `x-example` or inline example for the `400` response

All schema components go under `#/components/schemas/`.

### Step 3 — Document contract source-of-truth policy

If `docs/decisions/adr-001-openapi-policy.md` does not exist, create it choosing one:
- **Design-time first**: committed YAML is authoritative; implementation must conform.
- **Code-first**: implementation generates OpenAPI; committed YAML is a snapshot for drift detection.

### Step 4 — Validate the contract

Run the gate (validates AND records the result in `status.yaml`):
```powershell
dotnet run scripts/sdlc/sdlc.cs -- gate <spec-dir> contract
```

Report the exit code and any errors. Do **not** declare the contract gate closed
unless it exits `0`.

### Step 5 — Breaking-change detection

If a previously approved contract exists (e.g. saved from git: `git show <ref>:<path> > old.yaml`):
```powershell
dotnet run scripts/sdlc/sdlc.cs -- drift specs/<NNN>/contracts/openapi.yaml --old=old.yaml
```
Exit `2` means breaking changes. They require recorded human approval before proceeding.
With no baseline the tool only warns; that is NOT evidence of "no drift".

### Step 6 — Contract gate check

**CONTRACT GATE: PASSED** only if:
- `gate <spec-dir> contract` exits `0`.
- All operations have required responses and security declarations.
- Breaking-change approval is on record (if applicable).

Otherwise: **CONTRACT GATE: BLOCKED** with specific failure details.

## Stop conditions

- Do not begin implementation while the contract gate is blocked.
- Do not change an approved contract without recording the change and re-validating.

## Outputs

- `specs/<NNN>/contracts/openapi.yaml`
- `docs/decisions/adr-001-openapi-policy.md` (if not present)
- Gate status in the conversation

## Handoff

Next step: `/sdlc-ux` (if not already done) → `/speckit-tasks` → `/sdlc-implement-slice`
