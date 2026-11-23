# AgentOps Nexus AI Demo Guide

## 1. Demo purpose

This guide provides a repeatable demonstration of the complete AgentOps Nexus AI capstone.

The demonstration proves:

- ReactJS request interface
- ASP.NET Core API integration
- Microsoft Foundry GenAI integration
- Agent-selected tool invocation
- Server-Sent Events streaming
- Input validation
- Loading and response states
- Error handling
- Failure simulation
- Retry behavior
- Persistent request history
- Evidence citations and navigation
- Correlation IDs
- Security headers
- Rate limiting
- OpenTelemetry tracing and metrics
- Automated unit and component tests
- GitHub Actions CI

## 2. Recommended demo duration

```text
12 to 15 minutes
```

Suggested timing:

| Section | Duration |
|---|---:|
| Architecture and workspace | 2 minutes |
| Successful investigation | 4 minutes |
| Evidence navigation | 1 minute |
| Failure and retry | 2 minutes |
| Persistence and observability | 2 minutes |
| Tests, CI, and limitations | 3 minutes |

## 3. Demo environment

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
```

### Local URLs

```text
Frontend: http://localhost:5173
Backend:  http://localhost:7001
Health:   http://localhost:7001/health
```

## 4. Pre-demo checklist

Before presenting, confirm:

- [ ] Repository is on the expected branch
- [ ] Working tree is clean
- [ ] Backend builds
- [ ] Backend tests pass
- [ ] Frontend dependencies install
- [ ] Frontend tests pass
- [ ] Frontend production build passes
- [ ] Microsoft Foundry configuration exists in user secrets
- [ ] SQLite database migration applies
- [ ] Port 7001 is free
- [ ] Port 5173 is free
- [ ] Browser zoom is 100%
- [ ] No credentials are visible
- [ ] No personal browser tabs are visible
- [ ] GitHub Actions workflow is green

Verify Git:

```powershell
cd C:\Workspace\AgentOpsNexus

git status
git branch --show-current
git log -3 --oneline
```

Expected:

```text
On branch feature/milestone-7-documentation
nothing to commit, working tree clean
```

The exact branch may differ after Milestone 7 is merged.

## 5. Validate the solution before the demo

### Backend validation

Stop any running API process first:

```powershell
Get-Process AgentOpsNexus.AgentApi `
  -ErrorAction SilentlyContinue |
Stop-Process -Force
```

Run:

```powershell
cd C:\Workspace\AgentOpsNexus

dotnet tool restore
dotnet restore .\AgentOpsNexus.slnx
dotnet build .\AgentOpsNexus.slnx
dotnet test .\AgentOpsNexus.slnx --no-build
```

Expected result:

```text
Build succeeded
Total tests: 58
Passed: 58
Failed: 0
```

### NuGet vulnerability audit

```powershell
dotnet list `
  .\AgentOpsNexus.slnx `
  package `
  --vulnerable `
  --include-transitive
```

Expected result:

```text
No vulnerable NuGet packages
```

### Frontend validation

```powershell
cd C:\Workspace\AgentOpsNexus\frontend\shell

npm ci
npm audit --omit=dev
npm run test
npm run build
```

Expected result:

```text
Production dependency vulnerabilities: 0
Test files: 3 passed
Tests: 18 passed
Production build: succeeded
```

The frontend build may display a non-blocking bundle-size warning.

## 6. Configure the agent provider

### Real Microsoft Foundry demo

Use .NET user secrets:

```powershell
cd C:\Workspace\AgentOpsNexus\services\AgentOpsNexus.AgentApi

dotnet user-secrets set "Agent:Provider" "Foundry"
dotnet user-secrets set "Agent:ProjectEndpoint" "<your-foundry-project-endpoint>"
dotnet user-secrets set "Agent:ModelDeployment" "<your-model-deployment-name>"
dotnet user-secrets set "Agent:TenantId" "<your-entra-tenant-id>"
dotnet user-secrets set "Agent:TimeoutSeconds" "180"
```

Do not run or display this command during a public demo:

