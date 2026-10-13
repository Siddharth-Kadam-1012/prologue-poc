# Vertical-Slice Architecture — Rules

## Feature folder naming
```
src/Features/<Domain>/<UseCaseName>/
```
Examples: `src/Features/Orders/CreateOrder/`, `src/Features/Orders/ListOrders/`.

## Allowed files per slice (illustrative — adapt to project conventions)
```
<UseCaseName>/
├── Endpoint.cs       # Thin HTTP adapter — translates HTTP ↔ application operation
├── Command.cs        # OR Query.cs — the request record
├── Handler.cs        # Business / application logic
├── Validator.cs      # Input validation (if complex)
├── Response.cs       # Response shape (if needed)
└── Tests.cs          # Co-located unit / integration tests (or separate test project)
```
This is **illustrative**. Use existing project conventions. Do not force one file per type if the project coheres differently.

## Slice boundaries
- Each slice owns its own HTTP mapping, application logic, and persistence access.
- Slices within the same domain may share **value objects and domain entities** but must not call each other's handlers directly.
- Cross-feature dependencies must be mediated by a well-defined interface, event, or shared contract — never a direct handler-to-handler call.

## Forbidden dependency directions
- A slice **must not** import from another feature's slice folder directly.
- Infrastructure code (database, HTTP clients) must not import from feature folders.
- No "service layer" that aggregates multiple features unless a domain boundary justifies it and an ADR documents the decision.

## Shared code justification
Before adding anything to a shared/common folder, answer:
1. Is this genuinely reused by ≥ 3 slices?
2. Does extracting it obscure business intent?
3. Is an existing framework feature (extension method, middleware, policy) sufficient instead?

If the answer to any of these is unfavourable, keep the code local to the slice.

## Endpoints
- Endpoint classes are thin: map route, validate, dispatch, return HTTP result.
- No business logic inside endpoints.
- Use `IResult` return types (minimal API) or `IActionResult` (MVC controller) — consistent with existing project style.

## Handlers
- Handlers contain application / business logic.
- Handlers do not depend on `HttpContext`. They receive a command/query and return a result.
- Validate preconditions, enforce business rules, delegate to domain objects where appropriate.

## Avoiding unnecessary abstractions
- Do not add a generic `IRepository<T>` unless the project already uses one.
- Do not add a generic `IService<T>` or catch-all mediator pattern unless the project already uses one (e.g., MediatR).
- Prefer direct dependency on the database context or typed repository for the feature's aggregate.
