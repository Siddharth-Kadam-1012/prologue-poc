# Proposed Target Microservice Solution Folder Structure

## Overview

The solution follows Clean Architecture principles with clear separation between API, Application, Domain, and Infrastructure layers. Business rules are isolated in the Domain layer, application workflows are defined in the Application layer, technical concerns reside in Infrastructure, and the API layer is responsible only for hosting and transport concerns.

---------------------------------------------------------------------

## Principles
- Clean Architecture
- Layered + Hexagonal Principles
- Dependency Rules : 
    Dependencies flow inward
        API -> Application -> Domain
        Infrastructure dependes on Application + Domain

---------------------------------------------------------------------

# Target Folder Structure

```text
src/
├── Services/
│   └── <ServiceName>/             ← one folder per microservice
│       ├── <ServiceName>.Api/
│       ├── <ServiceName>.Application/
│       ├── <ServiceName>.Domain/
│       ├── <ServiceName>.Infrastructure/
│       ├── <ServiceName>.Contracts/
│       ├── <ServiceName>.Tests/
│       └── <ServiceName>.slnx
│
└── <FrontendApp>/                 ← frontend projects live directly under src/
    └── (Angular / React / etc.)

# Per-service detail
<ServiceName>/
├── <ServiceName>.Api/
    │   ├── BackgroundServices/
    │   │   └── Consumers/
    │   │       ├── <EventName>Consumer.cs
    │   │       └── ...
    │   ├── Controllers/
    │   │   └── <Resource>Controller.cs
    │   ├── Middlewares/
    │   │   └── ExceptionHandlingMiddleware.cs
    │   ├── Filters/
    │   │   └── ValidationFilter.cs
    │   ├── Extensions/
    │   │   └── WebApplicationExtensions.cs
    │   ├── Properties/
    │   │   └── launchSettings.json
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   ├── Program.cs
    │   └── <ServiceName>.Api.csproj
    │
    ├── <ServiceName>.Application/
    │   ├── Abstractions/
    │   │   ├── Messaging/
    │   │   │   └── IEventPublisher.cs
    │   │   ├── Persistence/
    │   │   │   └── IRepository.cs
    │   │   └── Services/
    │   │       └── I<UseCase>Service.cs
    │   ├── Contracts/
    │   │   ├── Requests/
    │   │   │   └── <UseCase>Request.cs
    │   │   └── Responses/
    │   │       └── <UseCase>Response.cs
    │   ├── Events/
    │   │   ├── Integration/
    │   │   │   └── <IntegrationEvent>.cs
    │   │   └── Internal/
    │   │       └── <DomainEvent>.cs
    │   ├── UseCases/
    │   │   └── <UseCaseName>/
    │   │       ├── <UseCase>Command.cs
    │   │       ├── <UseCase>Handler.cs
    │   │       └── <UseCase>Validator.cs
    │   ├── Services/
    │   │   └── <UseCase>Service.cs
    │   ├── DependencyInjection/
    │   │   └── ApplicationServiceCollectionExtensions.cs
    │   └── <ServiceName>.Application.csproj
    │
    ├── <ServiceName>.Domain/
    │   ├── Entities/
    │   ├── ValueObjects/
    │   ├── Events/
    │   │   └── DomainEvent.cs
    │   ├── Enums/
    │   ├── Exceptions/
    │   ├── Specifications/
    │   └── <ServiceName>.Domain.csproj
    │
    ├── <ServiceName>.Infrastructure/
    │   ├── Persistence/
    │   │   ├── DbContexts/
    │   │   │   └── <ServiceName>DbContext.cs
    │   │   ├── Configurations/
    │   │   │   └── <Entity>Configuration.cs
    │   │   └── Repositories/
    │   │       └── <Repository>Repository.cs
    │   ├── Messaging/
    │   │   └── <Broker>/
    │   │       ├── <Broker>EventPublisher.cs
    │   │       └── <Broker>Options.cs
    │   ├── Identity/
    │   │   └── AuthenticationService.cs
    │   ├── Observability/
    │   │   └── TelemetryExtensions.cs
    │   ├── DependencyInjection/
    │   │   └── InfrastructureServiceCollectionExtensions.cs
    │   └── <ServiceName>.Infrastructure.csproj
    │
    ├── <ServiceName>.Contracts/
    │   └── Events/
    │       └── <IntegrationEvent>.v1.json
    │
    ├── <ServiceName>.Tests/
    │   ├── Unit/
    │   │   ├── Application/
    │   │   ├── Domain/
    │   │   └── Infrastructure/
    │   ├── Integration/
    │   └── <ServiceName>.Tests.csproj
    │
    └── <ServiceName>.Tests.csproj
```

---------------------------------------------------------------------

## Layer By Layer Explanation

### API Layer 

#### Responsibilities
- HTTP Endpoints
- Request / Response Handling
- Hosting Background Consumers 
- API Specific Middlewares & Filters 

#### Contains 
- Controllers 
- Background Message Consumers 
- Middlewares (logging, exception handling)
- Startup and Configuration 

---------------------------------------------------------------------

### Application Layer

#### Responsibilities
- Use Cases
- Application Workflows 
- Orchestration (Sagas)
- Interfaces (Ports)

#### Contains 
- Use Cases (Commands / Queries)
- Contracts (DTOs)
- Application Services
- Integration Events
- Interfaces for Persistence and Messaging


---------------------------------------------------------------------

### Domain Layer 

#### Responsibilities 
- Core Business Rules  
- Domain Invariants 

#### Contains 
- Value Objects
- Domain Events
- Domain Exceptions 

---------------------------------------------------------------------

### Infrastructure Layer 

#### Responsibilities 
- Technical Implementations 

#### Contains 
- EF Core DbContexts 
- Repository Implementations 
- Messaging Infrastructure
- Identity / Auth 
- Telemetry and Logging

---------------------------------------------------------------------

### Contracts Layer 

#### Responsibilities 
- Public Event Contracts
- Versioned Integration Schemas 

---------------------------------------------------------------------

### Tests Layer 

#### Responsibilities 
- Verfication at all levels 

#### Contains 
- Domain Unit Tests 
- Application Unit Tests 
- Infrastructure Integration Tests 

---------------------------------------------------------------------

