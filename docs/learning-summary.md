# Individual Learning Summary

## Overview

I designed and implemented AgentOps Nexus AI as a full-stack React, ASP.NET Core, Microsoft Foundry, and Microsoft Agent Framework capstone.

The project now includes:

- React incident workspace
- ASP.NET Core API
- Foundry and Mock providers
- Agent-selected tools
- Server-Sent Events
- Evidence citations
- Evidence navigation
- Persistent history
- Failure and retry
- OpenTelemetry
- Security controls
- Deterministic evaluation
- Unit tests
- API integration tests
- Component tests
- Playwright browser tests
- GitHub Actions CI

The main lesson was that an AI application requires much more than a model call. Reliable AI software needs evidence boundaries, validation, persistence, observability, security, deterministic tests, and transparent limitations.

## React and TypeScript learning

I improved my understanding of:

- Component composition
- Controlled forms
- React Hook Form
- Zod validation
- Asynchronous state
- Loading and retry states
- Server-Sent Events
- Markdown rendering
- Accessible controls
- Keyboard focus
- Persistent history
- Vitest
- Testing Library
- Playwright

The interface represents:

```text
Idle
Submitting
Streaming
Retrying
Completed
Failed
Cancelled
```

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
- EF Core
- SQLite
- Migrations
- OpenTelemetry
- Unit tests
- Integration tests

## GenAI learning

I connected the API to Microsoft Foundry with:

- Project endpoint configuration
- Model deployment configuration
- Tenant-specific authentication
- Application-owned instructions
- Streaming model updates
- Timeout handling
- Cancellation handling
- Mock and Foundry provider selection

The key lesson was that model quality is not enough. Responses also require evidence, traceability, safe boundaries, and explicit missing information.

## Agentic AI learning

The model can decide whether to call:

```text
GetRecentDeployments
```

Application code controls:

- Tool registration
- Tool descriptions
- Allowed arguments
- Environment restrictions
- Lookback limits
- Evidence structure
- Logging
- Metrics
- Error handling
- Safety boundaries

The model must not define its own security boundary.

## Evidence-grounding learning

The project uses identifiers such as:

```text
DEP-EV-001
DEP-EV-002
```

The React UI turns citations into accessible buttons connected to evidence cards.

The central grounding rule is:

> Correlation does not prove causation.

Responses separate:

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

I implemented:

```text
Entity Framework Core
SQLite
```

The persistence model includes:

- Request identity
- Incident description
- Environment
- Severity
- Status
- Response
- Error code
- Retryable state
- Attempt number
- Retry parent
- Correlation ID
- Timestamps

Retry preserves the original failed request and creates a linked second attempt.

## Observability learning

I implemented:

- Correlation-ID propagation
- Safe caller-ID preservation
- Unsafe-ID replacement
- Logging scopes
- ASP.NET Core instrumentation
- HTTP client instrumentation
- Custom activities
- Custom metrics
- Console export

Custom telemetry excludes prompt text, response text, tokens, API keys, tenant IDs, and user-secret values.

## Security learning

I implemented:

- User secrets
- Safe checked-in settings
- Input normalization
- Null-byte rejection
- Control-character rejection
- Length validation
- Security headers
- Rate limiting
- Correlation-ID validation
- Tool validation
- Output evaluation
- Dependency audits
- Human-approval constraints

Prompt injection cannot be solved by blocking a small list of phrases. Stronger security combines instruction hierarchy, bounded tools, evidence validation, unsupported-access restrictions, output evaluation, least privilege, and human approval.

## Backend testing learning

### Unit testing

```text
Framework: xUnit
Tests: 58 passed
```

Coverage includes:

- Repository behavior
- Agent events
- Deployment tools
- Input policy
- Security headers
- Response evaluation
- Retry relationships

### API integration testing

```text
Framework: xUnit
Hosting: WebApplicationFactory<Program>
Tests: 25 passed
```

I learned to use:

```text
Microsoft.AspNetCore.Mvc.Testing
WebApplicationFactory
TestServer
Temporary SQLite databases
Mock agent injection
Real middleware
Real controllers
Real SSE endpoints
```

Integration-test parallelism is disabled to prevent shared rate-limit and database state from making tests unreliable.

