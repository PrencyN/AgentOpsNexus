# AgentOps Nexus AI Test Evidence

## Purpose

This document records the automated and manual evidence used to validate AgentOps Nexus AI.

The capstone validation covers:

- Successful requests
- Invalid inputs
- AI service failures
- Retry behavior
- Persistent request history
- React-to-ASP.NET Core integration
- ASP.NET Core-to-agent integration
- Evidence citations
- Security and observability controls

## Current automated results

### Backend

```text
Framework: xUnit
Test project: AgentOpsNexus.AgentApi.UnitTests
Total tests: 58
Passed: 58
Failed: 0
Skipped: 0
```

### Frontend

```text
Framework: Vitest and Testing Library
Test files: 3
Total tests: 18
Passed: 18
Failed: 0
```

### Dependency audits

```text
NuGet vulnerable packages: 0
Frontend production dependency vulnerabilities: 0
```

### Continuous integration

```text
Workflow: AgentOps Nexus CI
Backend job: Passed
Frontend job: Passed
Final CI result: Passed
```

## Test layers

| Layer | Technology | Status |
|---|---|---:|
| Domain and repository tests | xUnit | Implemented |
| Agent tests | xUnit | Implemented |
| Deployment-tool tests | xUnit | Implemented |
| Input-security tests | xUnit | Implemented |
| Evaluator tests | xUnit | Implemented |
| Security-header tests | xUnit | Implemented |
| React component tests | Vitest and Testing Library | Implemented |
| API integration tests | WebApplicationFactory | Planned |
| Browser E2E tests | Playwright | Planned |
| Live Foundry validation | Manual demonstration | Implemented |
| CI validation | GitHub Actions | Implemented |

## Backend test coverage

### Repository behavior

Validated behavior includes:

- Add and retrieve a request
- Reject duplicate request IDs
- Return null for an unknown request
- Return newest records first
- Persist domain mutations
- Persist completed status
- Persist failed status
- Persist cancelled status
- Preserve retry state
- Preserve correlation ID
- Honor cancellation tokens

### Mock incident agent

Validated behavior includes:

- `agent.started`
- `agent.progress`
- `agent.message.delta`
- `agent.completed`
- Simulated failure
- Cancellation
- Event timestamps
- Response generation

### Deployment tool

Validated behavior includes:

- PaymentService evidence lookup
- CheckoutService evidence lookup
- Production environment filtering
- Evidence field serialization
- Input trimming
- Case-insensitive matching
- Invalid environment rejection
- Invalid lookback rejection
- Invalid service-name rejection
- Excessive service-name rejection
- Empty evidence response
- Mock-data disclosure
- Cancellation propagation
- Metrics-compatible dependency construction

### Incident input policy

Validated behavior includes:

- Valid input acceptance
- Trimming
- Required input
- Minimum length
- Maximum length
- Null-byte rejection
- Unsafe control-character rejection
- Newline and tab acceptance
- Windows line-ending normalization
- Excessive blank-line rejection
- Prompt-injection text preserved as untrusted content

### Deterministic response evaluator

Validated behavior includes:

- Safe grounded response acceptance
- Unknown evidence-ID rejection
- Missing citation detection
- Missing mock-evidence disclosure
- Causation overstatement detection
- Rollback approval enforcement
- Executed rollback-claim detection
- Unsupported system-access detection
- Potential secret-disclosure detection
- System-prompt disclosure detection
- Empty-response detection
- Evidence-ID normalization and deduplication

### Security headers

Validated headers include:

```text
X-Content-Type-Options
Referrer-Policy
X-Frame-Options
Content-Security-Policy
Permissions-Policy
Cache-Control
```

The middleware tests also confirm:

- The next middleware executes.
- Existing response headers are not overwritten.

## Frontend test coverage

### Request form

Validated behavior includes:

- Short-description validation
- Valid submission
- Busy-state disabling

### Markdown renderer

Validated behavior includes:

- Headings
- Lists
- Evidence citation conversion
- Bracketed evidence IDs
- Unbracketed evidence IDs
- Empty responses
- Partial streaming Markdown
- Evidence badge activation
- Evidence-card focus

### Evidence parser and panel

Validated behavior includes:

- Unique evidence extraction
- Empty-input handling
- Markdown evidence-link creation
- Evidence URL recognition
- Unrelated text preservation
- Multiple evidence cards
- Empty evidence panel
- Mock-evidence labels
- Demonstration-data warning

## Manual end-to-end matrix

| Scenario | Expected result | Status |
|---|---|---:|
| Valid investigation | HTTP 202, stream, completed response | Passed |
| Short input | Client validation | Passed |
| Null-byte input | HTTP 400 | Passed |
| AI service failure | Safe error and retry control | Passed |
| Retry | Attempt 2 linked to attempt 1 | Passed |
| Request history | History survives API restart | Passed |
| Evidence citation | Evidence IDs shown as badges | Passed |
| Evidence navigation | Matching evidence card receives focus | Passed |
| Correlation ID | Header and body values match | Passed |
| Invalid correlation ID | Unsafe input replaced | Passed |
| Security headers | Required headers present | Passed |
| Foundry execution | Real model response streamed | Passed |
| Tool invocation | Deployment tool invoked | Passed |
| Persistence | SQLite history retained | Passed |
| OpenTelemetry | Agent and tool metrics emitted | Passed |

