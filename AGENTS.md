# Repository Constitution — Autonomous SDLC

## Stack baseline (see `docs/architecture/repository-baseline.md`)
No application code exists yet. Until a project is added, treat every item below as
**policy / intent, not a detected fact**. Re-inspect the repository before relying on any of them.
- **Target runtime (intent):** .NET 10 / ASP.NET Core 10 for new projects. Never retarget an existing project silently.
- **Data layer: stub data (current phase).** There is no database or ORM. Slices read and write through a
  feature-specific store interface backed by deterministic, synthetic in-memory stub data. See `.bob/instructions/dotnet.md` § Stub data.
  Introducing a real database/ORM requires an ADR and human approval.
- **Frontend, auth, component library:** undecided — record an explicit decision (ADR) before choosing.
- **Test runner (intent):** xUnit unless NUnit is already present. Browser tests: Playwright.
- **SpecKit:** 1.0.12 with the IBM Bob integration (PowerShell scripts, skills in `.bob/skills/`).
- **Stage/check definitions:** the `Cfg` class in `scripts/sdlc/sdlc.cs` (single source of truth; no separate config file)

## Required workflow entry point

Every feature development cycle **must** follow the staged SDLC flow defined
in `.specify/workflows/speckit/workflow.yml` extended by the custom stages
documented in `.bob/skills/`. Use `/speckit-specify` to start a feature.

## Core operating rules

1. **Inspect before generating.** Read existing code, config, and status files
   before proposing or creating anything. Never overwrite unrelated user changes.
2. **Specification first.** Ambiguous requirements must be clarified before
   implementation begins. Record questions and assumptions explicitly.
3. **Contracts before implementation.** OpenAPI contract must be reviewed and
   committed before the corresponding slice is implemented.
4. **Vertical slices.** Implement one user-visible capability end-to-end at a
   time. Do not batch all controllers, then all services.
5. **Tests are evidence.** Derive tests from requirements and acceptance
   criteria — not only from implementation shape.
6. **Deterministic enforcement.** Only script/CI output proves a gate passed.
   An agent must never claim success without actual tool output.
7. **Human approval at risk boundaries.** Auth/authz changes, destructive
   migrations, breaking API changes, secrets, and production operations
   require explicit human approval before proceeding.
8. **No invented success.** Never mark a check passed by editing a status file.
9. **Least privilege.** Modify only the files required for the assigned task.
10. **No secrets in files or logs.**

## Scoped instruction files

| Concern | File |
|---|---|
| .NET conventions | `.bob/instructions/dotnet.md` |
| Vertical slices | `.bob/instructions/vertical-slices.md` |
| API contracts | `.bob/instructions/api-contracts.md` |
| Frontend | `.bob/instructions/frontend.md` |
| Testing | `.bob/instructions/testing.md` |

## Available skills (SpecKit)

| Skill | Purpose |
|---|---|
| `/speckit-constitution` | Establish project principles |
| `/speckit-specify` | Create feature specification |
| `/speckit-clarify` | Clarify ambiguous requirements |
| `/speckit-plan` | Create implementation plan |
| `/speckit-checklist` | Validate requirements completeness |
| `/speckit-tasks` | Generate dependency-ordered tasks |
| `/speckit-analyze` | Cross-artifact consistency report |
| `/speckit-implement` | Execute a task |
| `/speckit-converge` | Assess remaining work and append tasks |

## Custom SDLC skills (this repository)

| Skill | Purpose |
|---|---|
| `/sdlc-autopilot` | **One command**: agent runs every command; asks the user for an explicit yes in chat before every step; also status/resume |
| `/sdlc-init-feature` | Create `status.yaml` + `traceability.md` via `sdlc.cs` |
| `/sdlc-run-gates` | Run deterministic gates, record real exit codes |
| `/sdlc-analyze-prd` | PRD → structured requirement set |
| `/sdlc-contracts` | Design and validate OpenAPI contract |
| `/sdlc-ux` | UX flows, wireframes, component map |
| `/sdlc-implement-slice` | Implement a single vertical slice |
| `/sdlc-generate-tests` | Generate tests from acceptance criteria |
| `/sdlc-change-impact` | Analyze impact of a requirement or contract change |
| `/sdlc-converge-feature` | Full verification and PR report |

## Available custom modes

| Mode slug | Purpose |
|---|---|
| `prd-analyst` | PRD analysis and requirement structuring |
| `contract-engineer` | OpenAPI contract design and validation |
| `ux-engineer` | UX flows, wireframes, component maps |
| `slice-implementer` | Single-slice implementation |
| `test-engineer` | Test generation from acceptance criteria |
| `quality-reviewer` | Independent quality and coverage review |
| `sdlc-orchestrator` | Stage coordination and approval management |

## Feature artifacts location

All feature artifacts live under `specs/<NNN>-<feature-name>/`.
Never put feature-specific artifacts inside `.bob/` or `.specify/`.

## Traceability ID scheme

`PRD-XXX-NNN` · `REQ-XXX-NNN` · `AC-XXX-NNN` · `API-XXX-NNN`
`UX-XXX-NNN` · `TASK-XXX-NNN` · `TEST-XXX-NNN` · `E2E-XXX-NNN`

IDs must be stable; never reuse an ID for a different requirement.

## Risk tiers

| Tier | Examples | Requirement |
|---|---|---|
| Low | Isolated UI, unit tests, internal refactors | Autonomous + deterministic validation |
| Medium | Shared components, persistence changes, non-breaking API additions | Targeted review + impact analysis + test evidence |
| High | Auth/authz, sensitive data, destructive migrations, breaking API, secrets, infra | **Explicit human approval before action** |