```powershell
dotnet user-secrets list
```

The output may reveal sensitive configuration.

### Deterministic offline demo

If Foundry is unavailable:

```powershell
dotnet user-secrets set "Agent:Provider" "Mock"
```

Mock mode is appropriate for:

- Automated testing
- Offline demonstrations
- Repeatable failure simulation
- CI
- Assessors without Foundry access

## 7. Start the application

### Terminal 1: backend

```powershell
cd C:\Workspace\AgentOpsNexus\services\AgentOpsNexus.AgentApi

dotnet run
```

Wait for output similar to:

```text
Now listening on: http://localhost:7001
Application started.
```

Keep the terminal running.

### Terminal 2: frontend

```powershell
cd C:\Workspace\AgentOpsNexus\frontend\shell

npm run dev
```

Wait for:

```text
Local: http://localhost:5173
```

Open the frontend in the browser.

Reset browser zoom:

```text
Ctrl + 0
```

## 8. Demonstration narrative

Use this short introduction:

> AgentOps Nexus AI is an evidence-grounded incident investigation workspace built with React, ASP.NET Core, Microsoft Foundry, Microsoft Agent Framework, EF Core, SQLite, and OpenTelemetry. The application streams AI investigations, allows the agent to invoke a bounded read-only deployment tool, presents traceable evidence IDs, persists investigation history, handles failures and retries, and evaluates responses against deterministic safety rules.

## 9. Demo 1: application workspace

Show:

- Product header
- New investigation form
- Incident-description field
- Environment selector
- Severity selector
- Simulated-failure option
- AI investigation panel
- Request-history section

Explain:

> The React application handles validation, asynchronous loading, streamed responses, error handling, retry, and persistent request history.

## 10. Demo 2: successful grounded investigation

### Incident prompt

```text
Checkout failures increased after the latest PaymentService deployment.
Investigate whether a recent deployment could be related.
```

### Configuration

```text
Environment: Development
Severity: High
Simulate AI service failure: unchecked
```

Select:

```text
Start investigation
```

### Expected frontend behavior

1. Submit button becomes disabled.
2. Loading or streaming state appears.
3. Progress messages appear.
4. Markdown content streams incrementally.
5. The response eventually shows Completed.
6. The request appears in history.

### Expected event sequence

```text
agent.started
agent.progress
agent.message.delta
agent.completed
```

Explain:

> React submits the request to ASP.NET Core. The API persists a queued record and returns HTTP 202. React then opens a Server-Sent Events stream. The agent streams progress and response fragments through the API.

## 11. Demo 3: agentic tool invocation

Observe the backend console.

Expected log meaning:

```text
GetRecentDeployments invoked
Service: PaymentService
Environment: Development
Lookback: 24 hours
```

Expected evidence:

```text
DEP-EV-001
DEP-EV-002
```

Explain:

> The model decides whether deployment evidence is relevant, but application code controls the tool. The model cannot register tools, provide arbitrary URLs, execute shell commands, or perform a rollback. The tool validates service name, environment, and lookback period before returning structured evidence.

## 12. Demo 4: grounded response

Show response sections such as:

```text
Summary
Known Facts
Deployment Evidence
Initial Assessment
Possible Hypotheses
Evidence Still Required
Recommended Next Steps
Risk and Approval Notes
Limitations
```

Highlight:

```text
DEP-EV-001
DEP-EV-002
```

Explain:

> The response distinguishes known facts from hypotheses. Deployment timing is treated as correlation rather than proof of causation. The response asks for logs, metrics, traces, and configuration comparisons instead of claiming that those systems were already examined.

## 13. Demo 5: evidence navigation

Locate a blue evidence badge:

```text
DEP-EV-001
```

Select the badge.

Verify:

- The application scrolls to the matching evidence card.
- The matching card receives keyboard focus.
- The card is labeled `Mock evidence`.
- The evidence-warning message is visible.

Keyboard check:

```text
Tab
Enter
Space
```

Explain:

> Evidence citations are not plain text. The frontend converts citations into accessible controls connected to evidence details. This gives reviewers a visible path from an AI claim to supporting evidence.

