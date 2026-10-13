# ADR-002: Frontend Framework

**Status:** Undecided — fill in before implementing any UI feature.

**Date:** (to be set when decided)

---

## Context

No frontend code exists yet. Before implementing any user-visible feature, the team
must choose a frontend approach. The choice affects:

- Component library selection.
- Testing toolchain (Playwright setup, component test framework).
- Build pipeline.
- SDLC agent behavior (the `ux-engineer` mode and `sdlc-ux` skill inspect the actual framework).

---

## Options

| Option | Notes |
|---|---|
| ASP.NET Core Razor Pages / MVC (server-rendered) | Lowest JS complexity; Playwright for browser tests |
| Blazor WebAssembly / Server | .NET-native; component library options: Radzen, MudBlazor, Fluent UI Blazor |
| React + Vite | Most flexible; requires TypeScript toolchain; Playwright + Vitest |
| Next.js | If SSR/SSG is needed with React |
| Vue 3 | Alternative to React; Playwright + Vitest |
| No frontend (API-only) | If this repository is a backend service only |

---

## Decision

**[ ] Option selected:** ______________________

**Component library:** ______________________

**Build tool:** ______________________

**Testing:** ______________________

---

## Consequences

_(Fill in when decided)_

**Decided by:** (name)
**Date:** (date)
