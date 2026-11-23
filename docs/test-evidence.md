# AgentOps Nexus AI Test Evidence

## Purpose

This document records the automated and manual evidence used to validate AgentOps Nexus AI.

Validation covers:

- Successful requests
- Invalid inputs
- AI service failures
- Retry behavior
- Persistent history
- React-to-ASP.NET Core integration
- ASP.NET Core agent execution
- Server-Sent Events
- Evidence citations
- Security controls
- Correlation IDs
- Rate limiting
- Dependency audits

## Current validated results

```text
Backend unit tests:          58 passed
Backend integration tests:   25 passed
Backend total:               83 passed
Frontend component tests:    18 passed
Playwright Chromium tests:    5 passed
NuGet vulnerable packages:    0
npm production vulnerabilities: 0
Frontend production build:   succeeded
```

## Test layers

| Layer | Technology | Result |
|---|---|---:|
| Domain and repository tests | xUnit | Passed |
| Agent tests | xUnit | Passed |
| Deployment-tool tests | xUnit | Passed |
| Input-security tests | xUnit | Passed |
| Evaluator tests | xUnit | Passed |
| Security middleware tests | xUnit | Passed |
| API integration tests | `WebApplicationFactory<Program>` | Passed |
| SQLite integration tests | EF Core and isolated SQLite | Passed |
| SSE integration tests | ASP.NET Core TestServer | Passed |
| React component tests | Vitest and Testing Library | Passed |
| Browser E2E tests | Playwright Chromium | Passed |
| Live Foundry validation | Manual | Passed |
| CI validation | GitHub Actions | Implemented |

## Backend unit tests

```text
Framework: xUnit
Project: AgentOpsNexus.AgentApi.UnitTests
Total: 58
Passed: 58
Failed: 0
Skipped: 0
```

Unit coverage includes:

- Repository behavior
- Request status transitions
- Retry relationships
- Mock-agent events
- Simulated failures
- Deployment evidence
- Tool validation
- Input security
- Security headers
- Correlation IDs
- Deterministic response evaluation

## Backend integration tests

```text
Framework: xUnit
Hosting: Microsoft.AspNetCore.Mvc.Testing
Factory: WebApplicationFactory<Program>
Project: AgentOpsNexus.AgentApi.IntegrationTests
Total: 25
Passed: 25
Failed: 0
Skipped: 0
```

Integration tests use:

- The real ASP.NET Core middleware pipeline
- Real controllers
- Real request contracts
- Real security headers
- Real correlation middleware
- Real rate limiting
- Real EF Core persistence
- Isolated temporary SQLite databases
- Deterministic Mock agent provider
- Real SSE streaming endpoints

Integration-test parallelism is disabled for deterministic database and rate-limit behavior.

### Request creation coverage

- Valid request returns HTTP 202.
- Accepted response includes a request ID.
- Accepted response includes a stream URL.
- Safe correlation ID is preserved.
- Short input returns HTTP 400.
- Empty input returns HTTP 400.
- Oversized input returns HTTP 400.
- Null byte returns HTTP 400.
- Unsupported control character returns HTTP 400.
- Windows line endings are normalized.

### Security-pipeline coverage

- `X-Content-Type-Options` is present.
- `Referrer-Policy` is present.
- `X-Frame-Options` is present.
- Content Security Policy is restrictive.
- Permissions Policy disables sensitive features.
- Cache control prevents storage.
- A missing correlation ID is generated.
- A safe caller correlation ID is preserved.
- An unsafe correlation ID is replaced.
- Header and response-body correlation IDs match.
- The creation rate limit returns HTTP 429.

### History and retry coverage

- A created request appears in history.
- An unknown request returns HTTP 404.
- A simulated failure is persisted.
- Failure is retryable.
- Retry creates a new request.
- Attempt number increments to 2.
- Retry references the original request.
- Original failed attempt remains available.
- History is returned newest first.

### SSE coverage

- Stream content type is `text/event-stream`.
- Stream includes `agent.started`.
- Stream includes `agent.progress`.
- Stream includes `agent.message.delta`.
- Stream includes `agent.completed`.
- Simulated failure includes `agent.failed`.
- Successful stream persists `Completed`.
- Failure stream persists `Failed`.
- Unknown request returns HTTP 404.
- Completed request cannot be streamed again.

## Frontend component tests

```text
Framework: Vitest and Testing Library
Test files: 3
Total: 18
Passed: 18
Failed: 0
```

