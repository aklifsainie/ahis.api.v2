# Codex Project Bootstrap Prompt for Existing .NET 8 Solution

You are working inside an existing production-oriented `.NET 8` solution that was designed and implemented entirely by human engineers.

This repository currently has:

- No Codex-specific project instructions
- No root `AGENTS.md`
- No module-specific `AGENTS.md`
- No Codex Skills created specifically for this repository
- No AI-generated architecture documentation
- Existing architecture, coding patterns, business logic, and conventions created by human engineers

Your job is to deeply analyze the existing solution and bootstrap a maintainable Codex operating structure for future AI-assisted development.

The existing source code, project references, database implementation, tests, configuration, naming conventions, and current implementation patterns are the primary source of truth.

Do not redesign the system into your preferred architecture.

---

# PRIMARY OBJECTIVE

Analyze the entire solution and establish enough persistent Codex context so that future prompts such as:

- Add a new module
- Add a new sub-module
- Add a new API controller
- Add a new endpoint
- Add an entity
- Add or change business logic
- Add a command
- Add a query
- Add a service
- Add a repository
- Modify an existing feature
- Add database persistence
- Add an EF Core migration
- Add tests
- Refactor a module
- Debug a defect

can be executed consistently with the existing architecture.

After bootstrapping, Codex should be able to determine:

1. Which business module owns the requirement
2. Which project contains the correct layer
3. Which folders should be used
4. Which existing files should be modified
5. Which new files should be created
6. Which existing pattern should be copied
7. Which business rules apply
8. Whether a repository or service should be used
9. Which tests should be created or updated
10. Which documentation should be updated
11. Which verification commands should be executed

---

# CRITICAL EXECUTION WORKFLOW

The following workflow applies to every future request that will add, modify, refactor, or remove application code.

---

## Execution Constraints & Strategy

1. Do not generate the complete implementation code immediately.

2. First output a high-level architectural blueprint detailing:

   - The affected module or sub-module
   - The relevant existing implementation pattern
   - The proposed design or pattern for the change
   - Whether the implementation should use an existing:
     - Repository pattern
     - Service pattern
     - Factory pattern
     - Strategy pattern
     - Handler pattern
     - Domain service
     - Application service
     - Or another pattern already used by this repository
   - Why that pattern is appropriate
   - A list of specific existing files that will be modified
   - A list of specific new files that will be created
   - Database impact
   - API impact
   - Business-rule impact
   - Test impact
   - Documentation impact

3. Do not write the actual implementation file contents until the user explicitly approves the architectural blueprint.

The expected first response for implementation requests should therefore resemble:

```text
Architecture Blueprint

Affected module:
Renewal

Existing pattern discovered:
CQRS + repository + MediatR handler

Proposed flow:
Controller
    -> Command
    -> CommandHandler
    -> RenewalService
    -> RenewalRepository
    -> DbContext

Files to add:
- ...
- ...

Files to modify:
- ...

Business rules:
- ...

Database impact:
- ...

Tests:
- ...

Risks / assumptions:
- ...

Awaiting architectural approval before implementation.
```

Do not bypass this approval gate even when the implementation appears simple.

---

# IMPORTANT DISTINCTION

The architectural approval gate applies to:

- New modules
- New sub-modules
- New endpoints
- New controllers
- Business-rule changes
- Database changes
- Refactoring
- Repository changes
- Service changes
- Domain changes
- Infrastructure changes

It does not need to block clearly read-only tasks such as:

- Explaining existing code
- Tracing a request
- Searching for a file
- Reviewing code
- Explaining architecture
- Identifying where a feature belongs
- Investigating an error without modifying code

---

# PHASE 1 — DISCOVER THE SOLUTION

Inspect the repository from the solution root.

Identify:

- `.sln`
- `.slnx`
- `.csproj`
- `Directory.Build.props`
- `Directory.Build.targets`
- `Directory.Packages.props`
- `global.json`
- `NuGet.config`
- Application settings
- EF Core configuration
- Docker configuration
- CI/CD configuration
- Test projects
- Source folders
- Shared libraries
- Domain projects
- Application projects
- Infrastructure projects
- API projects
- Worker projects
- Background processors
- Integration components
- Device-integration components
- Existing documentation

Produce a project inventory.

For every project identify:

