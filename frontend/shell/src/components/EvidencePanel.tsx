import {
  Badge,
  Card,
  CardHeader,
  Text,
} from "@fluentui/react-components";
import type {
  EvidenceReference,
} from "../utils/evidenceParser";

interface EvidencePanelProps {
  evidence: EvidenceReference[];
}

export function EvidencePanel({
  evidence,
}: EvidencePanelProps) {
  if (evidence.length === 0) {
    return null;
  }

  return (
    <aside
      className="evidence-panel"
      aria-labelledby="evidence-panel-title"
    >
      <div className="evidence-panel-heading">
        <div>
          <h3 id="evidence-panel-title">
            Evidence cited
          </h3>

          <Text className="evidence-panel-description">
            Deployment evidence used by the AI response.
          </Text>
        </div>

        <Badge
          appearance="tint"
          color="warning"
        >
          Mock evidence
        </Badge>
      </div>

      <div className="evidence-card-list">
        {evidence.map((item) => (
          <Card
            key={item.id}
            id={`evidence-${item.id}`}
            className="evidence-detail-card"
            tabIndex={-1}
          >
            <CardHeader
              header={
                <div className="evidence-card-title">
                  <Badge
                    appearance="filled"
                    color="brand"
                  >
                    {item.id}
                  </Badge>

                  <Text weight="semibold">
                    Deployment record
                  </Text>
                </div>
              }
              description={
                <Text>
                  Read-only deployment evidence
                </Text>
              }
            />

            <dl className="evidence-metadata">
              <div>
                <dt>Source type</dt>
                <dd>Mock deployment evidence</dd>
              </div>

              <div>
                <dt>Trust level</dt>
                <dd>Demonstration only</dd>
              </div>

              <div>
                <dt>Usage</dt>
                <dd>
                  Supports investigation hypotheses but
                  does not prove root cause.
                </dd>
              </div>
            </dl>
          </Card>
        ))}
      </div>

      <div
        className="evidence-warning"
        role="note"
      >
        These records are deterministic demonstration
        data. Production decisions require verification
        from the actual deployment platform, telemetry,
        logs, and service owners.
      </div>
    </aside>
  );
}