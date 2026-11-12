import type {
  AgentStreamEvent,
  ApiError,
  CreateAgentRequest,
  RequestAcceptedResponse,
  RequestDetails,
} from "../types/agent";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "/api";

async function parseError(response: Response): Promise<ApiError> {
  try {
    const problem = (await response.json()) as ApiError;

    return {
      title: problem.title ?? "Request failed",
      detail: problem.detail ?? "An unexpected error occurred.",
      status: response.status,
      code: problem.code,
      retryable: problem.retryable ?? response.status >= 500,
      errors: problem.errors,
    };
  } catch {
    return {
      title: "Request failed",
      detail: `The server returned HTTP ${response.status}.`,
      status: response.status,
      retryable: response.status >= 500,
    };
  }
}

export async function createRequest(
  input: CreateAgentRequest,
  signal?: AbortSignal,
): Promise<RequestAcceptedResponse> {
  const response = await fetch(`${API_BASE_URL}/v1/requests`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(input),
    signal,
  });

  if (!response.ok) {
    throw await parseError(response);
  }

  return (await response.json()) as RequestAcceptedResponse;
}

export async function retryRequest(
  requestId: string,
  signal?: AbortSignal,
): Promise<RequestAcceptedResponse> {
  const response = await fetch(
    `${API_BASE_URL}/v1/requests/${requestId}/retry`,
    {
      method: "POST",
      signal,
    },
  );

  if (!response.ok) {
    throw await parseError(response);
  }

  return (await response.json()) as RequestAcceptedResponse;
}

export async function getRequestHistory(): Promise<RequestDetails[]> {
  const response = await fetch(`${API_BASE_URL}/v1/requests`);

  if (!response.ok) {
    throw await parseError(response);
  }

  return (await response.json()) as RequestDetails[];
}

export async function streamRequest(
  streamUrl: string,
  onEvent: (event: AgentStreamEvent) => void,
  signal: AbortSignal,
): Promise<void> {
  const response = await fetch(streamUrl, {
    method: "GET",
    headers: {
      Accept: "text/event-stream",
    },
    signal,
  });

  if (!response.ok || !response.body) {
    throw await parseError(response);
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();

  let buffer = "";

  while (true) {
    const result = await reader.read();

    if (result.done) {
      break;
    }

    buffer += decoder.decode(result.value, { stream: true });

    const blocks = buffer.split("\n\n");
    buffer = blocks.pop() ?? "";

    for (const block of blocks) {
      const lines = block.split("\n");

      const eventName = lines
        .find((line) => line.startsWith("event:"))
        ?.replace("event:", "")
        .trim();

      const data = lines
        .filter((line) => line.startsWith("data:"))
        .map((line) => line.replace("data:", "").trim())
        .join("");

      if (!data) {
        continue;
      }

      const parsed = JSON.parse(data) as AgentStreamEvent;

      onEvent({
        ...parsed,
        type: eventName ?? parsed.type,
      });
    }
  }
}