- Project name
- Path
- Target framework
- Project type
- Main responsibility
- Direct project references
- Important NuGet packages
- Whether it is executable
- Whether it contains database persistence
- Whether it contains domain entities
- Whether it contains application logic
- Whether it contains API endpoints
- Whether it contains tests

Do not determine responsibility from project names alone.

Verify responsibilities by reading representative source files.

---

# PHASE 2 — DISCOVER THE ARCHITECTURE

Determine what architecture the repository actually uses.

Possible patterns may include:

- Layered Architecture
- Clean Architecture
- Onion Architecture
- Modular Monolith
- Traditional Monolith
- Vertical Slice Architecture
- CQRS
- MediatR
- Repository Pattern
- Generic Repository
- Unit of Work
- Domain Services
- Application Services
- Controller-Service-Repository
- Feature-based folders
- Module-based folders

Do not force terminology if evidence is weak.

Document only patterns supported by repository evidence.

Identify the real dependency direction.

For example, if the repository actually uses:

```text
API
    -> Application
        -> Domain

Infrastructure
    -> Application
    -> Domain
```

document that.

If the repository does something different, document the actual implementation instead.

---

# PHASE 3 — IDENTIFY BUSINESS MODULES

Identify every genuine business module.

For each module determine:

- Module name
- Module location
- Business purpose
- Main use cases
- Entities
- View models
- Commands
- Queries
- Handlers
- Repositories
- Services
- Controllers
- Background processors
- External integrations
- Database tables
- DbContext usage
- Status enums
- Constants
- Validation logic
- Tests
- Dependencies on other modules

Distinguish business modules from technical concerns such as:

- Middleware
- Logging
- Extensions
- Helpers
- Common
- Shared
- Utilities
- Configuration

Do not create module boundaries based only on folder names.

---

# PHASE 4 — LEARN THE HUMAN ENGINEERING WORKFLOW

The human engineer who owns this repository generally follows the workflow below when creating a new endpoint.

Treat this as the default engineering workflow unless the existing source code for the affected module clearly demonstrates a different pattern.

---

# DEFAULT ENDPOINT DEVELOPMENT WORKFLOW

When implementing a new endpoint, evaluate the following sequence.

## Step 1 — Domain Entity

Determine whether the feature requires a new entity.

If required:

Create or modify the model entity in the Domain layer.

Inspect existing entities first for:

- Base entity inheritance
- Key conventions
- Nullable conventions
- Audit fields
- Soft-delete fields
- Navigation properties
- Status fields
- Constructor conventions
- Default values
- Data annotations
- Naming conventions

Do not create a new entity if the requirement belongs to an existing aggregate or model.

---

## Step 2 — Entity Type Configuration

If a new persisted entity is required:

Create its Entity Framework Core `IEntityTypeConfiguration<T>` implementation in the Infrastructure layer.

Follow existing conventions for:

- Table name
- Schema
- Primary key
- Column length
- Required fields
- Default values
- Relationships
- Foreign keys
- Indexes
- Unique constraints
- Delete behaviour
- Precision
- Value conversion
- Seed data

Do not rely only on conventions if existing entities use explicit configuration.

---

## Step 3 — View Model

Determine whether the endpoint requires a ViewModel, DTO, request model, response model, or other projection.

The current human engineering convention generally places entity-related ViewModels in the Domain layer.

Follow the existing repository implementation exactly.

Do not relocate ViewModels into another layer merely because generic Clean Architecture guidance recommends doing so.

Inspect similar ViewModels and follow:

- Naming
- Folder
- Namespace
- Mapping style
- Property naming
- Nullable handling

---

## Step 4 — Repository Contract and Implementation

Determine whether the entity or feature requires repository access.

If required:

Create or modify:

```text
Application Layer
    -> IRepository interface

Infrastructure Layer
    -> Repository implementation
```

Follow existing repository patterns.

Inspect:

- Generic repository usage
- Specific repository inheritance
- Constructor injection
- DbContext access
- AsNoTracking usage
- Include patterns
- Query patterns
- Pagination
- CancellationToken support
- Save behaviour
- Unit of Work usage
- Result handling
- Soft-delete filtering

Do not create a new repository when an existing repository already owns the required persistence operation.

---

## Step 5 — Evaluate Whether a Service Is Required

Do not automatically create a service.

Before creating a service, evaluate the business complexity.

