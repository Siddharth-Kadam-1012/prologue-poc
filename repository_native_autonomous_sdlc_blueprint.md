# Repository-Native Autonomous SDLC Blueprint
## SpecKit + IBM BOB + ASP.NET Core 10 + Vertical Slices

**Purpose:** Use this document as the master instruction to an LLM or coding agent working in a software repository. The agent should inspect the existing repository, adapt the blueprint to its actual technologies and conventions, and generate the files and executable tooling needed to implement a reliable, repository-level, SpecKit-driven software development lifecycle (SDLC).

**Primary outcome:** A workflow that starts with a product requirements document (PRD), clarifies and specifies requirements, creates API and UX contracts, implements end-to-end vertical slices, generates and runs tests, verifies quality, and leaves auditable evidence.

---

# 1. Mission and operating principles

Build a **repository-native AI engineering system**, not merely a collection of prompts that generate code.

The repository should version its workflow, engineering rules, architecture knowledge, design-system references, validation scripts, and agent instructions. IBM BOB should operate within that environment and produce changes that are understandable, testable, reviewable, and traceable.

Follow these principles:

1. **Specification first.** Do not implement materially ambiguous requirements. Record questions and assumptions explicitly.
2. **Contracts before implementation.** Define and review API and UX behavior before implementing the relevant slice.
3. **Vertical slices over technical-layer batches.** Complete user-visible capabilities end to end instead of generating all controllers, then all services, then all UI.
4. **Tests are evidence.** Generate tests from requirements and expected behavior, not solely from the implementation.
5. **Deterministic enforcement.** Scripts and CI enforce objective rules; an LLM cannot declare a failing check to be passing.
6. **Human approval at meaningful risk boundaries.** Require review for significant architectural changes, authentication/authorization changes, destructive migrations, breaking public API changes, secrets, infrastructure, and production-impacting operations.
7. **Repository context is authoritative.** Inspect existing code and configuration before proposing or generating new conventions.
8. **Incremental adoption.** Preserve working project conventions unless a documented decision authorizes a change.
9. **Resumability.** Persist stage state and validation evidence so work can continue after an agent session ends.
10. **Least privilege and minimal scope.** Agents should modify only the files required for their assigned task.
11. **No invented success.** Never claim tests, builds, generated files, approvals, or checks succeeded unless actual tool output establishes that they did.
12. **Maintainability over code volume.** Avoid excessive abstractions, duplicated boilerplate, speculative frameworks, and generated code that the team cannot maintain.

## Definition of success

The resulting system should be able to explain:

- What it built and why.
- Which requirements the change satisfies.
- Which API and UX contracts apply.
- Which tests verify each important acceptance criterion.
- Which commands actually ran and their results.
- Which decisions, assumptions, and risks remain.
- What the next safe action is.

---

# 2. First task for the implementing LLM: inspect before generating

Before creating or changing files, inspect the repository and report a concise baseline.

Inspect, where present:

- `.sln`, `.slnx`, `.csproj`, `global.json`, `Directory.Build.props`, `Directory.Build.targets`, `NuGet.config`.
- Target frameworks and installed SDK assumptions.
- Existing ASP.NET Core endpoint/controller patterns.
- Existing vertical-slice, domain, application, persistence, and dependency-injection conventions.
- Frontend framework, package manager, build scripts, routes, components, and design system.
- Existing test frameworks, fixtures, test containers, integration-test infrastructure, and Playwright setup.
- OpenAPI generation, API clients, schema validation, and contract-testing tools.
- Existing IBM BOB configuration, instructions, agents, prompt files, and SpecKit installation/version.
- CI workflows, linting, analyzers, coverage gates, security scanning, and formatting rules.
- Architecture documentation, ADRs, and feature-folder conventions.
- Current working-tree changes. **Never overwrite, revert, or silently reformat unrelated user changes.**

Then produce:

1. A short repository inventory.
2. A list of conventions that should be reused.
3. A list of gaps between the repository and this blueprint.
4. A proposed file-generation/change plan.
5. Any essential questions that cannot safely be inferred.

Do not ask questions that repository inspection can answer. If a decision is not essential to start, record a clearly labeled assumption and keep the workflow configurable.

## Compatibility rule

Use **ASP.NET Core 10 / .NET 10** for a new or explicitly upgraded project, but verify the repository's actual target framework and installed SDK before editing. Do not silently retarget an existing application. If the repository is on another supported framework, preserve it and report the required migration decision.

Do not assume a frontend framework. Detect and preserve the existing one. If none exists, make the choice an explicit configuration decision rather than creating a frontend arbitrarily.

Do not assume a database, ORM, authentication scheme, hosting platform, or component library. Detect existing choices or record an explicit placeholder/decision.

---

# 3. Target workflow architecture

The end-to-end flow is:

1. **PRD analysis**
   - Extract actors, goals, business outcomes, capabilities, business rules, constraints, dependencies, and unknowns.
   - Separate facts, assumptions, and questions.
   - Identify non-functional requirements and measurable acceptance outcomes.

2. **SpecKit specification**
   - Create a feature specification.
   - Clarify ambiguities and resolve critical questions.
   - Define functional requirements, non-functional requirements, edge cases, and acceptance criteria.
   - Give requirements stable identifiers.

