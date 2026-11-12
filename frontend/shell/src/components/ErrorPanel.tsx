import {
  Button,
  MessageBar,
  MessageBarActions,
  MessageBarBody,
  MessageBarTitle,
} from "@fluentui/react-components";
import type { ApiError } from "../types/agent";

interface ErrorPanelProps {
  error: ApiError;
  onRetry: () => Promise<void>;
}

export function ErrorPanel({
  error,
  onRetry,
}: ErrorPanelProps) {
  return (
    <MessageBar intent="error">
      <MessageBarBody>
        <MessageBarTitle>{error.title}</MessageBarTitle>
        {error.detail}
        {error.code && ` Error code: ${error.code}.`}
      </MessageBarBody>

      {error.retryable && (
        <MessageBarActions>
          <Button type="button" onClick={() => void onRetry()}>
            Retry
          </Button>
        </MessageBarActions>
      )}
    </MessageBar>
  );
}