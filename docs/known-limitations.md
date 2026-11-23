# AgentOps Nexus AI Known Limitations

## Purpose

This document records the current limitations of AgentOps Nexus AI so that the project does not overstate its capabilities, scale, security, or production readiness.

## 1. Deployment evidence is demonstration data

The current application uses:

```text
MockDeploymentEvidenceService
```

The records are deterministic mock data, not live deployment records from:

- Azure DevOps
- GitHub Actions
- Kubernetes
- Azure Container Apps
- Production release pipelines

### Planned improvement

Implement:

```text
AzureDevOpsDeploymentEvidenceService
```

The real adapter should use project and pipeline allowlists, managed identity or Key Vault credentials, timeouts, cancellation, record limits, pagination, and normalized evidence models.

The Mock provider should remain for tests, CI, and offline demonstrations.

## 2. No end-user authentication

The web application does not currently authenticate users.

The project therefore does not provide:

- Per-user history
- Role-based access
- Tenant isolation
- Investigator roles
- Reader roles
- Approver roles
- Administrator roles

Interactive browser authentication is used only to access Foundry during local development.

### Planned improvement

Use:

```text
@azure/msal-browser
@azure/msal-react
Microsoft.Identity.Web
JWT bearer authentication
Microsoft Entra application roles
```

Recommended roles:

```text
Incident.Investigator
Incident.Reader
Incident.Approver
AgentOps.Admin
```

A development mode should remain available for assessors without access to the owner’s tenant.

## 3. No consequential actions are performed

The current application cannot:

- Roll back a deployment
- Restart a service
- Deploy code
- Modify configuration
- Update a database
- Change a feature flag
- Notify stakeholders
- Execute shell commands

This is intentional.

Consequential actions require:

- Strong authorization
- Explicit human approval
- Least-privileged identity
- Environment restrictions
- Change-management linkage
- Idempotency
- Audit history
- Post-action verification
- Compensation strategy

## 4. SQLite is a development provider

The application uses EF Core and SQLite for local persistence.

SQLite is suitable for:

- Local development
- Demonstrations
- Automated testing
- Low-volume single-instance use

SQLite is not the planned production provider for a horizontally scaled multi-user application.

### Planned improvement

Use:

```text
Azure Database for PostgreSQL
Npgsql.EntityFrameworkCore.PostgreSQL
```

Production requirements include encrypted connections, backups, retention, connection resiliency, controlled migrations, monitoring, and pagination.

## 5. Rate limiting is instance-local

Current policies:

```text
20 investigation creations per minute per client
5 concurrent streams per client
```

Each API process maintains its own counters.

### Planned improvement

Use:

```text
Azure API Management rate limiting
or
Redis-backed distributed rate limiting
```

Forwarded headers must be configured safely before trusting proxy-provided client addresses.

## 6. No separate automated integration-test project

The backend has extensive xUnit tests, but no separate project currently uses:

```text
Microsoft.AspNetCore.Mvc.Testing
WebApplicationFactory<Program>
```

### Planned improvement

Create:

```text
tests/AgentOpsNexus.AgentApi.IntegrationTests
```

The project will use:

- Temporary SQLite
- Mock provider
- Real middleware
- Real controllers
- Real persistence
- Real security headers
- Real rate limiter
- Real SSE endpoint

## 7. No automated browser E2E suite

The frontend has component tests with Vitest and Testing Library.

The project does not currently include Playwright, Cypress, or Selenium automation.

### Planned improvement

Add Playwright for:

- Successful investigations
- Invalid inputs
- Failure and retry
- Evidence navigation
- Persistent history
- Keyboard navigation
- Responsive layout

CI will run E2E tests with the Mock provider.

## 8. Foundry output is non-deterministic

Real model responses can vary in:

- Wording
- Section order
- Response length
- Tool timing
- Latency
- Token usage

CI therefore does not depend on real Foundry calls.

The Mock provider supports repeatable and credential-free tests.

## 9. Interactive browser credential is local-only

The current Foundry implementation uses:

```text
InteractiveBrowserCredential
```

This is suitable for local development but not for an unattended production API.

### Planned improvement

Use:

```text
ManagedIdentityCredential
```

## 10. OpenTelemetry exports to the console

The local application demonstrates tracing and metrics through the console exporter.

The console is not a production telemetry backend.

### Planned improvement

Use:

```text
Azure Monitor OpenTelemetry
Application Insights
```

Add dashboards and alerts for:

- P50 and P95 latency
- Failure rate
- Tool availability
- Retry rate
- Evidence count
- Evaluation pass rate

## 11. Standard HTTP telemetry may contain hostnames

Custom telemetry avoids prompt text, response text, credentials, tenant IDs, and user secrets.

