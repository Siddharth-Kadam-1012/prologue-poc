# Developer Usage Guide: Autonomous SDLC Workflow

## What this gives you

A SpecKit-driven, IBM Bob-native workflow with deterministic gates. Agents do the drafting; **tool output and
human approvals** decide whether a stage may advance.

| Layer | Where | Role |
|---|---|---|
| Rules for agents | `AGENTS.md`, `.bob/instructions/*.md` | Conventions; referenced from `AGENTS.md` |
| Roles | `.bob/custom_modes.yaml` (7 modes) | Scoped edit permissions per role |
| Procedures | `.bob/skills/sdlc-*/`, `.bob/skills/speckit-*/` | Invoked as `/skill-name` |
| Hooks | `.specify/extensions.yml` | Prompts before/after SpecKit commands |
| Workflow | `.specify/workflows/sdlc-full/workflow.yml` | `specify workflow run` automation |
| Gates | `scripts/sdlc/sdlc.cs` (+ `scripts/*.ps1` wrappers) | Deterministic checks, state, approvals |
| CI | `.github/workflows/sdlc-gates.yml` | Same checks on push/PR |

No application code exists yet. Build/test gates report **not run** until a .NET project is added.

## Prerequisites

| Tool | Check |
|---|---|
| .NET SDK 10 | `dotnet --version` |
| SpecKit CLI (1.0.x, Bob integration) | `specify version` |
| IBM Bob | open this folder |
| Git (recommended) | `git --version` |

First `sdlc.cs` run restores NuGet packages (network needed once).

## The stages and who may do what

Stages, in order: `specification`, `clarification`, `architecture`, `api_contract`, `ux`, `task_decomposition`,
`implementation`, `verification`. `clarification` and `ux` may be `skipped`; nothing else.

| Action | Who | Command |
|---|---|---|
| Create state files | agent or human | `sdlc.cs init-feature <dir>` |
| Run a gate / record evidence | agent or human | `sdlc.cs gate <dir> <gate>` / `sdlc.cs evidence <dir> <check> <cmd...>` |
| Mark stage in progress/failed/skipped | agent or human | `sdlc.cs set-stage <dir> <stage> <status>` |
| **Approve a stage** | **you, by an explicit yes** | in chat: agent records it via `sdlc.cs approve-chat` (your reply is stored); or `sdlc.cs approve <dir> <stage> <name>` in your own terminal |

`approve` (terminal) refuses when stdin is redirected (agents/CI) and asks you to retype your name. `approve-chat` skips that prompt (the chat yes is the approval) but enforces the same rules. Both refuse unless:
earlier stages are approved/skipped, required artifacts exist, and the stage's required checks have a recorded
**passing** run. It stores a SHA-256 of each artifact. If an artifact changes afterwards, `verify-state` reports
the approval as **STALE** and fails until you re-run the gate and re-approve. Hand-editing `status.yaml`
(forged approvals, fake "passed", edited ledger) is detected by `verify-state`.

Required passing checks: specification -> `spec`; clarification -> `spec_strict`; api_contract -> `contract_tests`;
ux -> `ux`; verification -> `spec`, `traceability`, `build`, `unit_tests` (+ `contract_tests` if a contract exists).

> Limitation: this is tamper-*evident*, not tamper-*proof*. Someone with write access can edit both the artifact
> and the status file consistently. Real enforcement comes from CODEOWNERS on `specs/*/status.yaml` plus branch
> protection (see "One-time setup").

## Run a feature - one command (recommended)

In Bob, in **Agent** mode, paste:

```
/sdlc-autopilot <your PRD text or path to the PRD file>
```

Bob runs **every command itself** (gates, evidence, approval recording): you never type a shell command.
In return it asks for your explicit yes **before every step**:

- After finishing a piece of work it shows the result and asks "Approve and continue?" (a button prompt).
- After you answer questions (for example Q-001..Q-005), it applies your answers, re-runs the gate and asks again.
  Answering is not approving.
- After you ask a question it answers and asks again. A question is not approving.
- Only a clear yes moves it forward. When you say yes to a stage, Bob records it with
  `sdlc.cs approve-chat` and stores your reply word for word in the approval ledger.
- It also stops on its own for an unresolved critical PRD question, a breaking API change or other high-risk item,
  a gate that still fails after 3 fixes, or a need for a real database.

Status and resume: start /sdlc-autopilot (or just say "resume" or "status") in any new chat. Bob first runs
`sdlc.cs status` and `verify-state` and reports: stages done and who approved, the first open stage, last result of
each check ("not run" where applicable), open tasks, and any integrity problem. It then asks before resuming at the
first stage that is not approved. You can also read the report yourself: `dotnet run scripts/sdlc/sdlc.cs -- status`.

Chat approval is weaker than a terminal approval: the tool cannot prove you typed the "yes", only record what Bob says
you said. For stricter control set the environment variable `SDLC_REQUIRE_TERMINAL_APPROVAL=1`: `approve-chat` is then
refused and you approve with `sdlc.cs approve` in a terminal. This skill has **not been run in Bob yet**; expect to adjust it after a first trial.

## Run a feature (manual, step by step in Bob)

```
/speckit-specify       <feature description>   # creates specs/NNN-name/spec.md (+ .specify/feature.json)
/sdlc-init-feature                             # status.yaml + traceability.md
/speckit-clarify                               # resolve questions
/speckit-plan
/sdlc-contracts                                # specs/NNN/contracts/openapi.yaml
/sdlc-ux                                       # ux/user-flows.md, component-map.yaml, wireframes
/speckit-tasks
/sdlc-implement-slice  T001
/sdlc-generate-tests
/sdlc-run-gates
/sdlc-converge-feature                         # validation-report.md
```

