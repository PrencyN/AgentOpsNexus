import {
  Card,
  CardHeader,
  Spinner,
  Text,
} from "@fluentui/react-components";
import type { RequestUiStatus } from "../hooks/useAgentRequest";

interface ResponsePanelProps {
  status: RequestUiStatus;
  response: string;
  progress: string[];
}

export function ResponsePanel({
  status,
  response,
  progress,
}: ResponsePanelProps) {
  const busy =
    status === "submitting" ||
    status === "streaming" ||
    status === "retrying";

  return (
    <Card className="response-card">
      <CardHeader
        header={
          <Text size={500} weight="semibold">
            AI investigation
          </Text>
        }
      />

      {busy && (
        <Spinner
          label={
            status === "retrying"
              ? "Retrying investigation..."
              : "Investigating incident..."
          }
        />
      )}

      {progress.length > 0 && (
        <ol className="progress-list" aria-label="Agent progress">
          {progress.map((item, index) => (
            <li key={`${item}-${index}`}>{item}</li>
          ))}
        </ol>
      )}

      {response && (
        <div className="response-content" aria-live="polite">
          {response}
        </div>
      )}

      {status === "idle" && (
        <Text>
          Submit an incident request to begin the investigation.
        </Text>
      )}

      {status === "cancelled" && (
        <Text>The investigation was cancelled.</Text>
      )}
    </Card>
  );
}