A service is more appropriate when:

- Multiple repositories are coordinated
- Multiple entities participate
- Complex business logic exists
- External services are involved
- The logic is reusable
- Transaction orchestration is required
- Business workflows span multiple operations
- Handler complexity would become excessive

Direct repository use may be appropriate when:

- The operation is simple
- Only one repository is involved
- No reusable business workflow exists
- Existing repository conventions already support the operation
- The handler would remain small and understandable

Before implementation, explicitly state:

```text
Service required: Yes / No

Reason:
...
```

Do not introduce an unnecessary service layer.

---

## Step 6 — Command or Query and Handler

Create the appropriate CQRS operation in the Application layer.

Use:

- Command for state-changing operations
- Query for read operations

Create the associated handler.

The handler should delegate to:

```text
Repository
```

or:

```text
Service
```

depending on the architectural evaluation in Step 5.

Inspect existing features for:

- Folder organization
- Naming
- MediatR usage
- Request type
- Result type
- FluentResults or other result pattern
- Validation
- Logging
- CancellationToken
- Error handling
- Not-found handling
- Success messages

Do not invent another CQRS style.

---

## Step 7 — Controller

Create or modify the Controller according to existing API conventions.

Inspect:

- Base controller inheritance
- Route conventions
- API versioning
- Authorization
- ProducesResponseType
- Swagger annotations
- Dependency injection
- MediatR usage
- Response conversion
- ProblemDetails handling

Avoid business logic inside controllers.

---

## Step 8 — Feature Endpoint

Add the specific endpoint for the feature.

Before implementation verify:

- HTTP method
- Route
- Request model
- Response model
- Status codes
- Validation behaviour
- Authorization
- Error behaviour
- Idempotency requirements
- Query-string or route parameters
- CancellationToken

The endpoint should follow the existing controller structure.

---

# ENDPOINT FLOW SUMMARY

Use this as the default conceptual flow:

```text
Domain
│
├── Entity
└── ViewModel / DTO where existing convention requires it

Infrastructure
│
├── EntityTypeConfiguration
└── Repository Implementation

Application
│
├── IRepository
│
├── Service Interface / Service if required
│
└── Feature
    ├── Command / Query
    └── Handler

API
│
└── Controller
    └── Endpoint
```

Potential runtime flow:

```text
HTTP Request
    ↓
Controller Endpoint
    ↓
MediatR
    ↓
Command / Query Handler
    ↓
Service              <- only when justified
    ↓
Repository
    ↓
DbContext
    ↓
SQL Server
```

Or, for simple operations:

```text
HTTP Request
    ↓
Controller Endpoint
    ↓
MediatR
    ↓
Command / Query Handler
    ↓
Repository
    ↓
DbContext
    ↓
SQL Server
```

Always determine which flow matches the actual module before implementation.

---

# PHASE 5 — TRACE REPRESENTATIVE FEATURES

Before creating Codex instructions, trace multiple existing endpoints from beginning to end.

At minimum inspect:

1. One Create endpoint
2. One GetById endpoint
3. One GetAll or paginated query
4. One Update endpoint
5. One Delete or soft-delete endpoint
6. One endpoint containing meaningful business rules
7. One endpoint using a service, if available
8. One endpoint directly using a repository, if available

For each example trace:

```text
Controller
    ↓
Command / Query
    ↓
Handler
    ↓
Service, if applicable
    ↓
Repository Interface
    ↓
Repository Implementation
    ↓
DbContext
    ↓
Entity configuration
    ↓
Database
```

Also trace relevant DTO/ViewModel mapping.

The purpose is to discover exactly how human engineers currently implement features.

---

# PHASE 6 — DISCOVER CODING CONVENTIONS

Document established conventions including:

- Entity naming
- Entity base classes
- ViewModel naming
- DTO naming
- Repository naming
- Repository interface placement
- Repository implementation placement
- Service naming
- Command naming
- Query naming
- Handler naming
- Controller naming
- Endpoint routes
- Response DTOs
- Result wrappers
- Validation
- Logging
- Error handling
- Soft delete
- Auditing
- Date handling
- EF Core configuration
- Dependency injection
- Async conventions
- CancellationToken usage
- Testing

When conflicting patterns exist:

Identify:

- Dominant current pattern
- Module-specific variation
- Older pattern
- Possible legacy pattern