## Frontend testing learning

### Component testing

```text
Framework: Vitest and Testing Library
Tests: 18 passed
```

Coverage includes:

- Form validation
- Busy states
- Markdown
- Evidence parsing
- Evidence badges
- Focus behavior
- Evidence cards

Vitest is restricted to `src/` so Playwright files are not accidentally collected.

### Browser E2E testing

```text
Framework: Playwright
Browser: Chromium
Workers: 1
Tests: 5 passed
```

The E2E suite validates:

- Successful investigation
- Invalid input
- Simulated failure
- Retry to attempt 2
- Evidence navigation
- History after reload

The browser suite starts the ASP.NET Core API and Vite automatically and uses an isolated SQLite database.

## SSE troubleshooting learning

Browser tests exposed differences that unit tests did not reveal:

- SSE streams can use CRLF line endings.
- A stream can close without a trailing blank line.
- Absolute API stream URLs can conflict with the browser origin.
- History UI state can be stale until explicitly refreshed.

I fixed these by:

- Normalizing CRLF to LF
- Processing a final buffered event
- Checking `text/event-stream`
- Routing stream requests through the Vite `/api` proxy
- Polling the persisted API status in browser tests
- Refreshing history before asserting final state

This demonstrated why browser-level automation is valuable even when unit and API integration tests already pass.

## Continuous integration learning

The project validates:

- .NET tool restore
- Solution restore
- Release build
- Unit tests
- Integration tests
- NuGet vulnerability audit
- EF migration visibility
- npm lockfile installation
- Frontend component tests
- Frontend production build
- Playwright browser tests
- Artifact creation

Mock mode keeps automation deterministic and credential-free.

## Current validated results

```text
Backend unit tests:          58 passed
Backend integration tests:   25 passed
Backend total:               83 passed
Frontend component tests:    18 passed
Playwright E2E tests:         5 passed
NuGet vulnerable packages:    0
npm production vulnerabilities: 0
Frontend build:              succeeded
```

## Architecture decisions

### Modular monolith

The backend remains a modular monolith because the domain is cohesive, one developer owns the application, and independent scaling is not yet required.

### One React application

The frontend remains one application because independent deployment is not currently justified.

### SQLite

SQLite supports simple local persistence and isolated integration and E2E testing. EF Core preserves a migration path to PostgreSQL.

### Server-Sent Events

SSE matches the server-to-browser streaming requirement.

### Deterministic automation

Mock mode provides repeatable tests without Foundry credentials, model variability, or external throttling.

## Challenges overcome

- Foundry integration
- Agent-selected tools
- Evidence rendering
- SQLite migrations
- DateTimeOffset ordering
- Build file locking
- Dependency vulnerabilities
- Deterministic response evaluation
- WebApplicationFactory configuration
- Isolated SQLite integration databases
- Test parallelism
- Vitest and Playwright discovery conflicts
- ES-module path resolution
- SSE CRLF parsing
- Same-origin stream routing
- Browser history synchronization
- E2E database isolation

## Planned improvements

### Enterprise identity

```text
Microsoft Entra ID
MSAL React
Microsoft.Identity.Web
RBAC
User-scoped history
```

### Deployment Evidence microservice

```text
AgentOpsNexus.DeploymentEvidence.Api
Typed HttpClient
Timeout
Retry with jitter
Circuit breaker
Contract tests
Distributed tracing
```

### Browser expansion

```text
Firefox
WebKit
Mobile browser projects
Visual regression
axe-core accessibility scans
```

### Production deployment

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

- Code splitting
- Lazy loading
- Bundle analysis
- Bundle budgets
- History pagination
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
- Failure and retry
- Observability
- Security controls
- Unit testing
- Integration testing
- Browser E2E testing
- Dependency management
- Git workflows
- GitHub Actions
- Documentation
- Manual demonstration

## Conclusion

AgentOps Nexus strengthened my practical skills in:

```text
React
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
WebApplicationFactory
Vitest
Testing Library
Playwright
Chromium automation
Git
GitHub Actions
Security
System design
```

The most significant lesson was that the model is only one component of a reliable AI application.

Reliable AI software depends on evidence, validation, bounded tools, persistence, observability, tests, security controls, user experience, and honest limitations.