## Successful investigation evidence

### Input

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

### Expected event sequence

```text
agent.started
agent.progress
agent.message.delta
agent.completed
```

### Expected evidence

```text
DEP-EV-001
DEP-EV-002
```

### Expected persisted state

```text
Status: Completed
Retryable: false
Attempt number: 1
Response: populated
CreatedAt: populated
CompletedAt: populated
```

## Invalid-input evidence

### Client-side validation

Input:

```text
Failure
```

Expected:

```text
Validation message displayed
No request submitted
```

### API null-byte validation

```powershell
$body = @{
  message = "Invalid`0incident description"
  environment = "Development"
  severity = "High"
  simulateFailure = $false
} | ConvertTo-Json
```

Expected:

```text
HTTP 400
INCIDENT_DESCRIPTION_NULL_BYTE
```

## Failure and retry evidence

### Failure

```text
Status: Failed
Error code: AGENT_SERVICE_UNAVAILABLE
Retryable: true
Attempt number: 1
```

### Retry

```text
Status: Completed
Attempt number: 2
RetriedFromRequestId: original request ID
```

The original failed attempt remains available.

## Persistence evidence

Persistence was verified using this process:

1. Submit an investigation.
2. Wait for completion or failure.
3. Confirm the record appears in history.
4. Stop the API.
5. Restart the API.
6. Refresh the browser.
7. Confirm that the record remains visible.

Health output confirmed:

```text
status: Healthy
persistence: SQLite
databaseAvailable: True
```

## Correlation-ID evidence

### Generated ID

```powershell
$response = Invoke-WebRequest `
  -UseBasicParsing `
  -Uri "http://localhost:7001/health"

$response.Headers["X-Correlation-ID"]
```

Example result:

```text
cebfce453ebb4c37a8dae83eaee7e545
```

### Preserved caller ID

```powershell
$response = Invoke-WebRequest `
  -UseBasicParsing `
  -Uri "http://localhost:7001/health" `
  -Headers @{
    "X-Correlation-ID" = "agentops-test-001"
  }

$response.Headers["X-Correlation-ID"]
```

Expected:

```text
agentops-test-001
```

### Invalid ID replacement

Input:

```text
invalid correlation id!
```

Example generated result:

```text
52a75dda20b74b17a21ea7bc54e94d1f
```

## OpenTelemetry evidence

Observed agent metrics:

```text
Metric: agentops.investigations
Provider: MicrosoftFoundry
Status: completed
Count: 1
```

```text
Metric: agentops.investigation.duration
Observed duration: approximately 28,521 ms
```

Observed tool metrics:

```text
Metric: agentops.tool.invocations
Tool: GetRecentDeployments
Status: completed
Count: 1
```

```text
Metric: agentops.tool.duration
Observed duration: approximately 2.76 ms
```

```text
Metric: agentops.tool.evidence.count
Evidence count: 2
```

These figures are sample local observations and are not performance guarantees.

## Security-header evidence

Validated values include:

```text
X-Content-Type-Options: nosniff
Referrer-Policy: no-referrer
X-Frame-Options: DENY
Content-Security-Policy: default-src 'none'; ...
Permissions-Policy: camera=(), microphone=(), ...
Cache-Control: no-store, no-cache, must-revalidate
X-Correlation-ID: generated or preserved value
```

## GitHub Actions evidence

The CI workflow validates:

### Backend

```text
Repository-local .NET tool restore
Solution restore
Release build
xUnit tests
NuGet vulnerability audit
EF Core migration verification
Test-result artifact upload
```

### Frontend

```text
Node.js setup
Lockfile validation
npm ci
Production dependency audit
Development dependency report
Vitest tests
Vite production build
Build artifact upload
```

### Final gate

```text
Backend result: success
Frontend result: success
AgentOps Nexus CI completed successfully
```

## Planned screenshots

The following files will be captured with credentials and personal information removed:

```text
docs/images/successful-investigation.png
docs/images/invalid-input.png
docs/images/service-failure.png
docs/images/retry-success.png
docs/images/evidence-navigation.png
docs/images/persisted-history.png
docs/images/backend-tests.png
docs/images/frontend-tests.png
docs/images/github-actions.png
```

Until the files are present, the screenshots are planned evidence rather than completed evidence.

## Final validation commands

### Backend

```powershell
cd C:\Workspace\AgentOpsNexus

dotnet restore .\AgentOpsNexus.slnx
dotnet build .\AgentOpsNexus.slnx
dotnet test .\AgentOpsNexus.slnx --no-build
```

### NuGet audit

```powershell
dotnet list `
  .\AgentOpsNexus.slnx `
  package `
  --vulnerable `
  --include-transitive
```

### Frontend

```powershell
cd C:\Workspace\AgentOpsNexus\frontend\shell

npm ci
npm audit --omit=dev
npm run test
npm run build
```

## Planned automated E2E evidence

The next milestone will add:

```text
ASP.NET Core WebApplicationFactory integration tests
Temporary SQLite integration database
Automated SSE validation
Playwright browser E2E tests
GitHub Actions E2E job
```