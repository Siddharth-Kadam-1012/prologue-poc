---
name: sdlc-ux
description: >-
  Use when the user wants to define UX requirements and generate grayscale
  wireframes and component maps for a feature — produces user-flows.md,
  component-map.yaml, and grayscale HTML wireframes tied to the approved spec.
---

# SDLC — UX Requirements, Wireframes, and Component Map

You are acting as the **UX Engineer** for this repository.

## Inputs

- `$ARGUMENTS` — feature spec directory path or feature name/number.
- The approved `specs/<NNN>/spec.md`.
- The approved `specs/<NNN>/contracts/openapi.yaml` (may be in progress; cross-reference it).

## Pre-flight checks

1. Read `AGENTS.md` and `.bob/instructions/frontend.md`.
2. Inspect the repository for the actual component library, routing convention, and design tokens.
   - Look for: `package.json`, `*.csproj` (Blazor), `_Imports.razor`, `components/` folder, Storybook config.
   - If no frontend exists yet: record it as a decision in `docs/decisions/adr-002-frontend.md`.
3. Read `specs/<NNN>/spec.md` to extract actors, user stories, and acceptance criteria.

## UX design steps

### Step 1 — User flows

Create `specs/<NNN>/ux/user-flows.md`:

```markdown
# User Flows — <Feature Name>

## Flow 1: <Primary action> (REQ-XXX-001)

Actor: <Persona>
Entry: <URL or trigger>
Steps:
  1. ...
  2. ...
Exit: <URL or confirmation>
Error paths: ...
```

For each flow identify every API operation it calls (link to `API-XXX-NNN` IDs).

### Step 2 — Screen specification

For each distinct screen in the flows:

```markdown
### Screen: <Name>
Route: <URL pattern>
API operations: [API-XXX-NNN, ...]
Requirement IDs: [REQ-XXX-NNN, ...]

#### States
| State | Trigger | UI behavior |
|---|---|---|
| Loading | Page enters | Skeleton / spinner |
| Empty | 0 results | Empty state message + CTA |
| Populated | Data loaded | Render data table |
| Validation error | Submit with invalid data | Field-level error messages |
| Server error | API 500 | Error banner |
| Forbidden | API 403 | Permission-denied state |
```

### Step 3 — Component map

Create `specs/<NNN>/ux/component-map.yaml`. The shape below is exactly what
`sdlc.cs gate <spec-dir> ux` validates (extra keys are allowed and ignored):

```yaml
screens:
  - id: UX-ORD-001                     # real domain code, never XXX
    name: Orders list
    route: /orders
    components:
      - name: DataTable                # real component from the repository's design system
        source: existing               # existing | gap
      - name: EmptyState
        source: gap                    # not in the repo yet -> design-system gap
    states: [loading, empty, populated, error, forbidden]   # all five are required
```

**Stub data note:** screens are populated from stub data in this phase. For each screen's states, note in
`user-flows.md` which stub seed scenario produces it (e.g. `empty` -> empty seed set, `error` -> a seed
record or trigger value that causes the failure). This keeps every state reproducible without a database.

Rules enforced by the gate: at least one screen, a non-placeholder `id`, all five
states per screen, and every component `source` is `existing` or `gap`.

If the repository has **no design system yet**: mark every component `source: gap`
and do **not** reference fictional components.

Validate (and record the result in `status.yaml`):
```powershell
dotnet run scripts/sdlc/sdlc.cs -- gate <spec-dir> ux
```

### Step 4 — Grayscale wireframes

Create `specs/<NNN>/ux/wireframes/<screen-id>.html` — one file per screen.

Each wireframe must be:
- Self-contained HTML + inline CSS.
- Grayscale only (no brand colors, no final styling).
- Showing the **populated** state by default with comments pointing to other states.
- Labeled with the relevant requirement IDs.

Template structure:
```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <title>[Screen Name] — Wireframe</title>
  <style>
    /* grayscale layout-only styles — no brand colors */
    body { font-family: sans-serif; color: #222; background: #fff; max-width: 900px; margin: 2rem auto; }
    .state-note { border: 1px dashed #999; padding: .5rem; color: #666; font-size: .85rem; }
  </style>
</head>
<body>
  <!-- REQ-XXX-NNN, AC-XXX-NNN -->
  <h1>[Screen heading]</h1>
  <div class="state-note">Showing: populated state. Other states: loading, empty, error, forbidden.</div>
  <!-- ... wireframe content ... -->
</body>
</html>
```

### Step 5 — Accessibility requirements

Add an accessibility section to `user-flows.md`:
- Keyboard navigation path for each flow.
- ARIA roles required for non-semantic elements.
- Minimum color contrast ratio target (WCAG 2.1 AA).
- Screen-reader announcements for dynamic content.

## Stop conditions

- Do not invent production components that don't exist — record design-system gaps.
- Do not mark the UX gate complete if screens are missing required states.

## Outputs

- `specs/<NNN>/ux/user-flows.md`
- `specs/<NNN>/ux/component-map.yaml`
- `specs/<NNN>/ux/wireframes/*.html`
- `docs/decisions/adr-002-frontend.md` (if frontend was undecided)

## Handoff

Next step: `/speckit-tasks` → `/sdlc-implement-slice`