Component tests cover:

- Short-description validation
- Valid form submission
- Busy-state disabling
- Markdown headings
- Markdown lists
- Evidence citation transformation
- Streaming Markdown fragments
- Evidence extraction
- Evidence badge activation
- Evidence-card focus
- Mock-evidence disclosure
- Evidence warning content

Vitest is restricted to:

```text
src/**/*.test.ts
src/**/*.test.tsx
src/**/*.spec.ts
src/**/*.spec.tsx
```

Playwright files under `e2e/` are excluded from Vitest discovery.

## Browser E2E tests

```text
Framework: Playwright
Browser: Chromium
Workers: 1
Test files: 5
Total: 5
Passed: 5
Failed: 0
```

The E2E suite automatically starts:

```text
ASP.NET Core API on port 7001
Vite frontend on port 5173
Mock agent provider
Isolated SQLite database
```

### Successful investigation

The browser test verifies:

1. The application loads.
2. The incident form is completed.
3. The request is submitted.
4. The Mock agent streams a response.
5. The persisted request becomes `Completed`.
6. History is refreshed.
7. Attempt 1 is visible.
8. Evidence identifier `DEP-EV-001` is rendered.

### Invalid input

The browser test verifies:

1. `Failure` is entered.
2. Client validation displays the minimum-length message.
3. The form keeps the input value.
4. No new history record is created.
5. The submit button remains usable.

### Failure and retry

The browser test verifies:

1. AI failure simulation is enabled.
2. The Mock agent throws the deterministic failure.
3. Attempt 1 becomes `Failed`.
4. `AGENT_SERVICE_UNAVAILABLE` is persisted.
5. Retry is available.
6. Retry creates attempt 2.
7. Attempt 2 becomes `Completed`.
8. Attempt 1 remains visible.

The failure log is expected because the scenario deliberately simulates an unavailable AI service.

### Evidence navigation

The browser test verifies:

1. Mock evidence citations are rendered.
2. `DEP-EV-001` is an accessible button.
3. Selecting the citation navigates to the matching evidence card.
4. The card receives keyboard focus.
5. The card identifies mock deployment evidence.
6. The card identifies demonstration-only trust.
7. The production-verification warning is visible.

### Persistent history

The browser test verifies:

1. A request completes.
2. The request is persisted.
3. The browser page is reloaded.
4. The history record remains visible.
5. The status remains `Completed`.
6. Attempt 1 remains visible.

## SSE reliability improvement

Browser E2E testing exposed two stream-integration concerns:

- ASP.NET Core may use CRLF line endings.
- The API may return a stream URL that differs from the browser origin.

The frontend now:

- Normalizes CRLF to LF before splitting SSE blocks.
- Processes a final event without a trailing blank line.
- Verifies `text/event-stream`.
- Routes development stream requests through the same-origin `/api` proxy.
- Uses `cache: "no-store"` for stream requests.

## Persistence isolation

Each Playwright process creates a database named similarly to:

```text
agentops-e2e-<process-id>.db
```

The database is stored under the ignored `App_Data` directory.

Each API integration factory also creates an isolated temporary SQLite database.

## Dependency audits

### NuGet

```powershell
dotnet list `
  .\AgentOpsNexus.slnx `
  package `
  --vulnerable `
  --include-transitive
```

Validated result:

```text
AgentOpsNexus.AgentApi: no vulnerable packages
AgentOpsNexus.AgentApi.UnitTests: no vulnerable packages
AgentOpsNexus.AgentApi.IntegrationTests: no vulnerable packages
```

### npm production dependencies

```powershell
npm audit --omit=dev
```

Validated result:

```text
found 0 vulnerabilities
```

## Full validation commands

```powershell
cd C:\Workspace\AgentOpsNexus

dotnet build .\AgentOpsNexus.slnx

dotnet test `
  .\AgentOpsNexus.slnx `
  --no-build

dotnet list `
  .\AgentOpsNexus.slnx `
  package `
  --vulnerable `
  --include-transitive

cd .\frontend\shell

npm run test
npm run build
npm run test:e2e
npm audit --omit=dev
```

## Final result

```text
Backend: 83 of 83 tests passed
Frontend components: 18 of 18 tests passed
Browser E2E: 5 of 5 tests passed
NuGet audit: clean
npm production audit: clean
Production frontend build: succeeded
```