Standard .NET HTTP instrumentation may still include destination hostnames.

Production telemetry configuration requires a data-classification, sampling, retention, and exporter review.

## 12. Deterministic evaluation is rule-based

The evaluator is repeatable, fast, and CI-friendly.

However, pattern-based checks can produce false positives or false negatives and cannot understand every semantic variation.

### Planned improvement

Combine:

```text
Deterministic rules
Evidence graph validation
Optional model-based evaluation
Human review
```

The evaluator must not become the only control for consequential decisions.

## 13. No real operational-data tools

The agent cannot currently query:

- Logs
- Metrics
- Distributed traces
- Operational databases
- Source repositories
- Runbooks
- ServiceNow
- Azure Monitor
- Application Insights

The prompt and evaluator prevent the model from claiming that unsupported systems were examined.

### Planned improvement

Add bounded read-only tools with allowlists, authorization, result limits, cancellation, evidence IDs, audit logging, and data-classification controls.

## 14. Backend remains a modular monolith

The backend is not currently a microservice system.

This is deliberate because one developer owns a cohesive domain and independent service scaling is not currently required.

### Planned improvement

Extract:

```text
AgentOpsNexus.DeploymentEvidence.Api
```

This service can own external deployment credentials, resilience, caching, versioned contracts, and independent deployment.

## 15. Frontend remains one React application

The frontend is componentized but not a microfrontend runtime.

### Planned improvement

Create a focused proof:

```text
frontend/evidence-remote
```

The remote may expose:

```text
EvidenceBadge
EvidencePanel
```

The shell must provide a local fallback if the remote is unavailable.

## 16. Frontend bundle-size warning

Current output is approximately:

```text
826 KB JavaScript minified
238 KB gzip
```

The build succeeds, but Vite reports a size warning.

### Planned improvement

Implement:

- `React.lazy`
- `Suspense`
- Dynamic imports
- Route-level splitting
- Bundle visualizer
- CI bundle budgets
- Lazy Markdown loading
- Lazy evidence loading
- Lazy history loading

The warning should not be hidden only by increasing the configured threshold.

## 17. Request history is not paginated

The current API returns the complete history dataset.

This is acceptable for the capstone’s small local dataset but will not scale to thousands of records.

### Planned improvement

Add:

```text
page
pageSize
status
environment
createdFrom
createdTo
createdBy
```

Suggested defaults:

```text
Default page size: 25
Maximum page size: 100
```

## 18. No formal load testing

The project has not been validated under sustained concurrency.

Unmeasured areas include:

- Concurrent SSE streams
- Database contention
- Foundry throttling
- Memory used by long responses
- Tool latency
- Retry amplification
- Large request histories

### Planned improvement

Use:

```text
k6
NBomber
Azure Load Testing
```

Measure P50, P95, P99, error rate, CPU, memory, stream count, database latency, and model throttling.

## 19. No penetration test or certification

The project has not undergone:

- Professional penetration testing
- OWASP ASVS review
- SOC 2 assessment
- ISO 27001 assessment
- PCI DSS assessment
- Privacy-impact assessment

No certification is claimed.

## 20. Development dependency advisory

Production frontend dependencies currently audit cleanly.

The complete development dependency tree reports an upstream advisory for:

```text
picomatch 4.0.7
```

The package is introduced through the Vite and Vitest development toolchain.

At the time of documentation, no patched upstream release is available.

### Current mitigation

- Production dependency audit passes.
- The dependency is limited to development tooling.
- The lockfile is committed.
- CI uses `npm ci`.
- The full audit remains visible.
- The dependency will be updated when a patched release is available.

## 21. No production cloud deployment

The application currently runs locally.

It is not yet deployed to:

- Azure Static Web Apps
- Azure Container Apps
- Azure Database for PostgreSQL
- Azure Key Vault
- Application Insights
- Azure API Management

### Planned architecture

```text
React: Azure Static Web Apps
APIs: Azure Container Apps
Database: Azure Database for PostgreSQL
Secrets: Azure Key Vault
Identity: Managed Identity
Telemetry: Application Insights
Gateway: Azure API Management
Infrastructure: Bicep
```

## 22. No automated approval workflow

The response states that human approval is required, but the application does not yet persist approval requests or decisions.

### Planned improvement

Add:

```text
ActionRecommendation
ApprovalRequest
ApprovalDecision
ExecutionRecord
VerificationResult
```

Approvals must be explicit, role-protected, time-limited, auditable, bound to one action, and verified after execution.

## Production-readiness statement

The correct description of the current system is:

> A secure, evidence-grounded, assessment-ready capstone and production-oriented reference implementation.

The project should not yet be described as a fully production-ready incident-management platform.