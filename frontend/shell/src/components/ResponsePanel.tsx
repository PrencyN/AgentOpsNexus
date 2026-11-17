import {
  Card,
  CardHeader,
  Spinner,
  Text,
} from "@fluentui/react-components";
import type {
  RequestUiStatus,
} from "../hooks/useAgentRequest";
import {
  createEvidenceReferences,
} from "../utils/evidenceParser";
import { EvidencePanel } from "./EvidencePanel";
import { MarkdownResponse } from "./MarkdownResponse";

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

  const evidence =
    createEvidenceReferences(response);

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
        <section
          className="investigation-progress"
          aria-labelledby="progress-heading"
        >
          <h3
            id="progress-heading"
            className="visually-hidden"
          >
            Investigation progress
          </h3>

          <ol
            className="progress-list"
            aria-live="polite"
          >
            {progress.map((item, index) => (
              <li key={`${item}-${index}`}>
                {item}
              </li>
            ))}
          </ol>
        </section>
      )}

      {response && (
        <>
          <div className="response-content">
            <MarkdownResponse
              content={response}
            />
          </div>

          <EvidencePanel
            evidence={evidence}
          />
        </>
      )}

      {status === "idle" && (
        <Text>
          Submit an incident request to begin the
          investigation.
        </Text>
      )}

      {status === "cancelled" && (
        <Text>
          The investigation was cancelled.
        </Text>
      )}
    </Card>
  );
}