# Autonomous SDLC Constitution
# This is the project constitution for the autonomous SDLC workflow.
# Run /speckit-constitution to have Bob help you fill in project-specific values.

## I. Specification First (NON-NEGOTIABLE)

Every feature begins with a written specification in `specs/`. No implementation
code is written until the specification gate is approved by a human. Critical
ambiguities in security, data integrity, authorization, business rules, and public
API behavior must be resolved before proceeding. Assumptions are documented, not
silently applied.

## II. Contracts Before Implementation (NON-NEGOTIABLE)

An OpenAPI contract must be designed, validated by `scripts/validate-openapi.ps1`,
and approved before any API implementation begins. The contract is a first-class
design artifact, not a documentation afterthought. The source-of-truth policy
(design-time-first or code-first) must be documented in `docs/decisions/adr-001-openapi-policy.md`.

## III. Vertical Slices

Implementation proceeds one user-visible capability at a time — end to end
(UI → endpoint → handler → persistence → tests). Technical-layer batches
(all controllers, then all services) are prohibited.

## IV. Tests as Evidence (NON-NEGOTIABLE)

Test expectations are derived from `specs/<NNN>/spec.md` acceptance criteria,
not solely from implementation behavior. Every acceptance criterion must have
a verification strategy. Untested ACs are reported in `specs/<NNN>/traceability.md`.
Tests may not be weakened or deleted to achieve a green build.

## V. Deterministic Enforcement

Only script and CI output proves a gate passed. An agent must not claim that a
build, test, or check succeeded unless actual tool output confirms it. Status
files must not be edited manually to bypass a gate.

## VI. Human Approval at Risk Boundaries

High-risk actions require explicit human approval before execution:
- Authentication or authorization policy changes
- Destructive or irreversible database migrations
- Breaking public API changes (removed operations, changed required fields)
- Secrets and credentials
- Infrastructure and production operations

## VII. Repository Context is Authoritative

Inspect existing code and configuration before proposing conventions. Do not
silently retarget frameworks, replace the frontend, or change the database
without a documented ADR and human approval.

## VIII. Incremental Adoption

Preserve working project conventions unless a documented ADR authorizes a change.
Do not introduce new frameworks, libraries, or patterns without justification.

## IX. Resumability

Feature state is persisted in `specs/<NNN>/status.yaml` after each stage.
Work can be resumed in a new session by reading the status file. The workflow
should never require the original conversation to resume.

## X. No Secrets, No PII

Secrets and PII must never appear in committed files, generated artifacts, or
agent logs. Use environment variables or a secrets manager. Report any
accidental exposure immediately.

## Governance

This constitution supersedes all other practices when they conflict.
Amendments require:
1. A pull request updating this file and `docs/decisions/` with the rationale.
2. Human approval.
3. A migration plan if existing workflow or tooling is affected.

All agent work must comply with this constitution. Complexity beyond the
minimum necessary to implement the approved spec must be justified.

**Version**: 1.0.0 | **Ratified**: 2026-10-13 | **Last Amended**: 2026-10-13