3. **Architecture and technical requirements**
   - Inspect existing architecture.
   - Identify bounded capabilities, vertical slices, dependencies, data ownership, authorization, persistence, external integrations, failure behavior, observability, and migration concerns.
   - Record material decisions as ADRs or feature-specific architecture notes.

4. **API contract**
   - Propose OpenAPI operations, request/response schemas, validation behavior, status codes, authorization, pagination/filtering, error formats, examples, and compatibility implications.
   - Validate and review the contract before implementing the corresponding API.
   - Establish a clear source-of-truth policy for contract-first design versus implementation-generated OpenAPI.

5. **UX requirements**
   - Define user journeys, routes, screens, interactions, field validation, loading/empty/error/success/permission states, accessibility, and responsive behavior.
   - Generate grayscale HTML wireframes or equivalent low-fidelity artifacts.
   - Map wireframe elements to actual components in the repository's design system.
   - If an appropriate component does not exist, report a design-system gap instead of silently inventing an inconsistent component.
   - Where useful, create or update Storybook stories (for supported frontend stacks) or an equivalent component gallery.

6. **Task decomposition**
   - Create dependency-ordered tasks.
   - Each task has scope, prerequisites, requirement IDs, expected files/behavior, test expectations, and completion conditions.
   - Prefer small, independently verifiable vertical slices.

7. **Implementation**
   - Implement one slice at a time.
   - Include UI, endpoint, application/business logic, persistence, error behavior, and required tests as applicable.
   - Follow existing conventions and approved contracts.
   - Do not implement unrelated improvements in the same task.

8. **Test asset generation**
   - Generate unit tests, integration tests, OpenAPI/contract tests, UI component tests where appropriate, Playwright tests, accessibility checks, and relevant security tests from acceptance criteria.
   - Reuse existing test fixtures, data builders, authentication helpers, and runtime configuration.

9. **Verification and convergence**
   - Run build, formatting, analyzers, tests, contract validation, security checks, and relevant UI checks.
   - Compare the implementation and tests against the approved requirements.
   - Detect missing coverage, contract drift, UX-state gaps, unimplemented tasks, and out-of-scope changes.
   - Produce a validation report with actual command outputs/statuses.

10. **Pull request and human review**
    - Summarize behavior, requirement IDs, API changes, UX evidence, test results, architectural decisions, known limitations, and risks.
    - Require human approval at configured risk boundaries.
    - Do not merge or deploy unless explicitly authorized and repository policy allows it.

## Parallelism rule

API and UX design may proceed in parallel after requirements are sufficiently clear. They must converge against the same approved feature specification before implementation. Parallel agents must not independently redefine business semantics.

---

# 4. SpecKit integration

Use SpecKit as the foundation for specification-driven development. Inspect the installed SpecKit version, supported commands, templates, and customization mechanism before generating extensions. Preserve standard SpecKit conventions where practical; do not duplicate files or commands that already meet the need.

The baseline lifecycle commonly includes steps equivalent to:

- Specify
- Clarify
- Plan
- Checklist
- Tasks
- Analyze
- Implement
- Converge/review, if supported by the installed version or added as a repository-specific stage

**Important:** Exact command names, availability, and customization hooks depend on the installed SpecKit version. Verify them rather than assuming every command exists. Use the repository's installed conventions and document any added custom commands.

Recommended repository-specific stages to add where they are not already supported:

- PRD analysis
- Technical/API contract design
- UX definition and wireframe generation
- Vertical-slice implementation
- Test asset generation
- Contract drift checking
- Traceability validation
- Convergence/quality report

Each stage must specify:

- Entry conditions.
- Required inputs.
- Outputs and paths.
- Agent responsibility.
- Deterministic validation command(s).
- Exit conditions.
- Whether human approval is required.
- Failure and resume behavior.

A stage must not silently continue if a critical requirement is ambiguous, an approved contract is invalid, or a mandatory validation fails.

---

# 5. Repository structure

Use this as a **proposed structure**, adapting to the actual repository and SpecKit's conventions. Do not create empty or redundant directories just to match the example.

```text
repo/
├── .bob/
│   ├── instructions/
│   │   ├── dotnet.instructions.md
│   │   ├── vertical-slices.instructions.md
│   │   ├── api-contracts.instructions.md
│   │   ├── frontend.instructions.md
│   │   └── testing.instructions.md
│   ├── agents/
│   │   ├── sdlc-orchestrator.agent.md
│   │   ├── prd-analyst.agent.md
│   │   ├── contract-engineer.agent.md
│   │   ├── ux-engineer.agent.md
│   │   ├── slice-implementer.agent.md
│   │   ├── test-engineer.agent.md
│   │   └── quality-reviewer.agent.md
│   └── prompts/
│       ├── analyze-prd.prompt.md
│       ├── generate-contracts.prompt.md
│       ├── generate-wireframes.prompt.md
│       ├── implement-slice.prompt.md
│       ├── generate-tests.prompt.md
│       ├── analyze-change-impact.prompt.md
│       └── converge-feature.prompt.md
├── .specify/
│   ├── memory/
│   └── templates/
├── specs/
│   └── <feature-id>-<feature-name>/
│       ├── spec.md
│       ├── plan.md
│       ├── tasks.md
│       ├── research.md
│       ├── contracts/
│       │   └── openapi.yaml
│       ├── ux/
│       │   ├── user-flows.md
│       │   ├── component-map.yaml
│       │   └── wireframes/
│       ├── test-plan.md
│       ├── traceability.md
│       ├── status.yaml
│       └── validation-report.md
├── src/
├── tests/
├── design-system/              # only if suitable for this repository
├── scripts/
│   ├── validate-specs.*
│   ├── validate-traceability.*
│   ├── validate-openapi.*
│   ├── check-contract-drift.*
│   └── validate-workflow-state.*
├── docs/
│   ├── architecture/
│   └── decisions/
└── .github/workflows/
```