To approve a stage by hand (instead of through `/sdlc-autopilot`), after its gate passes run in your own terminal:

```powershell
dotnet run scripts/sdlc/sdlc.cs -- gate specs/001-orders specification
dotnet run scripts/sdlc/sdlc.cs -- approve specs/001-orders specification "Alice"
```

Omit `<dir>` to use the current feature (`SPECIFY_FEATURE_DIRECTORY`, else `.specify/feature.json`).
Modes (`sdlc-orchestrator`, `prd-analyst`, `contract-engineer`, `ux-engineer`, `slice-implementer`,
`test-engineer`, `quality-reviewer`) are selected from Bob's mode picker.

## Run it as a SpecKit workflow (does NOT work here)

```powershell
specify workflow info .specify/workflows/sdlc-full/workflow.yml   # validates structure (27 steps)
specify workflow run .specify/workflows/sdlc-full/workflow.yml -i prd="<feature description>"
specify workflow resume <run-id>                                    # after each human gate
```

Tested: the first `command` step fails with `integration 'bob' CLI not found or not installed` because no Bob CLI
is on this machine's PATH, so `specify workflow run` cannot drive Bob headlessly. The workflow validates (27 steps)
and the shell steps are sound, but it only becomes usable if a Bob command-line executable is installed. Until then
use `/sdlc-autopilot`.

## Resuming

`specs/<NNN>/status.yaml` holds stage status, approval ledger and last recorded run per check.

```powershell
dotnet run scripts/sdlc/sdlc.cs -- verify-state specs/001-orders
```

Then tell Bob (orchestrator mode): "Resume feature 001: read status.yaml and tell me the next action."

## Traceability

Coverage is **derived**: an AC is covered only if its ID (e.g. `AC-ORD-001`) appears in a test source under
`tests/`, `e2e/`, `*Tests.cs`, `*.spec.ts` or `*.feature`. Marking an AC "covered" in `traceability.md` without such
a test fails the gate. Before a feature reaches `implementation`, uncovered ACs are warnings; afterwards, or with
`--strict` / `gate traceability`, they fail. The gate proves a test *mentions* the AC, not that it asserts it well -
review is still needed.

## Commands

```powershell
dotnet run scripts/sdlc/sdlc.cs -- help
dotnet run scripts/sdlc/sdlc.cs -- selftest                       # 38 checks incl. tamper/stale cases
dotnet run scripts/sdlc/sdlc.cs -- gate  <dir> specification|clarification|contract|ux|traceability|state
dotnet run scripts/sdlc/sdlc.cs -- evidence <dir> build dotnet build --no-restore -warnaserror
dotnet run scripts/sdlc/sdlc.cs -- drift <new.yaml> --old=<old.yaml>   # exit 2 = breaking change
dotnet run scripts/sdlc/sdlc.cs -- secrets .                      # never prints secret values
```

Exit codes: `0` pass, `1` fail/usage, `2` breaking contract change. Wrappers (need
`powershell -ExecutionPolicy Bypass -File` if scripts are blocked): `scripts/validate-openapi.ps1`,
`validate-specs.ps1`, `validate-traceability.ps1`, `validate-workflow-state.ps1`, `check-contract-drift.ps1`.

## Risk tiers

| Tier | Examples | Requirement |
|---|---|---|
| Low | Isolated UI, unit tests, internal refactors | Autonomous + deterministic gates |
| Medium | Shared components, persistence, non-breaking API additions | Impact analysis (`/sdlc-change-impact`) + test evidence |
| High | Auth/authz, destructive migrations, breaking API, secrets, infra | **Explicit human approval before action** |

Breaking API changes show up as `drift` exit 2; record the decision under `decisions` via an ADR or PR review.

## One-time setup (not done by this repo)

1. `git init` / push to GitHub - the workspace was not a git repository, so CI has **never run**.
2. Replace `@<team-lead>` / `@<tech-leads>` placeholders in `CODEOWNERS`.
3. Enable branch protection: required review (code owners) and required status checks from `sdlc-gates.yml`.
4. Decide the open ADRs: `docs/decisions/adr-001-openapi-policy.md`, `adr-002-frontend.md`; create ADRs for
   authentication when chosen. The data layer is **stub data** for now (see `.bob/instructions/dotnet.md`);
   a real database/ORM needs its own ADR.
5. Add the .NET solution; then `build`/`unit_tests` gates become real.

## Troubleshooting

| Problem | Fix |
|---|---|
| `sdlc.cs` slow on first run | NuGet restore + compile; later runs are cached |
| "No spec directory given and none could be resolved" | Pass `<dir>`, or run `/speckit-specify` first |
| `verify-state`: STALE approval | Artifact changed after approval: re-run its gate, `set-stage ... in_progress`, re-approve |
| `approve`: "check ... has no recorded passing run" | Run the gate/evidence command first |
| `approve`: "stdin is redirected" | Expected for agents; use `approve-chat` after your yes in chat, or run `approve` in your own terminal |
| `approve-chat`: "Chat approval is disabled" | `SDLC_REQUIRE_TERMINAL_APPROVAL=1` is set; approve in a terminal with `approve` |
| PowerShell blocks `.ps1` | `powershell -ExecutionPolicy Bypass -File <script>` |
| Skill not listed in Bob | Name must match `^[a-z0-9]+(-[a-z0-9]+)*$`; restart Bob |