Do not refactor inconsistencies during bootstrap.

---

# PHASE 7 — DISCOVER BUSINESS RULES

Extract important business rules from:

- Entities
- Handlers
- Services
- Validators
- Repository queries
- Controllers
- Enums
- Constants
- Status checks
- Database constraints
- Tests
- Existing documents
- Comments

Assign rule identifiers.

Example:

```text
REN-001
ENR-001
REC-001
PAS-001
```

For each rule record:

- Rule ID
- Description
- Owning module
- Enforcement location
- Tests
- Related entities
- Related statuses
- Exceptions
- Confidence level

Classify confidence as:

```text
Confirmed
Strongly inferred
Uncertain
```

Do not convert inferred behaviour into confirmed business requirements.

---

# CODEX PROJECT INSTRUCTION STRUCTURE

After analyzing the repository, create the following structure.

Adapt names and paths according to the actual repository.

```text
<solution-root>/
│
├── AGENTS.md
│
├── REVIEW.md
│
├── docs/
│   ├── architecture/
│   │   ├── solution-overview.md
│   │   ├── project-map.md
│   │   ├── dependency-rules.md
│   │   ├── endpoint-development-flow.md
│   │   ├── request-lifecycle.md
│   │   ├── persistence.md
│   │   ├── error-handling.md
│   │   ├── authentication-authorization.md
│   │   └── testing-strategy.md
│   │
│   ├── decisions/
│   │   └── README.md
│   │
│   └── glossary/
│       └── business-terms.md
│
├── .agents/
│   └── skills/
│       ├── solution-explorer/
│       │   └── SKILL.md
│       │
│       ├── create-module/
│       │   └── SKILL.md
│       │
│       ├── create-submodule/
│       │   └── SKILL.md
│       │
│       ├── create-endpoint/
│       │   └── SKILL.md
│       │
│       ├── add-command/
│       │   └── SKILL.md
│       │
│       ├── add-query/
│       │   └── SKILL.md
│       │
│       ├── create-entity/
│       │   └── SKILL.md
│       │
│       ├── create-domain-feature/
│       │   └── SKILL.md
│       │
│       ├── create-ef-migration/
│       │   └── SKILL.md
│       │
│       ├── write-tests/
│       │   └── SKILL.md
│       │
│       ├── trace-request/
│       │   └── SKILL.md
│       │
│       ├── review-module/
│       │   └── SKILL.md
│       │
│       ├── debug-api-error/
│       │   └── SKILL.md
│       │
│       └── verify-solution/
│           └── SKILL.md
│
└── <module-root>/
    └── <ModuleName>/
        ├── AGENTS.md
        └── docs/
            ├── module-overview.md
            ├── business-rules.md
            ├── use-cases.md
            ├── data-model.md
            ├── integration-points.md
            └── status-transitions.md
```

Only create files that contain meaningful information.

Do not create empty documentation merely to satisfy this structure.

---

# ROOT `AGENTS.md`

Create a concise repository-level `AGENTS.md`.

Treat the root file as the operating map for Codex rather than the complete encyclopedia.

It should contain:

## System Overview

Briefly explain what the solution does.

## Technology Stack

Document confirmed technologies such as:

- .NET version
- ASP.NET Core
- EF Core
- SQL Server
- MediatR
- FluentResults
- FluentValidation
- Logging
- Testing frameworks

Only include technologies found in the repository.

## Solution Map

List projects and responsibilities.

## Architecture Rules

Document actual dependency direction.

## Module Map

List business modules and locations.

## Default Endpoint Development Workflow

Include the human workflow:

```text
1. Domain entity
2. Infrastructure EntityTypeConfiguration
3. Domain ViewModel
4. Application IRepository
5. Infrastructure Repository implementation
6. Evaluate Service necessity
7. Application Command / Query + Handler
8. Controller
9. Specific Endpoint
10. Tests
11. Verification
```

Explain that steps may be skipped only when not applicable.

## Mandatory Architectural Approval Gate

Include:

```text
Before creating or modifying implementation code:

1. Analyze the relevant existing implementation.
2. Produce the architectural blueprint.
3. List files to add and modify.
4. Explain repository/service/pattern decisions.
5. Wait for explicit user approval.
6. Only then implement.
```

## Coding Conventions

Summarize established conventions.