The file extensions for scripts should match the repository's tooling. Prefer PowerShell or cross-platform .NET tooling for a .NET-centric repository where appropriate; avoid assuming Bash is available on all developer machines.

Do not put generated feature artifacts into the repository-wide instructions directory. Shared instructions belong there; feature-specific artifacts belong under the feature's specification directory.

---

# 6. IBM BOB instructions and agent responsibilities

Use IBM BOB's repository-level instructions, scoped instruction files, custom agents, and prompt files where supported by the installed environment. Treat `.bob/` as a proposed organization only; verify the current file naming, directory layout, and metadata requirements against the actual tooling.

## 6.1 Shared instructions

The root IBM BOB instruction file should be concise and contain:

- The repository's architectural principles.
- The required workflow entry point.
- How to discover and obey scoped instructions.
- The requirement to inspect existing code before changing it.
- The vertical-slice rule.
- Contract-first expectations.
- Test and validation expectations.
- Scope-control and safe-editing rules.
- Human approval boundaries.
- How to report assumptions, failures, and evidence.

Do not put every implementation detail in the root file. Move specialized guidance into scoped instruction files.

## 6.2 Scoped instruction files

Generate relevant instructions for:

### .NET
- Target framework and SDK policy.
- Nullable reference types and analyzers.
- Dependency injection conventions.
- Validation and error handling.
- Logging, observability, configuration, and secret handling.
- Async/cancellation-token practices.
- Persistence and transaction conventions.
- Formatting and test conventions.

### Vertical slices
- Feature naming and folder structure.
- Slice boundaries and allowed dependencies.
- Rules for endpoints, commands/queries, handlers, validators, and domain logic.
- How shared code is justified.
- Forbidden dependency directions and cross-feature access rules.
- Avoiding unnecessary abstractions and generic service layers.

### API contracts
- OpenAPI version and style.
- Naming, operation IDs, schema reuse, status codes, and standard errors.
- Auth/security declaration rules.
- Pagination, sorting, filtering, idempotency, and concurrency where relevant.
- Contract compatibility and drift policy.
- Examples and contract-test requirements.

### Frontend
- Detected framework and build commands.
- Actual component library and design tokens.
- Routing, forms, state management, accessibility, and error states.
- Component mapping and story/gallery conventions.
- Prohibition on introducing a second design system without approval.

### Testing
- Test project layout and frameworks.
- Unit versus integration versus contract versus browser test boundaries.
- Fixture and test-data reuse.
- Deterministic tests, isolation, cleanup, and parallelism.
- Playwright locator and waiting conventions.
- Coverage reporting and minimum evidence.
- No arbitrary sleeps or test-order dependencies.

## 6.3 Agents

Create the following specialized agents. They should use the exact agent-file format supported by the installed IBM BOB version.

### SDLC Orchestrator
**Purpose:** Coordinate stage order, dependencies, approvals, and state.

Responsibilities:
- Read feature status and validation evidence.
- Identify the next eligible stage.
- Invoke or direct the appropriate specialized work.
- Ensure prerequisites and approval gates are met.
- Never fabricate validation results.
- Keep changes scoped and resumable.
- Produce a final feature report.

Restrictions:
- Do not override product or architectural decisions without approval.
- Do not bypass failed gates.
- Do not claim to have executed a tool or command that was not executed.

### PRD Analyst
**Purpose:** Turn raw product intent into a structured, reviewable requirement set.

Outputs:
- PRD analysis.
- Actors/personas and business goals.
- Functional and non-functional requirements.
- Business rules, assumptions, dependencies, unknowns, and risks.
- Acceptance criteria and clarification questions.

Restrictions:
- Do not silently invent business policy.
- Mark assumptions and unresolved decisions explicitly.
- Do not write implementation code.

### Contract Engineer
**Purpose:** Define and validate API contracts.

Outputs:
- Proposed OpenAPI contract.
- Request/response schemas and examples.
- Error/status-code semantics.
- Security/authentication requirements.
- Compatibility analysis and contract-test plan.

Restrictions:
- Do not implement behavior to conceal contract ambiguity.
- Do not silently change an approved contract.
- Record unresolved decisions and stop at required gates.

### UX Engineer
**Purpose:** Specify user journeys and generate UX artifacts tied to the real design system.

