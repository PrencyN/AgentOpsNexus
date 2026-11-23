# AgentOps Nexus AI Demo Guide

## Purpose

This guide provides a repeatable demonstration of AgentOps Nexus AI.

The demonstration proves:

- React request interface
- ASP.NET Core integration
- Microsoft Foundry and Mock providers
- Agent-selected tool invocation
- Server-Sent Events
- Input validation
- Error handling
- Failure simulation
- Retry behavior
- SQLite persistence
- Evidence navigation
- Correlation IDs
- Security headers
- OpenTelemetry
- Backend unit tests
- API integration tests
- Frontend component tests
- Playwright browser E2E tests
- Dependency audits
- GitHub Actions CI

## Recommended duration

```text
12 to 15 minutes
```

| Section | Duration |
|---|---:|
| Architecture | 2 minutes |
| Successful investigation | 4 minutes |
| Evidence navigation | 1 minute |
| Failure and retry | 2 minutes |
| Persistence and observability | 2 minutes |
| Tests, CI, and limitations | 3 minutes |

## Environment

### Backend

```text
.NET 10
ASP.NET Core
Microsoft Foundry
Microsoft Agent Framework
EF Core
SQLite
OpenTelemetry
```

### Frontend

```text
React 19
TypeScript
Vite
Fluent UI
React Markdown
Vitest
Testing Library
Playwright
Chromium
```

### Local addresses

```text
Frontend: http://localhost:5173
Backend:  http://localhost:7001
Health:   http://localhost:7001/health
```

## Pre-demo checklist

- [ ] Working tree is clean
- [ ] Expected branch is checked out
- [ ] Backend builds
- [ ] Backend unit tests pass
- [ ] Backend integration tests pass
- [ ] Frontend component tests pass
- [ ] Playwright tests pass
- [ ] Frontend production build passes
- [ ] NuGet audit is clean
- [ ] npm production audit is clean
- [ ] Foundry or Mock provider is configured
- [ ] Ports 7001 and 5173 are free
- [ ] Browser zoom is 100%
- [ ] Credentials are hidden
- [ ] GitHub Actions is green

## Validate before the demo

### Backend

```powershell
cd C:\Workspace\AgentOpsNexus

dotnet tool restore
dotnet restore .\AgentOpsNexus.slnx
dotnet build .\AgentOpsNexus.slnx
dotnet test .\AgentOpsNexus.slnx --no-build
```

Expected:

```text
Backend unit tests: 58 passed
Backend integration tests: 25 passed
Backend total: 83 passed
Failed: 0
```

### NuGet audit

```powershell
dotnet list `
  .\AgentOpsNexus.slnx `
  package `
  --vulnerable `
  --include-transitive
```

Expected:

```text
No vulnerable packages
```

### Frontend

```powershell
cd C:\Workspace\AgentOpsNexus\frontend\shell

npm ci
npm run test
npm run build
npm run test:e2e
npm audit --omit=dev
```

Expected:

```text
Component tests: 18 passed
Playwright E2E tests: 5 passed
Production build: succeeded
Production vulnerabilities: 0
```

## Configure the provider

### Real Foundry demo

```powershell
cd C:\Workspace\AgentOpsNexus\services\AgentOpsNexus.AgentApi

dotnet user-secrets set "Agent:Provider" "Foundry"
dotnet user-secrets set "Agent:ProjectEndpoint" "<project-endpoint>"
dotnet user-secrets set "Agent:ModelDeployment" "<deployment-name>"
dotnet user-secrets set "Agent:TenantId" "<tenant-id>"
dotnet user-secrets set "Agent:TimeoutSeconds" "180"
```

Do not display `dotnet user-secrets list`.

### Deterministic Mock demo

```powershell
dotnet user-secrets set "Agent:Provider" "Mock"
```

Use Mock mode for:

- Automated tests
- Offline demonstrations
- Repeatable failure simulation
- Assessors without Foundry access

## Start the application

### Terminal 1

```powershell
cd C:\Workspace\AgentOpsNexus\services\AgentOpsNexus.AgentApi

dotnet run
```

### Terminal 2

```powershell
cd C:\Workspace\AgentOpsNexus\frontend\shell

npm run dev
```

Open the frontend and press:

```text
Ctrl + 0
```

## Introduction

> AgentOps Nexus AI is an evidence-grounded incident investigation workspace built with React, ASP.NET Core, Microsoft Foundry, Microsoft Agent Framework, EF Core, SQLite, OpenTelemetry, and Playwright. The application streams AI investigations, invokes a bounded read-only deployment tool, presents traceable evidence IDs, persists investigation history, handles failures and retries, and evaluates responses against deterministic safety rules.

## Successful investigation

Use:

```text
Checkout failures increased after the latest PaymentService deployment.
Investigate whether a recent deployment could be related.
```

Configuration:

```text
Environment: Development
Severity: High
Simulate AI service failure: unchecked
```

Expected behavior:

1. The submit button is disabled.
2. Streaming progress appears.
3. Markdown arrives incrementally.
4. The response completes.
5. History shows `Completed`.
6. Attempt 1 is visible.

Expected stream events:

```text
agent.started
agent.progress
agent.message.delta
agent.completed
```

## Evidence demonstration