## Build/Test Commands

Document safe commands.

## Documentation Map

Point Codex to deeper files in `docs/`.

Keep this root `AGENTS.md` concise.

---

# MODULE-LEVEL `AGENTS.md`

Create module-level `AGENTS.md` files for genuine business modules.

Example:

```text
src/.../Renewal/AGENTS.md
```

Each module instruction file should contain:

- Module purpose
- Business ownership
- Projects/folders belonging to the module
- Entities owned
- Repositories owned
- Services owned
- Controllers
- Commands
- Queries
- Integration boundaries
- Persistence rules
- Important business rules
- Status model
- Testing expectations
- Required files to inspect before modifying the module
- Links to module docs

Do not duplicate the root instructions.

Nested module instructions should focus only on that module.

---

# MODULE DOCUMENTATION

For each meaningful business module create:

```text
docs/
├── module-overview.md
├── business-rules.md
├── use-cases.md
├── data-model.md
├── integration-points.md
└── status-transitions.md
```

Only create `status-transitions.md` when status-based lifecycle logic exists.

---

# BUSINESS RULE DOCUMENTATION

Use stable rule IDs.

Example:

```markdown
# Renewal Business Rules

## REN-001 — Only issued passports may renew

A passport may begin renewal enrolment only when its current status is `Issued`.

Ownership:
Renewal

Required source data:
Passport status

Enforcement:
- ...

Tests:
- ...

Confidence:
Confirmed
```

Never invent unsupported rules.

---

# CODEX SKILLS PHILOSOPHY

Skills must represent reusable engineering workflows.

Do not create one skill per module.

Correct:

```text
create-module
create-submodule
create-endpoint
create-entity
add-command
add-query
write-tests
```

Avoid:

```text
create-renewal-endpoint
create-payment-endpoint
create-record-management-endpoint
```

Business-specific knowledge belongs in:

```text
Module AGENTS.md
+
Module docs/
```

Engineering procedures belong in reusable Skills.

---

# REQUIRED SKILL — `solution-explorer`

Purpose:

Determine where a requested change belongs.

The skill should:

1. Read root `AGENTS.md`.
2. Determine likely module.
3. Read nested `AGENTS.md`.
4. Read relevant architecture docs.
5. Read relevant module docs.
6. Inspect similar source code.
7. Trace dependencies.
8. Return:

```text
Owning module
Relevant projects
Relevant existing files
Similar implementation
Recommended location
Likely files to modify
Potential files to add
Business rules
Architectural risks
Open uncertainties
```

This skill performs analysis only.

---

# REQUIRED SKILL — `create-module`

Purpose:

Create a new business module following repository conventions.

Before implementation:

1. Determine whether a new module is justified.
2. Identify related existing modules.
3. Identify data ownership.
4. Identify integration boundaries.
5. Identify similar modules.
6. Produce the mandatory architectural blueprint.
7. List exact files/projects to create.
8. Wait for approval.

After approval:

- Create code
- Create module `AGENTS.md`
- Create business-rule documentation
- Add tests
- Update solution documentation
- Verify build/tests

---

# REQUIRED SKILL — `create-submodule`

Use the same approval workflow.

Determine:

- Parent module
- Whether a sub-module is architecturally justified
- Folder location
- Ownership
- Existing related functionality
- Whether separate projects are necessary

Do not create a `.csproj` simply because the business concept has a name.

---

# REQUIRED SKILL — `create-endpoint`

This skill must follow the human endpoint workflow.

Before implementation:

## 1. Entity Evaluation

Determine whether a Domain entity must be created or modified.

## 2. EF Configuration Evaluation

Determine whether Infrastructure configuration must be created or modified.

## 3. ViewModel Evaluation

Determine whether a Domain ViewModel is required.

## 4. Repository Evaluation

Determine whether:

```text
Application IRepository
+
Infrastructure Repository
```

must be created or extended.

## 5. Service Evaluation

Explicitly report:

```text
Service required: Yes / No

Reason:
...
```

## 6. CQRS Evaluation

Determine whether the endpoint uses:

```text
Command
```

or:

```text
Query
```

and identify the handler.

## 7. Controller Evaluation

Determine:

- Existing controller
- New controller requirement
- Route
- Base class
- Result mapping

## 8. Endpoint Definition

Define:

- HTTP verb
- Route
- Request
- Response
- Validation
- Status codes
- Authorization

## 9. Blueprint

Produce:

```text
Endpoint Architectural Blueprint

Feature:
...

Module:
...

Existing similar endpoint:
...

Runtime flow:
Controller
    -> Command/Query
    -> Handler
    -> Service (if required)
    -> Repository
    -> DbContext

Entity:
Create / Modify / Reuse

EntityTypeConfiguration:
Create / Modify / Not required

ViewModel:
Create / Modify / Reuse / Not required

Repository:
Create / Extend / Reuse

Service required:
Yes / No

Reason:
...

Command/Query:
...

Controller:
...

Endpoint:
...

Files to add:
...

Files to modify:
...

Business rules:
...

Database impact:
...

Tests:
...

Risks:
...
```

Then stop and wait for approval.

Only after explicit approval should the skill generate implementation code.

---

# REQUIRED SKILL — `create-domain-feature`

Used when changing business rules.

Before implementation:

- Identify current rule location
- Trace all call sites
- Find related tests
- Determine correct enforcement layer
- Determine database implications
- Produce blueprint
- Wait for approval

After implementation:

- Add regression tests
- Update business rules documentation

---

# REQUIRED SKILL — `create-entity`

Before creating an entity inspect:

- Existing BaseEntity
- Key patterns
- Audit fields
- Soft-delete
- Navigation properties
- Foreign keys
- Nullability
- Existing EntityTypeConfigurations

Produce blueprint first.

Do not create implementation before approval.

---

# REQUIRED SKILL — `add-command`

Determine:

- Target module
- Existing command structure
- Request object
- Handler
- Repository/service dependencies
- Business rule enforcement
- Result pattern
- Logging
- Validation
- Tests

Produce blueprint first.

---

# REQUIRED SKILL — `add-query`

Determine:

- Target module
- Query structure
- Repository query pattern
- ViewModel / projection
- AsNoTracking behaviour
- Pagination
- Result pattern
- Tests

Produce blueprint first.

---

# REQUIRED SKILL — `create-ef-migration`

Never generate or apply a migration immediately.

First inspect:

- Entity changes
- Configuration changes
- DbContext
- Existing migration project
- Startup project
- Migration naming
- Database provider

Produce:

```text
Migration Blueprint

Entities affected:
...

Tables affected:
...

Columns:
...

Indexes:
...

Foreign keys:
...

Potential destructive changes:
...

Existing data risks:
...

Recommended migration name:
...
```

Wait for approval.

After approval, the migration may be generated.

Do not apply a migration to a real database unless explicitly requested.

---

# REQUIRED SKILL — `write-tests`

Inspect existing test conventions before proposing tests.

Identify:

- xUnit / NUnit / MSTest
- Moq / NSubstitute
- FluentAssertions
- Test fixture style
- Test naming
- Arrange/Act/Assert conventions
- Integration test infrastructure

Produce test plan before writing tests when tests are part of a code change.

---

# REQUIRED SKILL — `trace-request`

Trace an existing feature:

```text
HTTP Request
    ↓
Controller
    ↓
Command / Query
    ↓
Handler
    ↓
Service
    ↓
Repository Interface
    ↓
Repository Implementation
    ↓
DbContext
    ↓
Entity
    ↓
EntityTypeConfiguration
    ↓
Database
```

Skip steps not present.

Also identify:

- ViewModel
- Mapping
- Validation
- Error handling
- Result conversion
- Tests

No code modifications.

---

# REQUIRED SKILL — `review-module`

Review:

- Layer violations
- Module boundary violations
- Repository misuse
- Unnecessary services
- Overloaded handlers
- Business logic in controllers
- Duplicate business rules
- Validation gaps
- Error handling
- Logging
- Security
- EF Core query issues
- N+1
- Tracking misuse
- Soft-delete mistakes
- Missing tests

Return findings only.

Do not refactor automatically.

---

# REQUIRED SKILL — `debug-api-error`

Use repository context to diagnose errors.

Trace:

```text
Request
-> Controller
-> Handler
-> Service
-> Repository
-> DbContext
-> SQL
```

Identify:

- Symptom
- Root cause
- Evidence
- Smallest proposed fix
- Files affected
- Regression test

If a code change is required, invoke the architectural approval workflow before modifying files.

---