Outputs:
- User flows.
- Screen and interaction specifications.
- Grayscale HTML wireframes or equivalent low-fidelity artifacts.
- Component map referencing real components.
- Loading/empty/success/error/validation/permission states.
- Accessibility and responsive requirements.
- Stories/component gallery updates where appropriate.

Restrictions:
- Do not invent production components when a repository component exists.
- Report design-system gaps.
- Do not use illustrative wireframes as a substitute for functional production UI.

### Slice Implementer
**Purpose:** Implement a single bounded vertical slice at a time.

Outputs:
- UI, endpoint, business/application logic, persistence, and relevant tests.
- Any required documentation or contract updates approved through the workflow.

Restrictions:
- Do not modify unrelated features.
- Do not broaden task scope without recording the reason and obtaining approval when needed.
- Do not weaken tests, remove requirements, or change gates to force success.
- Follow the approved contracts and existing conventions.

### Test Engineer
**Purpose:** Generate tests from requirements and expected behavior.

Outputs:
- Test matrix.
- Unit, integration, contract, component, Playwright, accessibility, and security tests as applicable.
- Coverage and evidence mapping.
- Test-data setup and cleanup.

Restrictions:
- Do not derive all expectations from implementation behavior.
- Do not make tests pass by weakening assertions.
- Distinguish a product defect from a test setup issue or an outdated specification.

### Quality Reviewer
**Purpose:** Independently review requirements coverage, architecture, contracts, security, tests, and scope.

Outputs:
- Findings prioritized by severity.
- Missing or weak evidence.
- Contract drift and architecture violations.
- Security and authorization concerns.
- Recommendation: pass, conditional pass, or fail.

Restrictions:
- A review agent is advisory; deterministic CI and required human approvals remain authoritative.
- Do not dismiss failures without evidence.
- Do not edit the requirements or tests solely to make a review pass.

## 6.4 Prompt files

Generate reusable prompts for:

1. PRD analysis.
2. Clarification and gap analysis.
3. Technical planning and architecture decisions.
4. OpenAPI generation and validation.
5. UX flow and grayscale wireframe generation.
6. Component mapping against the repository's design system.
7. Vertical-slice task generation.
8. Single-slice implementation.
9. Test generation from acceptance criteria.
10. Change-impact analysis.
11. Convergence and quality reporting.

Every prompt must identify its inputs, allowed modifications, expected outputs, validations, stop conditions, and handoff.

---

# 7. PRD analysis and specification quality

The PRD analysis must distinguish:

- Explicitly stated requirements.
- Reasonable inferences.
- Assumptions requiring approval.
- Unknowns requiring clarification.
- Out-of-scope behavior.
- Constraints and dependencies.
- Non-functional requirements.
- Failure, recovery, and permission scenarios.

Use stable identifiers such as:

- `PRD-ORD-001`
- `REQ-ORD-001`
- `AC-ORD-001`
- `API-ORD-001`
- `UX-ORD-001`
- `TASK-ORD-001`
- `TEST-ORD-001`
- `E2E-ORD-001`

The exact naming scheme is configurable. IDs must remain stable across edits and must not be reused for unrelated requirements.

Each requirement should be testable, scoped, and traceable. Acceptance criteria should describe observable behavior and include negative/error paths where relevant.

Use Given/When/Then scenarios where they improve clarity. Do not force every requirement into Gherkin if a simpler format is more readable.

The clarification gate should stop the workflow when an unresolved question materially affects security, data integrity, authorization, business rules, public API behavior, or major architecture. Lower-risk assumptions may be documented and carried forward if the workflow explicitly permits them.

---

# 8. ASP.NET Core 10 and vertical-slice implementation standards

Apply these rules to new work unless repository conventions or approved ADRs specify otherwise.

- Prefer a modular monolith organized around business capabilities for a new application unless operational or organizational requirements justify microservices.
- Organize code by feature/use case rather than only by technical layer.
- Keep HTTP endpoints thin: translate HTTP input into application operations and translate results to HTTP responses.
- Put business invariants in the appropriate domain/application boundary, not in UI code or incidental endpoint plumbing.
- Validate at system boundaries and test valid, invalid, and unauthorized requests.
- Use consistent `ProblemDetails`/`HttpValidationProblemDetails` behavior where appropriate.
- Use dependency injection and configuration conventions already established by the application.
- Handle cancellation and asynchronous operations correctly.
- Make transaction boundaries, concurrency behavior, and persistence semantics explicit.
- Use structured logging and relevant observability conventions.
- Avoid leaking secrets, personal data, or sensitive payloads in logs and generated artifacts.
- Avoid premature abstractions, unnecessary repositories around every ORM operation, and generic infrastructure that obscures business behavior.
- Use the repository's actual ORM/database; do not assume EF Core unless detected or approved.

Illustrative feature structure:

```text
src/Features/Orders/
├── ListOrders/
│   ├── Endpoint.cs
│   ├── Query.cs
│   ├── Handler.cs
│   ├── Response.cs
│   └── Tests.cs
├── CreateOrder/
│   ├── Endpoint.cs
│   ├── Command.cs
│   ├── Handler.cs
│   ├── Validator.cs
│   └── Tests.cs
└── EditOrder/
    ├── Endpoint.cs
    ├── Command.cs
    └── Handler.cs
```

