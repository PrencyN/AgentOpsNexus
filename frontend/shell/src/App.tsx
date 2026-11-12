import {
  FluentProvider,
  webDarkTheme,
  webLightTheme,
} from "@fluentui/react-components";
import { useCallback, useEffect, useState } from "react";
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
  const prefersDarkTheme = window.matchMedia(
    "(prefers-color-scheme: dark)",
  ).matches;

  const [history, setHistory] = useState<RequestDetails[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);

  const loadHistory = useCallback(async () => {
    setHistoryLoading(true);

    try {
      const requests = await getRequestHistory();
      setHistory(requests);
    } catch {
      // The main request error experience handles API failures.
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
      theme={prefersDarkTheme ? webDarkTheme : webLightTheme}
    >
      <div className="app-shell">
        <header className="app-header">
          <div>
            <span className="eyebrow">AGENTOPS NEXUS AI</span>
            <h1>Incident Intelligence Workspace</h1>
            <p>
              Investigate operational incidents using bounded,
              evidence-grounded AI workflows.
            </p>
          </div>
        </header>

        <main>
          <div className="workspace-grid">
            <section className="request-panel">
              <h2>New investigation</h2>

              <RequestForm
                busy={busy}
                onSubmit={handleSubmit}
                onCancel={cancel}
              />
            </section>

            <section className="result-panel">
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

          <RequestHistory
            requests={history}
            loading={historyLoading}
          />
        </main>
      </div>
    </FluentProvider>
  );
}

export default App;