# REQUIRED SKILL — `verify-solution`

Discover the repository’s actual build/test commands.

Prefer:

```text
Affected project first
↓
Affected tests
↓
Solution build
↓
Broader test suite
```

Do not run destructive commands.

Report failures accurately.

---

# SERVICE DECISION RULE

Because this repository's human development process intentionally evaluates whether a service is necessary, every new endpoint blueprint must include this decision explicitly.

Example:

```text
Service required: No

Reason:
The operation contains a single repository lookup and no reusable business
workflow. Existing GetById features in this module allow handlers to communicate
directly with repositories.
```

Or:

```text
Service required: Yes

Reason:
The operation coordinates Passport, Renewal, and Biometric repositories and
contains reusable eligibility logic used by multiple commands.
```

Do not create a service merely to produce another abstraction layer.

---

# EXAMPLE — FUTURE RENEWAL REQUEST

If the user asks:

```text
Create Renewal module.

Business rule:
Only an issued passport can enrol for renewal.
```

Do not immediately create code.

First analyze:

- Passport entity
- Passport status
- Existing enrolment module
- Existing issuance flow
- Whether Renewal should be module or sub-module
- Similar modules
- Repository boundaries
- Existing services
- Data ownership

Then produce something like:

```text
Renewal Architectural Blueprint

Recommended boundary:
Renewal as a sub-module of Enrolment

Reason:
...

Existing source of passport status:
...

Confirmed rule:
REN-001 — Only passports with status Issued may start renewal.

Recommended flow:

RenewalController
    ↓
StartRenewalCommand
    ↓
StartRenewalCommandHandler
    ↓
RenewalEligibilityService
    ↓
PassportRepository
    ↓
Database

Service required:
Yes

Reason:
...

Files to add:
...

Files to modify:
...

Database impact:
...

Tests:
...

Documentation:
...
```

Then wait for user approval.

---

# NEW MODULE DOCUMENTATION RULE

Whenever a new genuine module is implemented, after approval Codex should normally create:

```text
<Module>/
├── AGENTS.md
└── docs/
    ├── module-overview.md
    ├── business-rules.md
    ├── use-cases.md
    ├── data-model.md
    └── integration-points.md
```

Add:

```text
status-transitions.md
```

only when applicable.

---

# DOCUMENTATION UPDATE RULE

After a confirmed business or architectural change, update only documentation that has become inaccurate.

Possible files:

- Root `AGENTS.md`
- Module `AGENTS.md`
- `business-rules.md`
- `use-cases.md`
- `data-model.md`
- `integration-points.md`
- `status-transitions.md`
- Architecture docs

Do not document temporary implementation details.

Do not create excessive documentation.

---

# CODEX INTERACTION PRINCIPLE

Future prompts from the user may be short.

Example:

```text
Add Update Country endpoint.
```

Do not require the user to restate the entire architecture.

Codex should automatically:

1. Read applicable `AGENTS.md`.
2. Find the Country feature.
3. Inspect similar endpoints.
4. Follow the default human endpoint workflow.
5. Produce the architectural blueprint.
6. Wait for approval.
7. Implement after approval.
8. Test.
9. Update documentation if required.

The repository knowledge system should reduce the amount of repeated prompting required.

---

# ROOT REVIEW INSTRUCTIONS

Create:

```text
REVIEW.md
```

Document code-review priorities such as:

- Wrong-layer dependencies
- Business logic inside controllers
- Repository implementations outside Infrastructure
- Repository interfaces outside Application when inconsistent with current architecture
- Incorrect entity placement
- Incorrect ViewModel placement
- Unnecessary services
- Direct DbContext access where repository convention is required
- CQRS violations
- Missing business-rule tests
- Incorrect soft delete
- Unsafe EF Core migrations
- Missing authorization
- Sensitive logging
- Breaking API changes
- N+1 queries
- Incorrect tracking
- Missing CancellationToken propagation

Only include rules supported by the repository architecture.

---

# ARCHITECTURE DOCUMENTATION

Create:

```text
docs/architecture/endpoint-development-flow.md
```

This is especially important.

Document the discovered implementation workflow and the human developer's preferred process:

```text
Feature Requirement
    ↓
1. Evaluate/Create Domain Entity
    ↓
2. EntityTypeConfiguration
    ↓
3. Domain ViewModel
    ↓
4. IRepository
    ↓
5. Repository Implementation
    ↓
6. Evaluate Service Requirement
    ↓
7. Command / Query
    ↓
8. Handler
    ↓
9. Controller
    ↓
10. Endpoint
    ↓
11. Tests
    ↓
12. Build / Verify
```

For every stage explain:

- When it is required
- When it may be skipped
- Which project owns it
- Existing example files

Do not turn optional steps into mandatory abstractions.

---

# INITIAL BOOTSTRAP SAFETY RULES

During this bootstrap analysis:

Do NOT:

- Change production implementation
- Refactor code
- Rename projects
- Move files
- Modify database schema
- Create migrations
- Add NuGet packages
- Change application settings
- Change deployment configuration
- Alter business logic
- Reformat unrelated files

You may create only:

- Codex instruction files
- Codex skills
- Architecture documentation
- Module documentation

Run read-only inspection and safe build/test commands where useful.

---

# INITIAL BOOTSTRAP EXECUTION ORDER

Perform the bootstrap in this order:

## Stage 1 — Repository Discovery

Inspect the whole solution.

Do not create files yet.

## Stage 2 — Architecture Report

Report:

- Solution structure
- Architecture
- Projects
- Modules
- Dependencies
- Endpoint development pattern
- Repository pattern
- Service usage
- CQRS implementation
- Controller structure
- EF Core structure
- Testing structure
- Business-rule locations
- Important inconsistencies

## Stage 3 — Proposed Codex Knowledge Structure

Before creating Codex files, show:

```text
Files to create
Files to modify
Module AGENTS.md locations
Skills to create
Documentation to create
Files intentionally omitted
```

## Stage 4 — WAIT FOR APPROVAL

Do not create any Codex bootstrap files until the user approves the proposed structure.

This approval requirement applies even during initial bootstrap.

## Stage 5 — Generate Codex Structure

After approval create:

- Root `AGENTS.md`
- Relevant nested `AGENTS.md`
- Codex Skills
- Architecture documentation
- Business module documentation
- `REVIEW.md`

## Stage 6 — Self Review

Cross-check generated instructions against source code.

Look for:

- Incorrect assumptions
- Wrong module ownership
- Incorrect project paths
- Wrong patterns
- Duplicate documentation
- Inferred rules accidentally marked as confirmed

## Stage 7 — Safe Verification

Run appropriate safe commands such as:

```bash
dotnet restore
dotnet build
dotnet test
```

Use the actual solution/project paths.

Do not run database update commands.

## Stage 8 — Final Bootstrap Report

Report:

### Architecture discovered

...

### Projects

...

### Modules

...

### Endpoint development workflow

...

### Codex files created

...

### Documentation created

...

### Skills created

...

### Confirmed business rules

...

### Inferred rules

...

### Uncertainties

...

### Build results

...

### Test results

...

### Recommended next actions

...

---

# FUTURE PROMPT EXECUTION RULE

Once bootstrap is complete, every code-changing prompt must follow:

```text
User Requirement
      ↓
Read applicable AGENTS.md
      ↓
Read relevant module docs
      ↓
Inspect existing equivalent implementation
      ↓
Trace dependencies
      ↓
Determine business-rule impact
      ↓
Determine Entity impact
      ↓
Determine EF Configuration impact
      ↓
Determine ViewModel impact
      ↓
Determine Repository impact
      ↓
Determine Service necessity
      ↓
Determine Command / Query
      ↓
Determine Handler
      ↓
Determine Controller
      ↓
Determine Endpoint
      ↓
Determine Tests
      ↓
ARCHITECTURAL BLUEPRINT
      ↓
WAIT FOR USER APPROVAL
      ↓
IMPLEMENT
      ↓
TEST
      ↓
REVIEW
      ↓
UPDATE DOCUMENTATION IF REQUIRED
```

The approval stage is mandatory.

---

# FINAL PRINCIPLE

Your goal is not to transform this human-engineered repository into an AI-designed repository.

Your goal is to make Codex behave like an engineer who has been properly onboarded into the existing team.

Learn the architecture.

Learn the conventions.

Learn module ownership.

Learn the business rules.

Learn the human endpoint-development workflow.

Reuse existing patterns.

Propose architecture before implementation.

Wait for approval.

Then implement the smallest correct change.