This is illustrative, not a requirement for one file per type. Prefer cohesive code and existing repository conventions.

For each slice, define:
- User-visible capability.
- Preconditions and authorization.
- API operations and schemas.
- Business rules.
- Persistence behavior.
- Error and recovery behavior.
- UI states.
- Tests and acceptance evidence.
- Dependencies on other slices.

---

# 9. OpenAPI contract-first policy

Make OpenAPI a versioned design artifact, not merely documentation generated after implementation.

## 9.1 Contract design requirements

For each operation, specify as applicable:

- Stable operation ID.
- HTTP method and path.
- Parameters and request schemas.
- Required/optional fields and constraints.
- Response schemas and success status.
- Validation, not-found, conflict, authentication, authorization, and server-error behavior.
- Standardized error payload.
- Authentication/security requirements.
- Pagination, filtering, and sorting semantics.
- Idempotency and concurrency semantics.
- Examples for success and failure.
- Compatibility implications.

Use the OpenAPI version supported by the repository's tooling. ASP.NET Core 10 supports built-in OpenAPI document generation; verify available build-time generation options and output in the actual SDK/project.

## 9.2 Choose one authoritative contract policy

Prefer a documented policy such as:

**Design-time source:** the reviewed OpenAPI contract committed with the feature.

**Implementation verification:** generate the application's OpenAPI output and compare it with the approved design contract using a deterministic tool.

Do not maintain two independent definitions as authoritative. If the existing repository uses code-first OpenAPI generation, preserve that model unless an explicit decision approves moving to contract-first. In either case, detect unintended drift.

## 9.3 Contract validation

Add scripts or CI jobs to:

- Parse and structurally validate OpenAPI.
- Check references and schema validity.
- Enforce naming and operation-ID rules.
- Check required error responses and security declarations.
- Compare current and proposed contracts for breaking changes.
- Validate actual endpoint responses against schemas where practical.
- Run integration tests against the real application.
- Report changes requiring approval.

Do not assume a schema-valid document proves the business logic or authorization behavior is correct.

---

# 10. UX requirements, wireframes, and design-system mapping

## 10.1 UX deliverables

For every applicable feature, define:

- Personas/actors and user journeys.
- Routes/screens.
- Main action and navigation.
- Fields and validation.
- Loading, empty, populated, success, error, permission-denied, and conflict states where relevant.
- Keyboard and accessibility behavior.
- Responsive behavior.
- API operation and data schema used by each interaction.
- Component map linking each visual element to an actual repository component.

## 10.2 Grayscale wireframes

Generate low-fidelity grayscale HTML wireframes or an equivalent format that can be previewed locally. The wireframes should emphasize layout, hierarchy, behavior, and state—not final branding or visual polish.

Wireframes must be derived from the actual specification. Generic stock screens are not substitutes for feature-specific UX artifacts.

For each screen, include representative states such as:
- Loading.
- Empty.
- Populated.
- Validation failure.
- Server failure.
- Forbidden/unauthorized as appropriate.
- Conflict or concurrency failure when applicable.

## 10.3 Real design-system components

The production implementation must use the actual components, tokens, and interaction patterns in the repository.

If the frontend is React, use the existing React component library and Storybook if present or approved. If it is Blazor, use the repository's Razor components and an appropriate gallery/demo approach. For Razor Pages/MVC, follow the existing server-rendered UI conventions.

Do not introduce Storybook, a new component library, or another design system without a justified decision.

Example component map:

```yaml
screen: order-list
route: /orders
components:
  - id: search-orders
    component: TextField
    purpose: Search orders by reference
  - id: orders-table
    component: DataTable
    purpose: Display filtered orders
  - id: create-order
    component: PrimaryButton
    action: navigate-to-create
states:
  - loading
  - empty
  - populated
  - error
  - forbidden
```

The schema is illustrative. Define and validate the actual schema used by the repository. If a component is missing, record a design-system gap and obtain approval for the component's design/implementation.

---

# 11. Testing strategy

Generate the initial test plan from requirements and acceptance criteria, then implement and refine tests alongside the code. Do not wait until all code is finished to decide what should be tested.

Use the repository's existing testing frameworks where viable. If none exist, propose a minimal standard set and explain the decision before adding dependencies.

## 11.1 Test types

| Test type | Purpose | Typical tools |
|---|---|---|
| Unit | Business rules, validation, edge cases | xUnit or NUnit, based on repository |
| Integration | HTTP pipeline, DI, persistence, auth, transactions | `WebApplicationFactory` and chosen database strategy |
| Contract | OpenAPI structure and actual API conformance | OpenAPI validator and HTTP test harness |
| Component/UI | Component behavior and UX states | Framework-specific tooling |
| Browser E2E | High-value end-to-end user journeys | Playwright |
| Accessibility | Automated accessibility rules and interaction checks | Suitable accessibility tooling plus manual review |
| Security | Authorization matrix, input boundaries, relevant threat cases | Integration tests and security scanners |

ASP.NET Core supports integration testing with `WebApplicationFactory`. Playwright is appropriate for browser-level testing of web applications. Verify versions and compatibility before installing packages.

## 11.2 Test-generation rules