Expected Mock evidence:

```text
DEP-EV-001
DEP-EV-002
```

Select the `DEP-EV-001` evidence control.

Verify:

- The matching evidence card becomes visible.
- Keyboard focus moves to the card.
- The card says `Mock deployment evidence`.
- Trust level says `Demonstration only`.
- The warning requires production verification.

Explain:

> Evidence citations are interactive controls rather than untraceable text. The UI provides a visible path from an AI claim to the evidence record used by the response.

## Invalid input

Enter:

```text
Failure
```

Expected:

- Minimum-length validation appears.
- No request is submitted.
- No history record is created.

## Service failure

Enable:

```text
Simulate AI service failure
```

Submit the standard incident.

Expected:

```text
Status: Failed
Error code: AGENT_SERVICE_UNAVAILABLE
Retryable: true
Attempt: 1
```

The backend log contains an expected simulated `HttpRequestException`.

## Retry

Select:

```text
Retry
```

Expected:

```text
Attempt 1: Failed
Attempt 2: Completed
```

Explain:

> Retry creates a linked second record instead of overwriting the original failure.

## Persistence

After a completed request:

1. Refresh the browser page.
2. Confirm the history record remains.
3. Confirm the status remains `Completed`.

Explain:

> Request history uses EF Core and SQLite, so records survive browser reloads and API restarts.

## Health

```powershell
Invoke-RestMethod `
  -Uri "http://localhost:7001/health"
```

Expected fields include:

```text
status
agentProvider
deploymentTool
persistence
databaseAvailable
telemetry
security
correlationId
```

## Correlation ID

```powershell
$response = Invoke-WebRequest `
  -UseBasicParsing `
  -Uri "http://localhost:7001/health"

$response.Headers["X-Correlation-ID"]
```

Safe caller value:

```powershell
$response = Invoke-WebRequest `
  -UseBasicParsing `
  -Uri "http://localhost:7001/health" `
  -Headers @{
    "X-Correlation-ID" = "agentops-demo-001"
  }

$response.Headers["X-Correlation-ID"]
```

Expected:

```text
agentops-demo-001
```

Unsafe value:

```text
invalid correlation id!
```

Expected: the API replaces it with a generated safe value.

## Security headers

```powershell
$response = Invoke-WebRequest `
  -UseBasicParsing `
  -Uri "http://localhost:7001/health"

$response.Headers["X-Content-Type-Options"]
$response.Headers["Referrer-Policy"]
$response.Headers["X-Frame-Options"]
$response.Headers["Content-Security-Policy"]
$response.Headers["Permissions-Policy"]
$response.Headers["Cache-Control"]
```

Expected values include:

```text
nosniff
no-referrer
DENY
default-src 'none'
camera=()
microphone=()
no-store
```

## OpenTelemetry

Look for:

```text
agent.investigate
tool.GetRecentDeployments
```

Metrics include:

```text
agentops.investigations
agentops.investigation.failures
agentops.investigation.duration
agentops.tool.invocations
agentops.tool.failures
agentops.tool.duration
agentops.tool.evidence.count
```

## Automated tests

### Backend unit tests

```powershell
dotnet test `
  .\tests\AgentOpsNexus.AgentApi.UnitTests\AgentOpsNexus.AgentApi.UnitTests.csproj
```

Expected:

```text
58 passed
```

### Backend integration tests

```powershell
dotnet test `
  .\tests\AgentOpsNexus.AgentApi.IntegrationTests\AgentOpsNexus.AgentApi.IntegrationTests.csproj
```

Expected:

```text
25 passed
```

The suite validates the real middleware, controllers, SQLite persistence, SSE streams, headers, correlation IDs, rate limiting, history, failure, and retry.

### Frontend component tests

```powershell
cd C:\Workspace\AgentOpsNexus\frontend\shell

npm run test
```

Expected:

```text
18 passed
```

### Browser E2E

```powershell
npm run test:e2e
```

Expected:

```text
5 passed
```

Playwright validates:

- Successful completion
- Invalid input
- Failure and retry
- Evidence navigation
- Persistent history after reload

## GitHub Actions

Show:

```text
AgentOps Nexus CI
```

Explain:

> CI uses deterministic providers and does not require Foundry credentials. Real Foundry behavior is demonstrated manually.

## Known limitations

State clearly:

- Deployment evidence is deterministic demonstration data.
- End-user authentication is not implemented.
- SQLite is a local provider.
- Rate limiting is instance-local.
- Real Foundry output is non-deterministic.
- Playwright currently covers Chromium only.
- No Firefox or WebKit automation exists.
- No visual-regression baseline exists.
- No automated accessibility scan exists.
- No consequential actions are available.
- Console telemetry is not a production backend.
- The frontend bundle has a non-blocking size warning.
- The project is not fully production-ready.

## Closing statement

> The capstone strengthened my practical skills in React, TypeScript, ASP.NET Core, C#, EF Core, SQLite, Microsoft Foundry, Microsoft Agent Framework, agentic AI, OpenTelemetry, xUnit, API integration testing, Playwright, security, Git, and GitHub Actions. The main lesson was that a reliable AI application needs evidence boundaries, validation, persistence, observability, failure handling, security, and automated tests around the model.