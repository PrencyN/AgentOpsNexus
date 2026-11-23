# Individual Learning Summary

## Overview

I designed and implemented AgentOps Nexus AI as a full-stack ReactJS, ASP.NET Core, and Microsoft Foundry capstone.

The project evolved from a deterministic mock-agent workflow into an evidence-grounded agentic application with:

- React request interface
- ASP.NET Core API
- Microsoft Foundry integration
- Agent-selected tools
- Server-Sent Events
- Evidence citations
- Persistent request history
- Failure and retry behavior
- OpenTelemetry
- Security controls
- Deterministic AI evaluation
- Automated tests
- GitHub Actions CI

The most important learning was that an AI-enabled application requires much more than a model call. Reliable AI software needs evidence boundaries, input validation, failure handling, persistence, observability, tests, security, and honest limitations.

## ReactJS learning

I improved my practical understanding of:

- React component composition
- TypeScript interfaces
- Controlled forms
- React Hook Form
- Zod validation
- Asynchronous state
- Loading states
- Error states
- Retry experiences
- Request-history presentation
- Server-Sent Events
- Markdown rendering
- Accessible evidence controls
- Keyboard focus management
- Vitest
- Testing Library
- Vite builds

The application explicitly represents:

```text
Idle
Submitting
Streaming
Retrying
Completed
Failed
Cancelled
```

The main frontend lesson was that failure, retry, cancellation, and empty states must be designed rather than treated as secondary conditions.

## ASP.NET Core learning

I implemented:

- API controllers
- Dependency injection
- Options validation
- ProblemDetails
- Middleware
- Correlation IDs
- Rate limiting
- Server-Sent Events
- Cancellation tokens
- Async streams
- Repository abstraction
- Entity Framework Core
- SQLite
- Migrations
- Logging scopes
- Security headers
- OpenTelemetry
- xUnit tests

The main backend lesson was that long-running model execution should not hold open database transactions.

The API persists request-status transitions before and after agent execution rather than keeping a transaction open while waiting for Foundry.

## GenAI learning

I connected an ASP.NET Core application to Microsoft Foundry.

The implementation includes:

- Project endpoint configuration
- Model deployment configuration
- Tenant-specific authentication
- Application-owned agent instructions
- Streaming model updates
- Timeout handling
- Cancellation handling
- Mock and Foundry provider selection

The main GenAI lesson was that response quality is not enough. A response also needs evidence, traceability, safe boundaries, and explicit missing information.

## Agentic AI learning

I learned the difference between a simple model call and an agentic workflow.

AgentOps Nexus supports agentic behavior because the model can decide when to invoke:

```text
GetRecentDeployments
```

Application code remains responsible for:

- Tool registration
- Tool descriptions
- Input validation
- Allowed environments
- Lookback limits
- Evidence normalization
- Logging
- Metrics
- Error handling
- Safety boundaries

The main agentic lesson was that the model must never define its own security boundary.

## Evidence-grounding learning

I implemented evidence identifiers such as:

```text
DEP-EV-001
DEP-EV-002
```

Deployment claims must cite exact evidence IDs.

The React UI transforms citations into interactive controls connected to evidence cards.

The deterministic evaluator compares cited identifiers with supplied identifiers.

The primary grounding lesson was:

> A recent deployment may support an investigation hypothesis, but correlation does not prove causation.

The response therefore separates:

```text
Known facts
Evidence
Hypotheses
Missing evidence
Recommended actions
Approval requirements
Limitations
```

## Persistence learning

I replaced temporary in-memory history with:

```text
Entity Framework Core
SQLite
```

The persistence model includes:

- Request ID
- Message
- Environment
- Severity
- Status
- Response
- Error code
- Retryable state
- Attempt number
- Retry parent
- Correlation ID
- Created time
- Started time
- Completed time

Challenges included:

- Async repository design
- EF entity configuration
- Retry relationships
- Migrations
- SQLite file exclusions
- Scoped DbContext lifetimes
- Persistence after restarts
- DateTimeOffset ordering in SQLite

The repository abstraction allows unit tests to continue using an in-memory implementation.

## Observability learning

I implemented:

- Correlation-ID generation
- Safe caller-ID preservation
- Unsafe-ID replacement
- Logging scopes
- ASP.NET Core instrumentation
- HTTP client instrumentation
- Custom activities
- Custom metrics
- Console export

Activities:

```text
agent.investigate
tool.GetRecentDeployments
```

Metrics:

```text
agentops.investigations
agentops.investigation.failures
agentops.investigation.duration
agentops.tool.invocations
agentops.tool.failures
agentops.tool.duration
agentops.tool.evidence.count
```

The main observability lesson was to record useful operational metadata without recording sensitive content.

Custom telemetry excludes prompts, generated responses, tokens, API keys, tenant IDs, and authentication codes.

## Security learning

I implemented:

- .NET user secrets
- Safe checked-in settings
- Git exclusions
- Input normalization
- Null-byte rejection
- Control-character rejection
- Maximum-length validation
- Security headers
- Rate limiting
- Correlation-ID validation
- Tool argument validation
- Deterministic output evaluation
- Dependency audits
- Human-approval constraints

Prompt injection cannot be solved by blocking a few phrases. Legitimate incident text may contain malicious instructions.

A stronger design combines:

- Instruction hierarchy
- User-data boundaries
- Bounded tools
- Input allowlists
- Evidence validation
- Unsupported-access restrictions
- Output evaluation
- Human approval
- Least privilege

## Testing learning

Backend testing uses:

```text
xUnit
Fluent Assertions
```

Frontend testing uses:

```text
Vitest
Testing Library
```

Validated results:

```text
Backend tests: 58 passed
Frontend tests: 18 passed
```

Tests cover:

- Repository behavior
- Retry relationships
- Mock agent events
- Deployment tools
- Tool validation
- Input policy
- Security headers
- Deterministic evaluation
- Form behavior
- Markdown rendering
- Evidence parsing
- Evidence navigation

The main testing lesson was to keep external AI calls out of deterministic CI.

Mock mode makes CI repeatable, fast, and credential-free. Real Foundry behavior is demonstrated separately.

## Continuous integration learning

I created a GitHub Actions workflow that performs:

- Repository-local .NET tool restore
- Release build
- Backend tests
- NuGet vulnerability audit
- EF migration verification
- `npm ci`
- Production dependency audit
- Development dependency report
- Frontend tests
- Frontend production build
- Artifact upload

The main CI lesson was that a successful local build is not sufficient.

A clean GitHub-hosted runner proves that committed files, lockfiles, tools, and tests are enough for another environment to build the project.

## Git and delivery learning

The project was developed through milestone branches:

```text
Milestone 1: Mock workflow
Milestone 2: Foundry integration
Milestone 3: Grounded tool
Milestone 4: Evidence UI
Milestone 5: Persistence and observability
Milestone 6: Security, evaluation, and CI
Milestone 7: Documentation
```

I practiced:

- Feature branches
- Small commits
- Stable checkpoints
- Pull requests
- CI validation
- Merge commits
- Branch cleanup
- Git ignore rules
- Secret review
- Remote synchronization

The main delivery lesson was that small, verified commits make troubleshooting and review easier.

## Architecture decisions

### Modular monolith

The backend remains a modular monolith because:

- One developer owns the platform.
- The domain is cohesive.
- Independent scaling is not required yet.
- A distributed system would increase local setup and testing complexity.

Interfaces remain available for a future Deployment Evidence service extraction.

### Single React application

The frontend remains one application because:

- The experience is cohesive.
- One owner controls the UI.
- Independent deployment is not required.
- A full microfrontend runtime would add unnecessary complexity.

A future evidence-panel remote may demonstrate independent composition.

### SQLite

SQLite provides simple, persistent local execution without Docker.

EF Core keeps the design portable to PostgreSQL.

### Server-Sent Events

SSE fits the current server-to-browser streaming direction.

The application does not require full bidirectional WebSocket communication.

## Challenges overcome

### Foundry streaming

I connected:

```text
Foundry update stream
→ ASP.NET Core async enumerable
→ Server-Sent Events
→ React state updates
```

### Evidence rendering

I converted:

```text
[DEP-EV-001]
```

into Markdown evidence links and rendered the links as accessible buttons connected to matching cards.

### EF command-line version

The global EF tool did not match the .NET 10 project.

I pinned:

```text
dotnet-ef 10.0.0
```

in the repository tool manifest.

### SQLite ordering

SQLite could not reliably translate the required DateTimeOffset ordering.

The small development history dataset is loaded and sorted in memory. A PostgreSQL implementation can perform ordering in SQL.

### Build file locking

A running API process locked assemblies during clean builds.

I learned to stop the API before clean and build operations.

### Dependency vulnerabilities

An older frontend toolchain introduced vulnerable development dependencies.

I upgraded and pinned Vite and Vitest while preserving a reproducible lockfile.

The remaining unpatched development-only advisory is documented rather than hidden.

### Deterministic evaluation

I designed repeatable checks for:

- Invented evidence
- Missing citations
- Causation overstatement
- Missing approval
- False execution claims
- Unsupported access
- Potential secret disclosure
- System-prompt disclosure

## Planned improvements

### Integration tests

Add:

```text
Microsoft.AspNetCore.Mvc.Testing
WebApplicationFactory<Program>
Temporary SQLite
```

### E2E tests

Add Playwright tests for:

- Success
- Invalid input
- Failure
- Retry
- Evidence navigation
- Persistent history

### Enterprise identity

Add:

```text
Microsoft Entra ID
MSAL React
Microsoft.Identity.Web
RBAC
User-scoped history
```

### Deployment evidence microservice

Extract:

```text
AgentOpsNexus.DeploymentEvidence.Api
```

Add typed HTTP clients, retries, timeout, circuit breaker, contract tests, and distributed tracing.

### Microfrontend proof

Extract EvidenceBadge and EvidencePanel into:

```text
frontend/evidence-remote
```

The shell will provide a local fallback.

### Production deployment

Use:

```text
Azure Static Web Apps
Azure Container Apps
Azure Database for PostgreSQL
Azure Key Vault
Managed Identity
Application Insights
Azure API Management
Bicep
```

### Performance

Add:

- Lazy loading
- Code splitting
- Bundle visualizer
- Bundle budgets
- Pagination
- Load testing

## Individual contribution

I personally worked through:

- Requirements analysis
- Architecture design
- Backend development
- Frontend development
- Foundry setup
- Prompt engineering
- Tool design
- Evidence modeling
- Persistence
- Retry workflow
- Observability
- Security controls
- Automated tests
- Dependency management
- Git workflows
- GitHub Actions
- Documentation
- Manual demonstration

I also diagnosed Windows and PowerShell-specific issues involving .NET CLI, EF tools, npm, Git, browser authentication, line endings, and locked build files.

## Conclusion

AgentOps Nexus strengthened my practical skills in:

```text
ReactJS
TypeScript
ASP.NET Core
C#
Entity Framework Core
SQLite
Microsoft Foundry
Microsoft Agent Framework
Azure Identity
GenAI
Agentic AI
OpenTelemetry
xUnit
Vitest
Git
GitHub Actions
Security
System design
```

The most significant lesson was that the model is only one component of an AI application.

Reliable AI software depends on evidence, validation, tool boundaries, persistence, observability, tests, security controls, user experience, and honest limitations.