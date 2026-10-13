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
- Register services in the feature's own extension method (`AddXyzFeature`) and call it from the composition root.
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

## Persistence
- Use the ORM/database already present in the project. Do not introduce a second one without a documented decision.
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
