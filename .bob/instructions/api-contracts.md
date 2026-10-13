# API Contracts — Rules

## Version and format
- Use **OpenAPI 3.1** (verify the project's existing tooling before changing the version).
- ASP.NET Core 10 has built-in OpenAPI support (`Microsoft.AspNetCore.OpenApi`). Verify actual package usage before adding dependencies.
- Store the committed contract at: `specs/<NNN>-<feature-name>/contracts/openapi.yaml`

## Source-of-truth policy
Choose **exactly one** of:

| Policy | Description |
|---|---|
| **Design-time first** | The committed OpenAPI YAML is the authoritative source. The implementation must conform to it. Drift is detected by comparing generated output to the committed file. |
| **Code-first** | The implementation generates the OpenAPI document at build time. The committed file is a snapshot used for drift detection only. |

Document the chosen policy in `docs/decisions/adr-001-openapi-policy.md`.
If the project already has a policy, preserve it.

## Operation requirements
Every operation must specify:
- Stable `operationId` (camelCase, unique across the document).
- HTTP method + path.
- All path, query, and header parameters with types and constraints.
- Request body schema with required fields, types, and validation constraints.
- Response schemas for every documented status code.
- `200`/`201` success response.
- `400` validation error (use `HttpValidationProblemDetails` shape).
- `401` if authentication is required.
- `403` if authorization is checked.
- `404` for resource-not-found.
- `409` for conflicts.
- `422` or `400` for semantic validation — be consistent.
- `500` generic error.
- Security requirement object referencing the auth scheme.
- At least one success example and one error example per operation.

## Naming conventions
- Paths: lowercase, hyphen-separated segments (`/orders/{orderId}/line-items`).
- Schema names: PascalCase (`CreateOrderRequest`, `OrderResponse`).
- Operation IDs: verb + noun, camelCase (`createOrder`, `listOrders`).

## Pagination, filtering, sorting
- Use a consistent envelope: `{ "items": [...], "total": N, "page": N, "pageSize": N }`.
- Document filter and sort query parameters with allowed values.

## Idempotency and concurrency
- Document idempotency key header for operations that must be idempotent.
- Document `ETag` / `If-Match` pattern for optimistic concurrency when applicable.

## Contract validation
The script `scripts/validate-openapi.ps1` must pass before the contract gate is considered closed.
It checks:
- YAML parses without errors.
- All `$ref`s resolve.
- Required response codes and security declarations are present.
- Breaking-change detection against the previous committed contract.

## Contract drift
The script `scripts/check-contract-drift.ps1` compares the committed contract
with the running application's generated OpenAPI. A drift failure blocks the build gate.

## Compatibility
- Breaking changes (removed operations, changed required fields, renamed paths) require the **Contract Engineer** agent and human approval.
- Additive changes (new optional fields, new operations) require targeted review.
