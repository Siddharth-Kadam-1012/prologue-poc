---
name: sdlc-implement-slice
description: >-
  Use when the user wants to implement a single vertical slice task from
  specs/<NNN>/tasks.md — implements UI, endpoint, business logic, stub-data store,
  error handling, and co-located tests, following the approved contracts.
---

# SDLC — Implement a Single Vertical Slice

You are acting as the **Slice Implementer** for this repository.

## Inputs

- `$ARGUMENTS` — task ID (e.g., `T005`) and optionally the feature directory.
- `specs/<NNN>/tasks.md` — the full task list.
- `specs/<NNN>/contracts/openapi.yaml` — the approved API contract.
- `specs/<NNN>/ux/component-map.yaml` — the approved component map.
- `specs/<NNN>/spec.md` — acceptance criteria.

## Pre-flight checks

1. Read `AGENTS.md`, `.bob/instructions/dotnet.md`, `.bob/instructions/vertical-slices.md`,
   `.bob/instructions/api-contracts.md`, `.bob/instructions/frontend.md`, `.bob/instructions/testing.md`.
2. Run `dotnet run scripts/sdlc/sdlc.cs -- verify-state <spec-dir>` — it must exit 0
   (this also detects stale approvals). Then read `specs/<NNN>/status.yaml` and verify:
   - `specification`, `api_contract` and `task_decomposition` are `approved`
   - `ux` is `approved` or `skipped`
   If any are not, STOP. Tell the user which stage needs approval, ask for their yes in chat, then record it
   with `approve-chat` before continuing.
3. Check `git status` — record any pending unrelated changes and **do not touch them**.
4. Read the target task from `tasks.md`. Confirm prerequisites are met.

## Implementation steps

### Step 1 — Scope confirmation

State exactly:
- Which task ID you are implementing.
- Which files you expect to create or modify.
- Which requirement IDs (REQ-XXX-NNN) this task satisfies.
- Which acceptance criteria (AC-XXX-NNN) it will make testable.

Stop if the scope is unclear. Do not begin coding until scope is confirmed.

### Step 2 — Implement the slice

Follow the vertical-slice structure from `.bob/instructions/vertical-slices.md`:

```
src/Features/<Domain>/<UseCase>/
├── Endpoint.cs
├── Command.cs or Query.cs
├── Handler.cs
├── Validator.cs (if needed)
└── Response.cs (if needed)
```

Rules:
- Endpoints are thin: receive HTTP, call handler, return HTTP result.
- Handlers contain business logic only (no `HttpContext` dependency).
- Validate at the boundary; return `ProblemDetails` for errors.
- Use the existing DI registration pattern.
- **Data access uses stub data, not a database.** Follow `.bob/instructions/dotnet.md` § Stub data:
  - Define one small feature-specific store interface (e.g. `IOrderStore`) in the feature folder.
  - Implement it as a thread-safe in-memory stub seeded from deterministic, synthetic seed data
    (e.g. `StubData/OrderSeed.cs`) and register it in DI.
  - The stub must follow the OpenAPI contract exactly (validation, `404`, `409`, etc.).
  - Seed data must cover every UX state (empty, populated, error, boundary).
  - Do **not** add a database, ORM, migrations, connection strings or a generic `IRepository<T>`.
  - Do not describe persistence as implemented or verified; say "stub data only".
- Implement exactly what the approved contract specifies — do not add extra fields or operations.
- Do not modify unrelated files.

### Step 3 — Implement co-located tests

For each acceptance criterion covered by this task:
- Unit test for the handler's business logic.
- Integration test via `WebApplicationFactory` covering the full HTTP pipeline, running against the seeded stub store
  (fresh store per test so tests are order-independent).
- Put the AC ID (e.g. `AC-ORD-001`) in the test name or a comment. `sdlc.cs traceability`
  derives coverage from these references in `tests/`; marking a row covered in
  `traceability.md` without a referencing test is rejected.

### Step 4 — Build and test

Run through the evidence recorder so the real exit code lands in `status.yaml`:
```powershell
dotnet run scripts/sdlc/sdlc.cs -- evidence <spec-dir> build dotnet build --no-restore -warnaserror
dotnet run scripts/sdlc/sdlc.cs -- evidence <spec-dir> unit_tests dotnet test --no-build
```

Report exact exit codes and failure details. Do **not** suppress warnings or
delete failing tests to achieve a green run.

### Step 5 — Update status

- Never hand-edit `status.yaml`; `evidence` already recorded the run.
- Mark the task `[X]` in `specs/<NNN>/tasks.md` only if both commands exited 0.
- If this was the last task, report it to the user and ask whether to continue with `/sdlc-generate-tests` and verification.

## Stop conditions

- Do not implement beyond the current task's scope.
- Do not alter the approved contract or spec to make the implementation easier.
- Do not mark build or tests as passed without actual tool output confirming it.

## Outputs

- Feature slice source files.
- Co-located or test-project test files.
- Updated `specs/<NNN>/status.yaml`.
- Updated `specs/<NNN>/traceability.md`.

## Handoff

Next: implement the next uncompleted task, **or** run `/sdlc-generate-tests` for
additional test coverage, **or** run `/sdlc-converge-feature` when all tasks are done.
