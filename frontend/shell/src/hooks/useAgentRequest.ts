import { useCallback, useRef, useState } from "react";
import {
  createRequest,
  retryRequest,
  streamRequest,
} from "../services/agentApi";
import type {
  AgentStreamEvent,
  ApiError,
  CreateAgentRequest,
} from "../types/agent";

export type RequestUiStatus =
  | "idle"
  | "submitting"
  | "streaming"
  | "completed"
  | "failed"
  | "cancelled"
  | "retrying";

function isApiError(value: unknown): value is ApiError {
  return (
    typeof value === "object" &&
    value !== null &&
    "title" in value &&
    "status" in value
  );
}

export function useAgentRequest(
  onHistoryChanged: () => Promise<void>,
) {
  const [status, setStatus] =
    useState<RequestUiStatus>("idle");

  const [response, setResponse] = useState("");
  const [progress, setProgress] = useState<string[]>([]);
  const [error, setError] = useState<ApiError | null>(null);
  const [requestId, setRequestId] = useState<string | null>(null);

  const abortControllerRef =
    useRef<AbortController | null>(null);

  const handleEvent = useCallback((event: AgentStreamEvent) => {
    if (event.type === "agent.message.delta") {
      setResponse((current) => current + event.message);
    } else {
      setProgress((current) => [...current, event.message]);
    }

    if (event.type === "agent.completed") {
      setStatus("completed");
    }

    if (event.type === "agent.failed") {
      setStatus("failed");
      setError({
        title: "Agent service unavailable",
        detail: event.message,
        status: 503,
        code: "AGENT_SERVICE_UNAVAILABLE",
        retryable: true,
      });
    }
  }, []);

  const stream = useCallback(
    async (streamUrl: string, controller: AbortController) => {
      setStatus("streaming");

      await streamRequest(
        streamUrl,
        handleEvent,
        controller.signal,
      );

      await onHistoryChanged();
    },
    [handleEvent, onHistoryChanged],
  );

  const submit = useCallback(
    async (input: CreateAgentRequest) => {
      abortControllerRef.current?.abort();

      const controller = new AbortController();
      abortControllerRef.current = controller;

      setStatus("submitting");
      setResponse("");
      setProgress([]);
      setError(null);
      setRequestId(null);

      try {
        const accepted = await createRequest(
          input,
          controller.signal,
        );

        setRequestId(accepted.requestId);

        await stream(accepted.streamUrl, controller);
      } catch (caughtError) {
        if (controller.signal.aborted) {
          setStatus("cancelled");
          await onHistoryChanged();
          return;
        }

        setStatus("failed");

        if (isApiError(caughtError)) {
          setError(caughtError);
        } else {
          setError({
            title: "Unexpected error",
            detail:
              caughtError instanceof Error
                ? caughtError.message
                : "The request could not be completed.",
            status: 500,
            retryable: true,
          });
        }

        await onHistoryChanged();
      }
    },
    [onHistoryChanged, stream],
  );

  const retry = useCallback(async () => {
    if (!requestId) {
      return;
    }

    const controller = new AbortController();
    abortControllerRef.current = controller;

    setStatus("retrying");
    setResponse("");
    setProgress([]);
    setError(null);

    try {
      const accepted = await retryRequest(
        requestId,
        controller.signal,
      );

      setRequestId(accepted.requestId);

      await stream(accepted.streamUrl, controller);
    } catch (caughtError) {
      if (controller.signal.aborted) {
        setStatus("cancelled");
        return;
      }

      setStatus("failed");

      setError(
        isApiError(caughtError)
          ? caughtError
          : {
              title: "Retry failed",
              detail: "The retry request could not be completed.",
              status: 500,
              retryable: true,
            },
      );
    } finally {
      await onHistoryChanged();
    }
  }, [onHistoryChanged, requestId, stream]);

  const cancel = useCallback(() => {
    abortControllerRef.current?.abort();
  }, []);

  return {
    status,
    response,
    progress,
    error,
    requestId,
    submit,
    retry,
    cancel,
  };
}