- Derive expectations from approved requirements, not merely from implementation.
- Reuse repository fixtures, builders, authentication helpers, and test-data setup.
- Cover valid, invalid, unauthorized, forbidden, missing-resource, conflict, and failure scenarios where applicable.
- Assert observable outcomes, including persistence and side effects when relevant.
- Keep unit, integration, contract, and browser tests focused on distinct responsibilities.
- Do not duplicate the same assertion at every layer without a reason.
- Do not weaken assertions or delete tests to force a green run.
- Do not make tests depend on execution order.
- Avoid arbitrary sleeps; use proper waits and stable Playwright locators.
- Ensure test data is isolated and cleaned up.
- Record skipped tests and why they were skipped.
- Report untested acceptance criteria explicitly.
- Use coverage thresholds as one signal, not proof of test quality.
- Consider mutation testing or targeted fault injection for critical business rules.

## 11.3 Playwright requirements

Generate browser tests from user journeys, acceptance criteria, UX state definitions, and component maps.

Each test plan should state:
- Preconditions and authentication.
- Test data and cleanup.
- Navigation.
- Accessible locators based on roles and labels where possible.
- Expected visible and persisted outcomes.
- Error/validation behavior.
- Relevant API expectations.
- Diagnostics such as traces, screenshots, and logs on failure.

Reuse existing Playwright configuration and fixtures. Do not introduce a second test runner or browser setup without approval.

Example only; adapt selectors and outcome to the real UI:

```typescript
import { test, expect } from "@playwright/test";

test("user can create an order", async ({ page }) => {
  await page.goto("/orders/new");

  await page.getByLabel("Customer").selectOption({
    label: "Contoso"
  });

  await page.getByLabel("Order reference").fill("ORD-1001");

  await page.getByRole("button", {
    name: "Create order"
  }).click();

  await expect(
    page.getByRole("heading", { name: "Order created" })
  ).toBeVisible();

  await expect(page).toHaveURL(/\/orders\/[^/]+$/);
});
```

## 11.4 Example acceptance scenarios

```gherkin
Feature: Create an order

Scenario: Create an order with valid details
  Given the user has permission to create orders
  When the user submits valid order details
  Then the API returns the documented success status
  And the response contains the created order identifier
  And the order is persisted
  And the UI displays the created order

Scenario: Reject an invalid order
  Given the user has permission to create orders
  When the user submits an order without a required customer
  Then the API returns the documented validation error
  And no order is persisted
  And the UI displays a useful validation message
```

Use these as examples only; actual requirements must come from the PRD and clarification decisions.

---

# 12. Traceability and evidence

This is a core system capability, not an optional documentation task.

Connect requirements to their acceptance criteria, API operations, UX screens, tasks, code areas where practical, tests, and validation results.

Example:

```text
PRD-ORD-001
  └── REQ-ORD-001
       ├── AC-ORD-001
       ├── API-ORD-001
       ├── UX-ORD-001
       ├── TASK-ORD-001
       ├── TEST-ORD-001
       └── E2E-ORD-001
```

Generate a traceability report that detects:
- Requirements without acceptance criteria.
- Acceptance criteria without a verification strategy.
- API operations without a documented security decision.
- UX states without implementation/test coverage.
- Tasks that are incomplete or unlinked.
- Tests that refer to removed or changed requirements.
- Contract changes without compatibility analysis.
- Critical requirements with missing evidence.

Do not force every source file to contain a requirement ID. Trace at meaningful behavior boundaries.

## Evidence integrity

Validation status must be based on actual script/test output wherever possible. An agent must not mark a test as passing by editing a status file.

Store, as practical:
- Command and arguments.
- Execution time.
- Exit code.
- Summary of output.
- Relevant artifact/report path.
- Commit or working-tree reference if available.

Avoid recording secrets, credentials, sensitive user data, or full sensitive logs.

---

# 13. Change-impact analysis

Support changes after initial implementation.

When a requirement, business rule, API contract, UX flow, or shared component changes, identify affected artifacts before editing:

- Specification and acceptance criteria.
- Architecture decisions.
- OpenAPI schemas and operations.
- UI states, components, and routes.
- Domain/application logic.
- Persistence and migrations.
- Unit, integration, contract, and browser tests.
- Documentation and traceability.

Produce a minimal, dependency-ordered change plan. Reuse stable IDs to identify impact. Do not regenerate an entire feature if a bounded change is sufficient.

Require approval before breaking API changes, risky migrations, security-policy changes, or other high-impact modifications.

---

# 14. Resumable workflow state

Persist feature state in a validated format, such as YAML or JSON. Adapt to the repository's conventions and available tooling.

Illustrative YAML:

```yaml
feature: order-management
stage: implementation

stages:
  specification:
    status: approved
  api_contract:
    status: approved
  ux:
    status: approved
  implementation:
    status: in_progress
  verification:
    status: pending

validation:
  build: passed
  unit_tests: passed
  integration_tests: pending
  contract_tests: pending
  playwright: pending

next_action: implement-create-order-slice
```

Define an explicit schema and allowed status transitions. Suggested statuses include `pending`, `in_progress`, `blocked`, `failed`, `passed`, `approved`, and `skipped` where appropriate.

