import {
  Badge,
  Card,
  CardHeader,
  Text,
} from "@fluentui/react-components";
import type { RequestDetails } from "../types/agent";

interface RequestHistoryProps {
  requests: RequestDetails[];
  loading: boolean;
}

function badgeAppearance(status: string) {
  switch (status) {
    case "Completed":
      return "filled";
    case "Failed":
    case "Cancelled":
      return "outline";
    default:
      return "tint";
  }
}

export function RequestHistory({
  requests,
  loading,
}: RequestHistoryProps) {
  return (
    <section>
      <h2>Request history</h2>

      {loading && <Text>Loading request history...</Text>}

      {!loading && requests.length === 0 && (
        <Text>No requests have been submitted.</Text>
      )}

      <div className="history-list">
        {requests.map((request) => (
          <Card key={request.requestId}>
            <CardHeader
              header={
                <Text weight="semibold">
                  {request.message}
                </Text>
              }
              description={
                <Text>
                  {request.environment} · {request.severity} ·
                  Attempt {request.attemptNumber}
                </Text>
              }
              action={
                <Badge
                  appearance={badgeAppearance(request.status)}
                >
                  {request.status}
                </Badge>
              }
            />

            {request.errorCode && (
              <Text className="error-code">
                {request.errorCode}
              </Text>
            )}
          </Card>
        ))}
      </div>
    </section>
  );
}