# AgentOps Nexus AI Known Limitations

## Purpose

This document records the current limitations of AgentOps Nexus AI so the project does not overstate its capabilities, security, scale, or production readiness.

## Deployment evidence is demonstration data

The current Mock provider returns deterministic records such as:

```text
DEP-EV-001
DEP-EV-002
```

These records are not live data from Azure DevOps, GitHub Actions, Kubernetes, or Azure Container Apps.

### Planned improvement

Implement a bounded production evidence adapter with:

- Project and pipeline allowlists
- Managed identity
- Timeouts
- Cancellation
- Pagination
- Record limits
- Evidence normalization
- Audit logging

## No end-user authentication

The application does not authenticate application users.

The project does not currently provide:

- Per-user history
- Role-based access
- Tenant isolation
- Investigator roles
- Reader roles
- Approver roles
- Administrator roles

### Planned improvement

Use:

```text
@azure/msal-browser
@azure/msal-react
Microsoft.Identity.Web
JWT bearer validation
Microsoft Entra application roles
```

## No consequential actions

The application cannot:

- Roll back a deployment
- Restart a service
- Deploy code
- Modify configuration
- Change a database
- Change a feature flag
- Execute shell commands

This is intentional.

Future consequential tools require authorization, explicit approval, least privilege, auditing, idempotency, and post-action verification.

## SQLite is a development provider

SQLite is appropriate for:

- Local development
- Demonstrations
- Integration tests
- Browser E2E tests
- Low-volume single-instance execution

SQLite is not the planned provider for a horizontally scaled production system.

### Planned improvement

Use Azure Database for PostgreSQL with encrypted connections, backups, controlled migrations, paging, and resiliency.

## Rate limiting is instance-local

Current counters are maintained by one API process.

### Planned improvement

Use Azure API Management policies or a Redis-backed distributed rate limiter.

## Browser coverage is Chromium-only

The Playwright suite currently uses:

```text
Browser: Chromium
Workers: 1
Tests: 5
```

The suite does not yet cover:

- Firefox
- WebKit
- Mobile browser profiles
- Tablet profiles
- Visual-regression baselines
- Automated accessibility scanning

### Planned improvement

Add:

```text
Desktop Chrome
Desktop Firefox
Desktop Safari
Mobile Chrome
Mobile Safari
@axe-core/playwright
```

## E2E tests use fixed local ports

Playwright starts:

```text
API: 7001
Frontend: 5173
```

Ports must be available.

Each E2E process uses an isolated SQLite database, but the fixed ports prevent multiple local E2E suites from running concurrently.

## E2E databases require cleanup

Playwright database files are created under:

```text
services/AgentOpsNexus.AgentApi/App_Data/
```

The directory is ignored by Git. Old E2E databases can be removed after test completion.

## Real Foundry output is non-deterministic

Real model output can vary in wording, ordering, length, latency, and tool timing.

CI therefore uses the deterministic Mock provider.

## Interactive browser credential is local-only

Local Foundry development uses interactive authentication.

Production should use:

```text
ManagedIdentityCredential
```

## OpenTelemetry exports locally

The development application uses console-based telemetry.

A production deployment should use Azure Monitor or Application Insights with data classification, sampling, retention, dashboards, and alerts.

## Deterministic evaluation is rule-based

The evaluator is fast and CI-friendly but cannot understand every semantic variation.

A production system should combine:

- Deterministic rules
- Evidence graph validation
- Optional secondary model evaluation
- Human review

## No real operational-data tools

The agent cannot currently query:

- Production logs
- Metrics
- Distributed traces
- Operational databases
- Source repositories
- Runbooks
- ServiceNow
- Azure Monitor
- Application Insights

The response must state these limitations rather than claim unsupported access.

## Backend remains a modular monolith

The backend is not a microservice system.

This is deliberate because one developer owns one cohesive domain and independent scaling is not currently required.

A future Deployment Evidence API may be extracted when ownership, scaling, or credential isolation justifies the boundary.

## Frontend remains one React application

The frontend is componentized but is not a microfrontend runtime.

A future evidence remote may demonstrate independently deployed UI composition.

## Frontend bundle-size warning

The production build is approximately:

```text
827 KB JavaScript minified
239 KB gzip
```

The build succeeds, but Vite reports a warning above 500 KB.

### Planned improvement

Use:

- Dynamic imports
- `React.lazy`
- `Suspense`
- Route-level splitting
- Lazy Markdown loading
- Bundle visualization
- CI bundle budgets

The warning should not be hidden only by increasing the threshold.

## Request history is not paginated

The API currently returns the complete local history.

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

## No formal load testing

The application has not been tested under sustained concurrent load.

Unmeasured areas include:

- Concurrent SSE streams
- SQLite contention
- Foundry throttling
- Memory used by streamed responses
- Retry amplification
- Large histories

### Planned improvement

Use k6, NBomber, or Azure Load Testing.

## No penetration test or certification

The project has not undergone:

- Professional penetration testing
- OWASP ASVS assessment
- SOC 2 assessment
- ISO 27001 assessment
- PCI DSS assessment
- Privacy-impact assessment

No certification is claimed.

## No production cloud deployment

The application currently runs locally.

The planned Azure architecture includes:

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

## No automated approval workflow

The response requires human approval for consequential actions, but the application does not persist approval requests or decisions.

### Planned improvement

Add:

```text
ActionRecommendation
ApprovalRequest
ApprovalDecision
ExecutionRecord
VerificationResult
```

## Production-readiness statement

The correct description is:

> A secure, evidence-grounded, assessment-ready capstone and production-oriented reference implementation.

The project should not yet be described as a fully production-ready incident-management platform.