Distinguish **validation status** from **human approval status**. For example, successful tests do not automatically mean an architecture decision is approved.

A resumable run summary should include:
- Feature and current stage.
- Completed stages and their artifacts.
- Files changed.
- Decisions and assumptions.
- Commands executed and actual results.
- Outstanding failures and risks.
- Required approvals.
- Next safe action.

Use native SpecKit resume/review capabilities when they meet the need. Avoid building a separate orchestration engine that duplicates supported features.

---

# 15. Deterministic quality gates

Build enforcement into scripts and CI. A language model's review is not a substitute for executable checks.

## 15.1 Specification gate

- Critical ambiguities resolved.
- Requirements have stable IDs.
- Acceptance criteria are testable.
- Scope and out-of-scope items are explicit.
- Relevant non-functional requirements are identified.

## 15.2 Contract gate

- OpenAPI parses and validates.
- Required schemas, responses, and security definitions are present.
- Examples are valid where tooling supports this.
- Breaking changes are detected and approved.
- Contract drift is checked.

## 15.3 Architecture gate

- Forbidden dependency directions are checked where feasible.
- Unapproved project/dependency changes are detected.
- Changes respect feature boundaries.
- Shared abstractions are justified.
- Material architectural decisions are documented.

## 15.4 Build gate

Adapt commands to the solution and installed SDK:

```bash
dotnet restore
dotnet build --no-restore -warnaserror
dotnet format --verify-no-changes
dotnet test --no-build
```

These are baseline examples, not a guarantee that every repository supports identical commands or shell syntax. Inspect and adapt. Do not hide existing failures by adding broad warning suppressions.

## 15.5 Test gate

- Required unit and integration tests pass.
- Contract checks pass.
- Browser tests run at the appropriate stage.
- Test skips and failures are reported.
- Required acceptance criteria have evidence.

## 15.6 Security gate

- Secret detection.
- Dependency and vulnerability scanning.
- Relevant authorization tests.
- Input-boundary and data-exposure checks.
- Configuration and logging checks.
- Additional threat checks based on the application.

## 15.7 Review and merge gate

- Change remains within scope.
- Tests and contract reports are attached or linked.
- High-risk decisions have human approval.
- CI policy passes.
- Merge/deploy actions occur only when explicitly authorized and permitted.

Create scripts that fail with useful error messages and nonzero exit codes. Use tools already present where possible; avoid installing multiple tools that perform the same job without a reason.

---

# 16. Risk-adaptive autonomy and safe editing

Use risk tiers.

### Low risk
Examples: well-specified local UI behavior, isolated unit tests, small internal refactors with no public behavior change.

Allowed: autonomous implementation and deterministic validation within task scope.

### Medium risk
Examples: changes to persistence behavior, shared components, external integration handling, or non-breaking public API additions.

Allowed: implementation with targeted review, impact analysis, and required test evidence.

### High risk
Examples: authentication/authorization policy, sensitive data handling, destructive or irreversible migrations, public API breaking changes, infrastructure/production changes, secrets, and deployment.

Required: explicit human approval before the risky action. Do not let an agent bypass this gate because its own confidence is high.

Always:
- Inspect `git status` and preserve unrelated user changes.
- Avoid broad destructive commands.
- Avoid modifying generated/lock files manually when tooling should generate them.
- Never expose credentials or commit secrets.
- Do not disable CI/security rules to force a passing result.
- Do not merge, deploy, or run destructive operations without explicit authorization.
- Record any out-of-scope change and ask before expanding scope.

---

# 17. Workflow telemetry and improvement

Measure the quality of the workflow, not the amount of generated code.

Suggested metrics:
- First-pass build success.
- First-pass test success.
- Requirements with mapped tests.
- Contract drift incidents.
- Escaped defects.
- Flaky browser tests.
- Agent retries and failure categories.
- Review rework.
- Time spent blocked on clarification or approval.
- Human review findings and severity.
- Percentage of changes requiring scope correction.

Use metrics to improve prompts, templates, deterministic validation, and architecture guidance. Do not optimize for raw test count or code volume.

For critical business rules, consider mutation testing or targeted fault injection to evaluate whether tests detect meaningful behavioral changes.

---

# 18. Recommended rollout

Build a reliable vertical path first, then expand.

## Phase 1 — Repository foundation
- Inspect and document current conventions.
- Establish or refine the repository constitution.
- Configure SpecKit for the actual IBM BOB environment.
- Add shared/scoped instructions.
- Add agent definitions and prompt templates.
- Standardize build/test commands and basic CI gates.

## Phase 2 — PRD to specification and API contract
- Implement PRD analysis and clarification templates.
- Define requirement and acceptance-criterion IDs.
- Generate OpenAPI contracts.
- Add validation, compatibility checks, and review gates.

## Phase 3 — First vertical slice
- Choose a small, realistic capability.
- Implement UI, API, business logic, persistence, unit tests, and integration tests.
- Verify the approved contract.
- Measure whether the generated code is maintainable and reviewable.

## Phase 4 — UX and browser testing
- Generate feature-specific grayscale wireframes.
- Map wireframes to real design-system components.
- Generate Playwright scenarios from acceptance criteria.
- Add CI execution, diagnostics, and traceability.

