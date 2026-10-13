# ADR-001: OpenAPI Contract Source-of-Truth Policy

**Status:** Undecided — fill in before implementing the first API feature.

**Date:** (to be set when decided)

---

## Context

Every API endpoint needs an OpenAPI contract. There are two mainstream policies:

1. **Design-time first:** The reviewed OpenAPI YAML committed under `specs/<NNN>/contracts/openapi.yaml` is the authoritative source. The application's generated OpenAPI document must match it. Drift is detected by comparing the generated output against the committed file during CI.

2. **Code-first:** The application code generates the OpenAPI document at build time (e.g., via `Microsoft.AspNetCore.OpenApi` or Swashbuckle). The committed YAML is a snapshot used only for drift detection. The code is the source of truth.

---

## Decision

**[ ] Design-time first** — OpenAPI YAML is designed, reviewed, and committed before implementation begins.
Validation: `dotnet run scripts/sdlc/sdlc.cs -- openapi specs/<NNN>/contracts/openapi.yaml`

**[ ] Code-first** — Application generates OpenAPI; committed snapshot used for drift detection only.
Validation: Generate document at build time and compare with `dotnet run scripts/sdlc/sdlc.cs -- drift`.

---

## Consequences

### Design-time first
- ✅ Contract reviewed before any implementation — reduces drift risk.
- ✅ Clear gate: implementation cannot start until the contract is approved.
- ⚠️  Requires discipline to keep implementation synchronized with the design contract.

### Code-first
- ✅ Contract always reflects current implementation.
- ⚠️  Risk of API changes landing without explicit contract review.
- ⚠️  Must run drift detection on every build to catch unintended changes.

---

## Rationale

_(Fill in when decided)_

**Decided by:** (name)
**Date:** (date)
