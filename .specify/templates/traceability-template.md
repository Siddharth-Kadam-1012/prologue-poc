---
description: "Traceability matrix reference — links requirements to tests and evidence"
---

# Traceability Matrix: [FEATURE NAME]

> `sdlc.cs init-feature` generates this file. This template documents the format.
>
> **Coverage is derived, not claimed.** `sdlc.cs traceability` scans test sources
> (`tests/`, `e2e/`, `*Tests.cs`, `*.spec.ts`, `*.feature`) for AC IDs. An AC is covered only
> if a test references it, e.g. `// AC-ORD-001` or a test named `AC_ORD_001_...` written as `AC-ORD-001`.
> Writing "covered" / ✅ next to an AC that no test references makes the gate fail.

## Traceability map

| REQ-ID | AC-ID | API-ID | UX-ID | TASK-ID | TEST-ID (file) | Status |
|---|---|---|---|---|---|---|
| REQ-ORD-001 | AC-ORD-001 | API-ORD-001 | UX-ORD-001 | T001 | tests/Orders/CreateOrderTests.cs | planned |

(Example row only — use real domain codes. `XXX` placeholder IDs are rejected by the spec gate.)

## Evidence log

Appended automatically by `sdlc.cs evidence`. Do not edit rows by hand.

| Date | Check | Command | Exit code | Output SHA-256 |
|---|---|---|---|---|
