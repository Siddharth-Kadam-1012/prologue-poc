# .NET / ASP.NET Core 10 — Coding Rules

## Target framework
- Verify `<TargetFramework>` in `.csproj` before making any framework assumption.
- Do **not** silently retarget or change `<LangVersion>`. Report any required migration decision.

## Nullable reference types
- `<Nullable>enable</Nullable>` is required. Never suppress CS8600/CS8603 with `!` to silence a warning.
- Annotate all new public APIs with correct nullability.

## Analyzers
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` applies. Fix warnings; never add `<NoWarn>` entries to hide them.
- Run `dotnet build --no-restore -warnaserror` to verify. A build that passes only with warnings suppressed is not passing.

## Dependency injection
- Follow the constructor-injection pattern used by the existing project.
- Register services in each layer's own extension method and call up from the composition root in `<Service>.Api`.
  - `ApplicationServiceCollectionExtensions` in `<Service>.Application`
  - `InfrastructureServiceCollectionExtensions` in `<Service>.Infrastructure`
- Do not use `ServiceLocator` / `IServiceProvider` in business logic.

## Validation and error handling
- Validate at system boundaries (endpoints). Use `FluentValidation` or Data Annotations — whichever is already in the project.
- Return `ProblemDetails` / `HttpValidationProblemDetails` for validation failures and server errors.
- Do not swallow exceptions silently. Log with structured logging (include correlation ID where available).

## Async / cancellation
- All I/O methods must be `async`, accept a `CancellationToken`, and pass it downstream.
- No `.Result`, `.Wait()`, or `Task.Run()` wrappers around async code.

## Logging and observability
- Use `ILogger<T>` via injection. Never use `Console.WriteLine` in production code.
- Use structured log messages (`_logger.LogInformation("Order {OrderId} created", order.Id)`).
- Do not log PII, secrets, or full request bodies.

## Configuration and secrets
- Read configuration via `IOptions<T>` bound from `appsettings.json` / environment variables.
- **Never hard-code secrets.** Use environment variables, user-secrets (`dotnet user-secrets`), or a secrets manager.

---

## Clean Architecture — Project Structure (MANDATORY)

Every new backend service **must** follow the six-project layout defined in
`docs/architecture/Microservices_Target_Solution_Structure.md`. Do not create a
single-project service unless an ADR explicitly authorises it.

### Six-project layout

```
src/Services/<ServiceName>/
├── <ServiceName>.Api/            ← HTTP layer: endpoints, controllers, middleware, startup
├── <ServiceName>.Application/    ← Use cases, ports (interfaces), DTOs, integration events
├── <ServiceName>.Domain/         ← Business rules, entities, value objects, domain events
├── <ServiceName>.Infrastructure/ ← Adapters: DB, messaging, auth, telemetry
├── <ServiceName>.Contracts/      ← Public versioned event schemas (shared across services)
├── <ServiceName>.Tests/          ← Unit + integration tests for all layers
└── <ServiceName>.slnx            ← Solution file for this service
```

The service folder sits under `src/Services/`. For example, `GlAccounts` lives at
`src/Services/GlAccounts/`. Frontend projects (Angular, React) live under `src/` directly,
not inside a service folder.

### Dependency rules (enforced by project references)

```
Api  ──►  Application  ──►  Domain
               ▲
               │
        Infrastructure
               ▲
               │
            Domain
```

- **Domain** has **zero** project references. No framework, no ORM, no ASP.NET.
- **Application** references only **Domain**. It defines interfaces (ports) — never implements them.
- **Infrastructure** references **Application** and **Domain**. It implements the ports.
- **Api** references **Application** (and **Infrastructure** only at the composition root to wire DI).
- **Contracts** has no references to other internal projects — it is a pure schema library.
- **Tests** references all layers it needs to test.

### What lives where

| Layer | Contains |
|---|---|
| `Api` | Controllers / Minimal API endpoints, background consumers, middleware, filters, `Program.cs`, `appsettings.json` |
| `Application` | Use case handlers (`<UseCase>Handler.cs`), commands/queries (`<UseCase>Command.cs`), validators (`<UseCase>Validator.cs`), request/response DTOs, port interfaces (`IRepository`, `IEventPublisher`, `I<UseCase>Service`), integration + internal events |
| `Domain` | Entities, value objects, domain events, enums, domain exceptions, specifications |
| `Infrastructure` | EF Core `DbContext`, repository implementations, messaging publisher/consumer, identity/auth service, telemetry extensions, DI registration |
| `Contracts` | Versioned integration event schemas (e.g. `<IntegrationEvent>.v1.json`) shared between services |
| `Tests` | `Unit/Application/`, `Unit/Domain/`, `Unit/Infrastructure/`, `Integration/` |

### Ports and adapters (hexagonal)

- **Port** = an interface declared in `Application/Abstractions/` (e.g. `IAccountStore`, `IEventPublisher`).
- **Adapter** = a class in `Infrastructure/` that implements the port (e.g. `InMemoryAccountStore`, `SqlAccountRepository`).
- The `Domain` and `Application` layers must never reference a concrete adapter class.

---

## Stub data (current phase)

- The project uses **stub data only**. Do not add a database, ORM, migrations or connection strings.
- Port interfaces live in `<Service>.Application/Abstractions/Persistence/` or `Abstractions/Services/`.
- In-memory stub implementations live in `<Service>.Infrastructure/Persistence/Repositories/` (or a `StubData/` subfolder within Infrastructure while the project is in the PoC phase).
- Register stub implementations in `InfrastructureServiceCollectionExtensions` so they can be swapped for real adapters later without touching Application or Domain.
- Seed data must be **synthetic**: no real names, emails, IDs or other personal data. Use fixed IDs/timestamps so tests are repeatable.
- Seed data must cover every state the UX contract defines (empty, populated, error-triggering, boundary values).
- Stub behaviour must honour the OpenAPI contract exactly (status codes, validation, conflicts, not-found).
- Never claim persistence was verified. Durability, transactions and migrations are **not covered** by stub data; record them as
  "not verified (stub data)" in the validation report.

## Persistence (when a real store is later approved)

- Use the ORM/database already present in the project. Do not introduce a second one without a documented decision.
- Place `DbContext` and `IEntityTypeConfiguration<T>` classes in `<Service>.Infrastructure/Persistence/`.
- Make transaction boundaries explicit. Avoid implicit ambient transactions.
- Do not add unnecessary repository wrappers around every single ORM call.

## Formatting
- Run `dotnet format --verify-no-changes` to check. All code must pass before committing.
- Use the `.editorconfig` settings already in the repository; do not add a conflicting one.

## Build gate commands (adapt to solution layout)
```powershell
dotnet restore
dotnet build --no-restore -warnaserror
dotnet format --verify-no-changes
dotnet test --no-build
```
