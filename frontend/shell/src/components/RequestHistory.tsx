import {
  Badge,
  Card,
  CardHeader,
  Text,
} from "@fluentui/react-components";

import type {
  RequestDetails,
} from "../types/agent";

interface RequestHistoryProps {
  requests: RequestDetails[];
  loading: boolean;
}

function badgeAppearance(
  status: string,
): "filled" | "outline" | "tint" {
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
    <section
      aria-labelledby="request-history-list-title"
    >
      <h2
        id="request-history-list-title"
        className="visually-hidden"
      >
        Request history records
      </h2>

      {loading && (
        <Text role="status">
          Loading request history...
        </Text>
      )}

      {!loading &&
        requests.length === 0 && (
          <Text>
            No requests have been submitted.
          </Text>
        )}

      <div
        className="history-list"
        aria-live="polite"
      >
        {requests.map((request) => (
          <Card
            key={request.requestId}
            aria-label={
              `${request.message}. ` +
              `${request.environment}. ` +
              `${request.severity}. ` +
              `Attempt ${request.attemptNumber}. ` +
              `Status ${request.status}.`
            }
          >
            <CardHeader
              header={
                <Text weight="semibold">
                  {request.message}
                </Text>
              }
              description={
                <Text>
                  {request.environment}
                  {" | "}
                  {request.severity}
                  {" | "}
                  Attempt {request.attemptNumber}
                </Text>
              }
              action={
                <Badge
                  appearance={badgeAppearance(
                    request.status,
                  )}
                >
                  {request.status}
                </Badge>
              }
            />

            {request.errorCode && (
              <Text
                className="error-code"
                aria-label={
                  `Error code ${request.errorCode}`
                }
              >
                {request.errorCode}
              </Text>
            )}

            {request.retriedFromRequestId && (
              <Text className="retry-reference">
                Retried from request{" "}
                {request.retriedFromRequestId}
              </Text>
            )}
          </Card>
        ))}
      </div>
    </section>
  );
}