## Phase 5 — Orchestration and convergence
- Connect stages with explicit prerequisites and approvals.
- Persist validated workflow state.
- Add change-impact analysis.
- Add convergence and evidence reporting.
- Add risk-adaptive autonomy.

Pilot with a small feature such as listing and creating orders. Avoid using a complex distributed workflow, major authentication redesign, or multi-service migration as the first experiment.

Measure:
- Acceptance criteria with verification strategies.
- Build/test success.
- Contract consistency.
- Scope control.
- Resume behavior.
- Review effort and defect rate.
- Developer confidence in generated changes.

---

# 19. Concrete acceptance criteria for the generated workflow itself

The implementing LLM must create a workflow that satisfies these requirements.

## Repository compatibility
- [ ] It inspected the actual repository and recorded the detected stack.
- [ ] It preserved existing conventions where reasonable.
- [ ] It did not silently retarget frameworks or replace the frontend/database.
- [ ] It preserved unrelated working-tree changes.
- [ ] It verified SpecKit/IBM BOB file conventions for the installed versions.

## Workflow and prompts
- [ ] Shared instructions are concise and specialized rules are scoped.
- [ ] Agents have explicit inputs, outputs, restrictions, validation, and handoff rules.
- [ ] Each stage has entry and exit conditions.
- [ ] Critical ambiguity and high-risk changes require approval.
- [ ] A failed or interrupted workflow can be resumed.
- [ ] Agents cannot claim checks passed without evidence.

## Specifications and contracts
- [ ] Requirements have stable IDs.
- [ ] Acceptance criteria are observable and testable.
- [ ] OpenAPI contract validation is automated.
- [ ] Contract source-of-truth policy is documented.
- [ ] Breaking API changes and unintended drift are detected.
- [ ] UX wireframes and component maps are tied to feature requirements.
- [ ] Existing design-system components are reused.

## Implementation and tests
- [ ] Vertical-slice implementation is the default.
- [ ] Test plans originate from requirements, not only from implementation.
- [ ] Unit and integration test conventions are documented.
- [ ] Contract-test strategy is implemented or clearly staged.
- [ ] Playwright strategy matches the detected frontend.
- [ ] Test data and browser tests are isolated and maintainable.
- [ ] Missing requirement coverage is reported.

## Quality and safety
- [ ] Build, formatting, tests, contract checks, and security checks are wired into CI as feasible.
- [ ] Quality gates fail deterministically.
- [ ] Human approvals cannot be bypassed by editing workflow state.
- [ ] Traceability and validation reports are generated.
- [ ] No secrets are written into files or logs.
- [ ] No unrelated application implementation is generated as part of merely establishing the workflow unless explicitly requested.

---

# 20. Required deliverables from the implementing LLM

After inspecting the repository, generate the files necessary to make the workflow operational. At minimum, consider:

1. Root IBM BOB instructions.
2. Scoped .NET, vertical-slice, API-contract, frontend, and testing instructions.
3. Orchestrator and specialist agent definitions.
4. Reusable prompt files for PRD analysis, contract generation, UX generation, slice implementation, test generation, change-impact analysis, and convergence.
5. SpecKit templates or extensions that fit the installed version.
6. A feature specification/template structure.
7. Schemas for component maps and workflow state where appropriate.
8. Scripts for OpenAPI validation, traceability validation, workflow-state validation, and contract drift checking.
9. CI workflow updates for applicable deterministic checks.
10. A usage guide explaining how a developer starts, advances, resumes, validates, and reviews a feature.
11. An example feature artifact set, only if requested or useful and clearly labeled as illustrative.
12. A report listing created/modified files, commands actually run, validation results, unresolved decisions, and next steps.

## Generation protocol

- First inspect and plan.
- Generate files incrementally, preserving existing content where possible.
- Prefer executable, minimal tooling over elaborate but nonfunctional scaffolding.
- Do not create duplicate commands or templates when existing SpecKit assets already serve the purpose.
- Validate syntax and paths of generated files.
- Run relevant checks when available.
- If a check cannot be run, report it as **not run**, with the reason.
- Distinguish implemented functionality from a proposed future enhancement.
- Do not claim the workflow is fully operational until its relevant scripts and CI paths have actually been exercised.
- Finish with a concise file inventory and a step-by-step developer usage guide.

---

# 21. Developer experience: intended end state

A developer should be able to:

1. Add or point the workflow at a PRD.
2. Ask IBM BOB to analyze the PRD and create a feature specification.
3. Resolve clarification questions.
4. Generate and review the technical plan and OpenAPI contract.
5. Generate UX flows, grayscale wireframes, and component mappings.
6. Generate dependency-ordered vertical-slice tasks.
7. Implement slices incrementally through IBM BOB.
8. Generate and run tests.
9. Run deterministic validation and convergence.
10. Review a report that links requirements, contracts, implementation, tests, and evidence.
11. Resume work later without relying on the original conversation.

The final workflow should be **SpecKit-driven, IBM BOB-friendly, repository-versioned, vertical-slice-oriented, contract-aware, test-evidence-based, secure by default, and resumable**.
