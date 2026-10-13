# Repository Baseline

> **Auto-generated.** Update this file whenever a technology decision is made.
> This is the authoritative record of what this repository actually contains.

## Date recorded

2026-10-13

## Status

Repository initialised with the autonomous SDLC workflow framework.
No application code exists yet.

---

## Toolchain

| Tool | Version (detected) | Notes |
|---|---|---|
| .NET SDK | 10.0.401 | `dotnet --version` |
| Node.js | 24.18.0 | `node --version` |
| SpecKit CLI | 1.0.12 | `specify version` |
| PowerShell | 5.1.26100.9444 | Windows built-in |
| Python | 3.14.5 | Used by SpecKit CLI |
| pyyaml | 6.0.3 | SpecKit dependency |
| Git | Present | `C:\Program Files\Git\cmd\git.exe` |

---

## Application stack (undecided)

No `.sln`, `.csproj`, or application code exists yet.
These items must be decided before the first feature is implemented.
Record each decision as an ADR in `docs/decisions/`.

| Aspect | Status | ADR |
|---|---|---|
| Runtime / framework | Undecided | — |
| Target framework | Undecided | — |
| Frontend | Undecided | docs/decisions/adr-002-frontend.md |
| Database / ORM | None - stub data (in-memory, synthetic seed data) for the current phase | ADR required before adding a real database |
| Authentication | Undecided | ADR to be created when decided |
| Component library | Undecided | docs/decisions/adr-002-frontend.md |

---

## Workflow infrastructure (installed)

| Component | Location |
|---|---|
| Root constitution / AGENTS.md | `AGENTS.md` |
| SpecKit constitution | `.specify/memory/constitution.md` |
| SpecKit project config | `.specify/init-options.json` |
| SpecKit extensions (hooks) | `.specify/extensions.yml` |
| Stage/check definitions | `Cfg` class in `scripts/sdlc/sdlc.cs` |
| SDLC full workflow | `.specify/workflows/sdlc-full/workflow.yml` |
| SpecKit built-in workflow | `.specify/workflows/speckit/workflow.yml` |
| Spec template override | `.specify/templates/overrides/spec-template.md` |
| Traceability template | `.specify/templates/traceability-template.md` |
| Test-plan template | `.specify/templates/test-plan-template.md` |
| Validation-report template | `.specify/templates/validation-report-template.md` |
| Bob scoped instruction files | `.bob/instructions/` (dotnet, vertical-slices, api-contracts, frontend, testing) — referenced from `AGENTS.md`, not auto-loaded |
| Bob custom modes | `.bob/custom_modes.yaml` (7 modes) |
| Bob skills (SpecKit, built-in) | `.bob/skills/speckit-*/` |
| Bob skills (SDLC, custom) | `.bob/skills/sdlc-*/` |
| SDLC CLI tool | `scripts/sdlc/sdlc.cs` (.NET 10 file-based app, 11 subcommands + selftest) |
| PowerShell wrappers | `scripts/validate-openapi.ps1`, `check-contract-drift.ps1`, `validate-specs.ps1`, `validate-traceability.ps1`, `validate-workflow-state.ps1` |
| CI workflow | `.github/workflows/sdlc-gates.yml` (5 jobs; never executed on GitHub) |

---

## Conventions not yet decided

- OpenAPI source-of-truth policy (design-time-first vs code-first) → `docs/decisions/adr-001-openapi-policy.md`
- Frontend framework → `docs/decisions/adr-002-frontend.md`
- Database/ORM → create an ADR when chosen
- Authentication scheme → create an ADR when chosen
- Deployment platform → TBD
- Branching strategy → TBD