## 14. Demo 6: invalid client input

Enter:

```text
Failure
```

Attempt to submit.

Expected:

- Validation message appears.
- Request is not submitted.
- No history record is created.

Explain:

> Validation occurs in the React form, but the API also validates independently. Client-side validation improves user experience; server-side validation protects the boundary.

## 15. Demo 7: invalid API input

Use PowerShell to send a null byte:

```powershell
$body = @{
  message = "Invalid`0incident description"
  environment = "Development"
  severity = "High"
  simulateFailure = $false
} | ConvertTo-Json

try
{
    Invoke-WebRequest `
      -UseBasicParsing `
      -Method Post `
      -Uri "http://localhost:7001/api/v1/requests" `
      -ContentType "application/json" `
      -Body $body
}
catch
{
    $_.Exception.Response.StatusCode.value__
}
```

Expected:

```text
400
```

The response should use the error code:

```text
INCIDENT_DESCRIPTION_NULL_BYTE
```

Explain:

> The API rejects unsafe input before persistence or model execution.

## 16. Demo 8: service failure

Return to the frontend.

Select:

```text
Simulate AI service failure
```

Submit the standard PaymentService incident.

Expected:

```text
Status: Failed
Error code: AGENT_SERVICE_UNAVAILABLE
Retryable: true
Attempt: 1
```

The UI should display a retry option.

Explain:

> Failure simulation provides deterministic testing of an external AI outage without depending on an actual service failure.

## 17. Demo 9: retry behavior

Select:

```text
Retry
```

Expected behavior:

- A new request is created.
- Attempt number becomes 2.
- Failure simulation is disabled.
- Original failed request remains visible.
- New request stores the original request ID.
- Retry completes under normal conditions.

Expected history:

```text
Attempt 1: Failed
Attempt 2: Completed
```

Explain:

> Retry does not overwrite the failed record. A new linked attempt preserves auditability.

## 18. Demo 10: persistent request history

Ensure at least one completed or failed request is visible.

Stop only the API:

```text
Ctrl + C
```

Restart:

```powershell
cd C:\Workspace\AgentOpsNexus\services\AgentOpsNexus.AgentApi

dotnet run
```

Return to the browser.

Select:

```text
Refresh history
```

or refresh the page.

Expected:

- Existing records remain visible.
- Completed and failed statuses remain.
- Retry links remain.

Explain:

> Request history uses EF Core and SQLite. Records survive API restarts instead of existing only in process memory.

## 19. Demo 11: health and service information

Run:

```powershell
Invoke-RestMethod `
  -Uri "http://localhost:7001/health"
```

Expected fields:

```text
status: Healthy
agentProvider: Foundry or Mock
deploymentTool: GetRecentDeployments
persistence: SQLite
databaseAvailable: True
telemetry: OpenTelemetry
security: Enabled
```

## 20. Demo 12: generated correlation ID

```powershell
$response = Invoke-WebRequest `
  -UseBasicParsing `
  -Uri "http://localhost:7001/health"

$response.StatusCode
$response.Headers["X-Correlation-ID"]
```

Expected:

```text
200
<generated 32-character identifier>
```

Explain:

> Every request receives a correlation ID. The ID is also used in logs, traces, ProblemDetails, and persisted investigation metadata.

## 21. Demo 13: preserve a safe correlation ID

```powershell
$response = Invoke-WebRequest `
  -UseBasicParsing `
  -Uri "http://localhost:7001/health" `
  -Headers @{
    "X-Correlation-ID" = "agentops-demo-001"
  }

$response.Headers["X-Correlation-ID"]

$body = $response.Content |
  ConvertFrom-Json

$body.correlationId
```

Expected:

```text
agentops-demo-001
agentops-demo-001
```

## 22. Demo 14: reject unsafe correlation ID

```powershell
$response = Invoke-WebRequest `
  -UseBasicParsing `
  -Uri "http://localhost:7001/health" `
  -Headers @{
    "X-Correlation-ID" = "invalid correlation id!"
  }

$response.Headers["X-Correlation-ID"]
```

Expected:

```text
A new generated value
```

Explain:

> Unsafe correlation values are replaced to reduce control-character and log-injection risk.

## 23. Demo 15: security headers

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

Explain:

> The API returns JSON and SSE rather than HTML, so the API can use a restrictive content-security policy.

## 24. Demo 16: OpenTelemetry

Run a successful investigation and inspect the backend terminal.

Look for custom activities:

```text
agent.investigate
tool.GetRecentDeployments
```

Look for custom metrics:

```text
agentops.investigations
agentops.investigation.duration
agentops.tool.invocations
agentops.tool.duration
agentops.tool.evidence.count
```

Example local observation:

```text
Investigation count: 1
Investigation duration: approximately 28.5 seconds
Tool invocation count: 1
Tool duration: approximately 2.8 milliseconds
Evidence count: 2
```

Clarify:

> These numbers are sample local observations, not performance guarantees.

Explain:

> Custom telemetry records request and tool metadata, status, durations, and evidence counts. Custom telemetry does not intentionally include prompt text, generated response text, tokens, tenant IDs, or user-secret values.

## 25. Demo 17: deterministic response evaluation

Open:

```text
services/AgentOpsNexus.AgentApi/Evaluation/IncidentResponseEvaluator.cs
```

Show checks for:

```text
UNKNOWN_EVIDENCE_ID
MISSING_EVIDENCE_CITATION
MOCK_EVIDENCE_NOT_DISCLOSED
CAUSATION_OVERSTATED
ROLLBACK_APPROVAL_MISSING
ROLLBACK_EXECUTION_CLAIM
UNSUPPORTED_SYSTEM_ACCESS_CLAIM
POTENTIAL_SECRET_DISCLOSURE
SYSTEM_PROMPT_DISCLOSURE
```

Open tests:

```text
tests/AgentOpsNexus.AgentApi.UnitTests/IncidentResponseEvaluatorTests.cs
```

Explain:

> The evaluator is deterministic, fast, and CI-friendly. The evaluator does not replace human review, but the evaluator catches repeatable unsafe response patterns.

## 26. Demo 18: automated tests

### Backend

```powershell
cd C:\Workspace\AgentOpsNexus

dotnet test .\AgentOpsNexus.slnx --no-build
```

Expected:

```text
Total: 58
Failed: 0
Passed: 58
```

### Frontend

```powershell
cd C:\Workspace\AgentOpsNexus\frontend\shell

npm run test
```

Expected:

```text
Test Files: 3 passed
Tests: 18 passed
```

Explain:

> Backend tests cover repositories, agent behavior, deployment tools, input policy, evaluator rules, and security middleware. Frontend tests cover form validation, Markdown, evidence parsing, evidence badges, focus behavior, and evidence cards.

## 27. Demo 19: dependency audits

### NuGet

```powershell
cd C:\Workspace\AgentOpsNexus

dotnet list `
  .\AgentOpsNexus.slnx `
  package `
  --vulnerable `
  --include-transitive
```

Expected:

```text
No vulnerable NuGet packages
```

### Frontend production dependencies

```powershell
cd C:\Workspace\AgentOpsNexus\frontend\shell

npm audit --omit=dev
```

Expected:

```text
found 0 vulnerabilities
```

The complete development dependency audit may report the documented upstream `picomatch` advisory.

## 28. Demo 20: GitHub Actions

Open the repository’s **Actions** tab.

Show:

```text
AgentOps Nexus CI
```

Expected successful jobs:

```text
Backend build, tests, migrations, and audit
Frontend install, audit, tests, and build
CI result
```

Show artifacts:

```text
backend-test-results
frontend-production-build
```

Explain:

> CI does not require Foundry credentials. Automated tests use deterministic providers, while real Foundry behavior is demonstrated manually.

## 29. Architecture explanation for assessors

Use:

> The current backend is a modular monolith because one developer owns one cohesive domain and there is no demonstrated need for independent scaling. Interfaces are preserved so the Deployment Evidence boundary can be extracted later as a justified microservice. The React application is also cohesive today, while an Evidence Panel remote is planned as a focused microfrontend proof-of-concept.

## 30. Hiring-focused future architecture

Explain the planned progression:

```text
Milestone 8
API integration tests and Playwright E2E tests

Milestone 9
Microsoft Entra ID, MSAL, RBAC, and user-scoped history

Milestone 10
Deployment Evidence microservice with resilience and tracing

Milestone 11
Evidence microfrontend with runtime fallback

Milestone 12
Azure deployment with Bicep, PostgreSQL, Key Vault,
managed identity, Container Apps, Static Web Apps,
Application Insights, and API Management
```

## 31. Known limitations to state clearly

During the demonstration, state:

- Deployment evidence is deterministic mock data.
- The application has no end-user login yet.
- SQLite is for development and demonstration.
- Rate limiting is instance-local.
- Real Foundry output is non-deterministic.
- There is no automated Playwright suite yet.
- There is no separate API integration-test project yet.
- The application cannot perform rollback or production changes.
- Console telemetry is not a production backend.
- The frontend bundle still produces a size warning.
- The project is not claimed to be fully production-ready.

## 32. Individual learning summary

Use this closing statement:

> The capstone strengthened my practical skills in React, TypeScript, ASP.NET Core, C#, Entity Framework Core, Microsoft Foundry, Microsoft Agent Framework, Azure Identity, GenAI, agentic AI, OpenTelemetry, testing, security, Git, GitHub Actions, and system design. The main lesson was that a reliable AI application requires more than a model call. The surrounding application must provide evidence boundaries, validation, persistence, observability, failure handling, tests, security controls, and honest limitations.

## 33. Recommended screenshots

Save sanitized images under:

```text
docs/images/
```

Recommended files:

```text
successful-investigation.png
grounded-response.png
evidence-navigation.png
invalid-input.png
service-failure.png
retry-success.png
persisted-history.png
backend-tests.png
frontend-tests.png
github-actions.png
```

## 34. Screenshot safety

Before capturing:

- Reset browser zoom to 100%.
- Crop unrelated tabs.
- Hide account information.
- Hide emails and notifications.
- Do not display user-secret output.
- Do not display tenant IDs.
- Do not display Foundry endpoints.
- Do not display device codes.
- Do not display access tokens.
- Do not display authentication callback pages.

## 35. Demo recovery plan

### Foundry authentication fails

Switch to Mock:

```powershell
dotnet user-secrets set "Agent:Provider" "Mock"
```

Restart the API.

### Port 7001 is occupied

```powershell
Get-NetTCPConnection `
  -LocalPort 7001 `
  -ErrorAction SilentlyContinue |
Select-Object OwningProcess

Stop-Process `
  -Id <process-id> `
  -Force
```

### Port 5173 is occupied

Stop the existing Vite process or use the URL printed by Vite for the alternate port.

### Database migration fails

```powershell
cd C:\Workspace\AgentOpsNexus\services\AgentOpsNexus.AgentApi

dotnet tool restore
dotnet tool run dotnet-ef database update
```

### Frontend dependencies are missing

```powershell
cd C:\Workspace\AgentOpsNexus\frontend\shell

npm ci
```

### History cannot load

Check:

```powershell
Invoke-RestMethod `
  -Uri "http://localhost:7001/api/v1/requests"
```

Then inspect the API terminal.

## 36. Final demo checklist

- [ ] Repository state shown
- [ ] Backend tests shown
- [ ] Frontend tests shown
- [ ] Successful request shown
- [ ] Tool invocation shown
- [ ] Evidence citations shown
- [ ] Evidence navigation shown
- [ ] Invalid input shown
- [ ] Failure shown
- [ ] Retry shown
- [ ] Persistent history shown
- [ ] Correlation ID shown
- [ ] Security headers shown
- [ ] OpenTelemetry shown
- [ ] GitHub Actions shown
- [ ] Known limitations stated
- [ ] Future architecture explained
- [ ] Learning summary presented