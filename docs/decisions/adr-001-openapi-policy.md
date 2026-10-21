# ADR-001: OpenAPI Contract Source-of-Truth Policy

**Status**: Accepted  
**Date**: 2026-10-16  
**Feature**: GL Account Maintenance and Inquiry (001-gl-account-maintenance)  
**Deciders**: Jaymin Trivedi

---

## Context

The project needs a declared policy for which artifact is authoritative when the committed OpenAPI YAML
and the running application's generated OpenAPI document diverge.

Two options were considered:

| Policy | Description |
|---|---|
| **Design-time first** | The committed `contracts/openapi.yaml` is the authoritative source. The implementation must conform to it. Drift is detected by comparing generated output to the committed file. |
| **Code-first** | The implementation generates the OpenAPI document. The committed file is a snapshot for drift detection only. |

---

## Decision

**Design-time first.**

The committed `specs/<NNN>/contracts/openapi.yaml` file is the authoritative contract.
Implementation must conform to it. Drift detection compares the generated document to this file.

---

## Rationale

- The SDLC workflow requires contracts to be approved before implementation begins (AGENTS.md § II).
- Design-time first enforces this: the contract is a first-class design artifact written before code.
- The stub-data phase has no code-generation tooling set up; design-time first is immediately actionable.
- Drift detection via `scripts/sdlc/sdlc.cs -- drift` can run in CI as an additive check.

---

## Consequences

- Implementers must not add, remove, or rename operations without updating `openapi.yaml` first.
- The contract gate (`gate <spec-dir> contract`) must pass before any implementation slice begins.
- Additive changes (new optional fields, new endpoints) require a PR updating `openapi.yaml` and re-running the gate.
- Breaking changes (removed operations, changed required fields, renamed paths) require the Contract Engineer agent and human approval per AGENTS.md § VI.

---

## Supersedes

Nothing — this is the first contract policy for this repository.
