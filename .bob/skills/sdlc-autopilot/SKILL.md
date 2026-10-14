---
name: sdlc-autopilot
description: >-
  Use when the user gives a PRD or feature description, or asks to resume or get the
  status of a feature. Drives the whole SDLC for them (specify, clarify, plan,
  contract, UX, tasks, implementation, tests, verification), runs every command
  itself, and asks the user for an explicit yes in chat before every step.
---

# SDLC — Autopilot (chat-driven, approval before every step)

You run the whole flow. **The user never runs a shell command.** You run every command yourself
(including gates and approval recording). In return, you never move forward without the
user's explicit yes in this chat.

Run in **Agent** mode. Follow `AGENTS.md`; the data layer is **stub data**.

## The approval rule (non-negotiable)

You must ask the user for approval **before every step**. A "step" is the next stage, the next
task, or the next skill you would run.

1. After you finish a piece of work, **stop**, show the result, and ask:
   "Do you approve this and want me to continue with <exact next step>?"
2. After the user **answers questions** (for example Q-001..Q-00N), do **not** continue. Apply their
   answers, re-run the gate, show what changed, then ask the same approval question.
3. After the user **asks you a question**, answer it, then ask the approval question again. A question
   is never approval.
4. After the user **changes their mind or requests edits**, make the edits, re-run the gate, show the
   result, and ask again.
5. Use the `ask_followup_question` tool for the approval question, with at least these choices:
   "Approve and continue", "Not yet - I have changes or questions". Never combine approval for two
   different steps in one question.
6. Only an unambiguous yes counts: "approve", "approved", "yes", "go ahead", "continue",
   or the "Approve and continue" choice. Silence, "ok maybe", a question, or answers to Q-IDs are **not** approval.
   If unsure, ask again.
7. Do not infer approval from earlier turns. Each step needs its own fresh yes.

## Recording stage approval (you do it, the user just says yes)

Stage approvals (`specification`, `clarification`, `architecture`, `api_contract`, `ux`,
`task_decomposition`, `verification`) are recorded **by you** after the user says yes in chat:

```powershell
dotnet run scripts/sdlc/sdlc.cs -- approve-chat <spec-dir> <stage> "<user's name>" "<the user's reply, word for word>"
```

- Ask the user for their name **once** at the start ("Whose name should I record as approver?") and reuse it.
- Pass the user's reply **verbatim**. Never paraphrase it, never write it yourself. It is stored in the
  hash-protected ledger so reviewers can see what was actually said.
- Never call `approve-chat` in the same turn as the work it approves. The approval must come from a
  user message that arrives **after** you showed them the finished artifact and the gate result.
- Never use `approve-chat` for a stage whose gate has not passed in this session (the tool refuses anyway).
- If the command is refused (gate not passed, earlier stage not approved, chat approval disabled), tell
  the user the exact reason and fix the cause. Do not work around it, do not edit `status.yaml`.
- If `approve-chat` reports "Chat approval is disabled", the team requires terminal approval. Stop and
  tell the user; do not ask them to disable the setting.

## Status updates and resuming

At the **start of every run** (including "resume", "continue", "what is the status") run:

```powershell
dotnet run scripts/sdlc/sdlc.cs -- status
dotnet run scripts/sdlc/sdlc.cs -- verify-state
```

Then tell the user in plain words: the feature, stages done (with who approved), the first open stage,
check results (say "not run" for anything not run), open tasks, and any integrity problem
(STALE, forged, or edited approval). Never skip this, even on a fresh message in a long chat.

- If `status` fails because no feature exists, start with step 1 below.
- If `state integrity` reports a problem, stop, explain it, and ask how to proceed. A STALE approval
  means an approved file changed: re-run its gate and ask for approval again.
- Resume at the **first stage that is not approved or skipped**. Do not redo approved stages.
  Then ask the approval question for that step; do not start working just because the user said "resume".

Give a short status update (one block: done / now / next / blockers) after every stage and
whenever the user asks.

## Sequence

For each row: (a) ask approval to **start** the step, (b) do the work with the named skill (open its
`SKILL.md` and follow it exactly), (c) run the gate, fixing failures up to 3 times, (d) show the result
and the gate exit code, (e) ask approval to record the stage and move on, (f) record with `approve-chat`.

| # | Work (skill) | Gate command | Stage recorded |
|---|---|---|---|
| 1 | `speckit-specify` + `sdlc-analyze-prd`, then `sdlc-init-feature` | `gate specification` | `specification` |
| 2 | `speckit-clarify` (ask the user the open questions here, one batch) | `gate clarification` | `clarification` |
| 3 | `speckit-plan` | `verify-state` | `architecture` |
| 4 | `sdlc-contracts` | `gate contract` | `api_contract` |
| 5 | `sdlc-ux` (API-only: `set-stage ... ux skipped`, explain why and ask first) | `gate ux` | `ux` |
| 6 | `speckit-tasks` | `verify-state` | `task_decomposition` |
| 7 | `sdlc-implement-slice` **one task at a time**, then `sdlc-generate-tests` | `evidence` build + unit_tests, `gate traceability` | none; ask before **each** task |
| 8 | `sdlc-run-gates`, then `sdlc-converge-feature` | all gates | `verification` |

Open questions (`[QUESTION]`, `Q-001` ...): list them numbered with a suggested default for each.
The user may answer them, ask you to propose defaults, or ask questions. Whatever they do, you then
update the spec, re-run the gate, and **ask for approval again**. Answering is not approving.

Before step 7 starts, set the stage with
`dotnet run scripts/sdlc/sdlc.cs -- set-stage <spec-dir> implementation in_progress`.

## Always stop and ask (even more strictly)

- A gate still fails after 3 fix attempts.
- A critical PRD question is open (security, data integrity, authorization, business rules).
- A breaking API change (`drift` exit 2), or any high-risk item in `AGENTS.md` (auth/authz,
  destructive migration, secrets, infrastructure). For these, name the risk in the approval question
  and require the user to say so explicitly.
- Anything that would need a real database, ORM or external service (stub data only).

## Ground rules

- Never ask the user to run a shell command. If something cannot be done by you, say so and why.
- Only tool output proves a gate passed. Quote real exit codes. Anything not run is "not run (reason)".
- Never edit `status.yaml` by hand. Never record an approval the user did not give.
- Never claim "verified" unless `verify-state` exits 0 and `verification` is approved.

## Final report

Stages approved (who, when, channel), last recorded result per check, anything "not run" with the
reason (for example build and tests while no .NET project exists), files created, and the
"not verified (stub data)" note for persistence.
