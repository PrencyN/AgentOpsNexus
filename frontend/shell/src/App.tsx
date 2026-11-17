import {
  FluentProvider,
  webDarkTheme,
  webLightTheme,
} from "@fluentui/react-components";
import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";
import "./App.css";
import { ErrorPanel } from "./components/ErrorPanel";
import { RequestForm } from "./components/RequestForm";
import { RequestHistory } from "./components/RequestHistory";
import { ResponsePanel } from "./components/ResponsePanel";
import { useAgentRequest } from "./hooks/useAgentRequest";
import { getRequestHistory } from "./services/agentApi";
import type {
  CreateAgentRequest,
  RequestDetails,
} from "./types/agent";

function App() {
  const prefersDarkTheme = useMemo(
    () =>
      window.matchMedia(
        "(prefers-color-scheme: dark)",
      ).matches,
    [],
  );

  const [history, setHistory] = useState<
    RequestDetails[]
  >([]);

  const [historyLoading, setHistoryLoading] =
    useState(false);

  const [historyError, setHistoryError] =
    useState<string | null>(null);

  const loadHistory = useCallback(async () => {
    setHistoryLoading(true);
    setHistoryError(null);

    try {
      const requests =
        await getRequestHistory();

      setHistory(requests);
    } catch (error) {
      setHistoryError(
        error instanceof Error
          ? error.message
          : "Request history could not be loaded.",
      );
    } finally {
      setHistoryLoading(false);
    }
  }, []);

  const {
    status,
    response,
    progress,
    error,
    submit,
    retry,
    cancel,
  } = useAgentRequest(loadHistory);

  useEffect(() => {
    void loadHistory();
  }, [loadHistory]);

  const busy =
    status === "submitting" ||
    status === "streaming" ||
    status === "retrying";

  const handleSubmit = async (
    request: CreateAgentRequest,
  ) => {
    await submit(request);
  };

  return (
    <FluentProvider
      theme={
        prefersDarkTheme
          ? webDarkTheme
          : webLightTheme
      }
    >
      <div className="app-shell">
        <header className="app-header">
          <div className="app-header-content">
            <div className="product-heading">
              <span className="product-mark">
                AN
              </span>

              <div>
                <span className="eyebrow">
                  AGENTOPS NEXUS AI
                </span>

                <h1>
                  Incident Intelligence Workspace
                </h1>
              </div>
            </div>

            <p className="app-description">
              Investigate operational incidents using
              bounded, evidence-grounded AI workflows.
            </p>

            <div
              className="capability-list"
              aria-label="Platform capabilities"
            >
              <span className="capability-pill">
                Microsoft Foundry
              </span>

              <span className="capability-pill">
                Grounded evidence
              </span>

              <span className="capability-pill">
                Human approval
              </span>
            </div>
          </div>
        </header>

        <main className="app-main">
          <div className="workspace-grid">
            <section
              className="surface-panel request-panel"
              aria-labelledby="new-investigation-title"
            >
              <div className="section-heading">
                <div>
                  <span className="section-kicker">
                    INCIDENT REQUEST
                  </span>

                  <h2 id="new-investigation-title">
                    New investigation
                  </h2>
                </div>

                <span
                  className={
                    busy
                      ? "status-indicator status-running"
                      : "status-indicator status-ready"
                  }
                >
                  {busy ? "Running" : "Ready"}
                </span>
              </div>

              <p className="section-description">
                Describe the incident and select the
                affected environment and severity.
              </p>

              <RequestForm
                busy={busy}
                onSubmit={handleSubmit}
                onCancel={cancel}
              />
            </section>

            <section
              className="result-panel"
              aria-labelledby="investigation-result-title"
            >
              {error && (
                <ErrorPanel
                  error={error}
                  onRetry={retry}
                />
              )}

              <ResponsePanel
                status={status}
                response={response}
                progress={progress}
              />
            </section>
          </div>

          <section
            className="history-section"
            aria-labelledby="request-history-title"
          >
            <div className="history-heading">
              <div>
                <span className="section-kicker">
                  INVESTIGATION RECORDS
                </span>

                <h2 id="request-history-title">
                  Request history
                </h2>

                <p className="section-description">
                  Previous requests remain available
                  after API restarts through SQLite
                  persistence.
                </p>
              </div>

              <button
                type="button"
                className="refresh-history-button"
                onClick={() => void loadHistory()}
                disabled={historyLoading}
              >
                {historyLoading
                  ? "Refreshing..."
                  : "Refresh history"}
              </button>
            </div>

            {historyError && (
              <div
                className="history-error"
                role="alert"
              >
                {historyError}
              </div>
            )}

            <RequestHistory
              requests={history}
              loading={historyLoading}
            />
          </section>
        </main>

        <footer className="app-footer">
          <span>
            AgentOps Nexus AI
          </span>

          <span>
            Evidence-grounded incident intelligence
          </span>
        </footer>
      </div>
    </FluentProvider>
  );
}

export default App;