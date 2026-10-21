# Vertical-Slice Architecture — Rules

## Overview

Vertical slices and Clean Architecture work **together**. Each slice represents one
user-visible capability delivered end-to-end. Within Clean Architecture the files
for that slice are **distributed across projects** according to their layer
responsibility — they are not all in one folder.

```
One vertical slice (e.g. "Create Account") spans:

  <Service>.Api/Controllers/          ← HTTP endpoint for this slice
  <Service>.Application/UseCases/     ← Command + Handler + Validator for this slice
  <Service>.Application/Contracts/    ← Request + Response DTOs for this slice
  <Service>.Domain/Entities/          ← Domain entity used by this slice (shared across slices)
  <Service>.Infrastructure/           ← Repository/store implementation (shared, slice-agnostic)
  <Service>.Tests/Unit/Application/   ← Unit tests for this slice's handler
  <Service>.Tests/Integration/        ← Integration tests for this slice's endpoint
```

Implement one slice at a time — do not batch all handlers, then all endpoints.

---

## Use case folder naming (Application layer)

```
<Service>.Application/UseCases/<UseCaseName>/
```

Examples: `UseCases/CreateAccount/`, `UseCases/UpdateAccount/`, `UseCases/GetAccount/`.

## Allowed files per slice (Application layer)

```
UseCases/<UseCaseName>/
├── <UseCase>Command.cs     # OR <UseCase>Query.cs — the input record
├── <UseCase>Handler.cs     # Business / application logic
└── <UseCase>Validator.cs   # Input validation (if complex)
```

Request and response DTOs live alongside their use case but in the `Contracts/` folder:

```
<Service>.Application/Contracts/
├── Requests/<UseCase>Request.cs
└── Responses/<UseCase>Response.cs
```

## Endpoint naming (Api layer)

```
<Service>.Api/Controllers/<Resource>Controller.cs
```

Each controller action is the HTTP adapter for one slice. It is thin: map route,
read request, call handler, return HTTP result. No business logic.

---

## Slice boundaries

- Each slice owns its own handler and request/response types.
- Slices within the same domain **share** Domain entities and value objects but
  must **not** call each other's handlers directly.
- Cross-feature dependencies must be mediated by a domain event, integration event,
  or well-defined Application interface — never a direct handler-to-handler call.

## Forbidden dependency directions

- `Application` must not reference `Infrastructure` or `Api`.
- `Domain` must not reference any other layer.
- An endpoint (Api layer) must not contain business logic — delegate to a handler.
- Handlers must not depend on `HttpContext`. They receive a command/query record and return a result.

## Shared code justification

Before adding anything to a shared folder (`Common/`, `Shared/`, etc.), answer:

1. Is this genuinely reused by ≥ 3 slices?
2. Does extracting it obscure business intent?
3. Is an existing framework feature (extension method, middleware, policy) sufficient instead?

If the answer to any of these is unfavourable, keep the code local to the slice.

## Endpoints

- Endpoint / controller actions are thin: map route, validate, dispatch, return HTTP result.
- No business logic inside endpoints.
- Use `IResult` return types (Minimal API) or `IActionResult` (MVC controller) — consistent with existing project style.

## Handlers

- Handlers contain application / business logic.
- Handlers do not depend on `HttpContext`. They receive a command/query and return a result.
- Validate preconditions, enforce business rules, delegate to domain objects where appropriate.

## Avoiding unnecessary abstractions

- Do not add a generic `IRepository<T>` unless the project already uses one.
- Do not add a generic `IService<T>` or catch-all mediator pattern unless the project already uses one (e.g., MediatR).
- Use small, use-case-focused port interfaces in `Application/Abstractions/` (e.g. `IAccountStore`) rather than fat generic repositories.
