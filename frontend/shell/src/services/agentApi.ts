import {
  getApiAccessToken,
} from "../auth/accessTokenProvider";
import {
  authenticationEnabled,
} from "../auth/authConfig";
import type {
  AgentStreamEvent,
  ApiError,
  CreateAgentRequest,
  RequestAcceptedResponse,
  RequestDetails,
} from "../types/agent";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "/api";

async function createHeaders(
  initialHeaders?: HeadersInit,
): Promise<Headers> {
  const headers =
    new Headers(initialHeaders);

  const accessToken =
    await getApiAccessToken();

  if (
    authenticationEnabled &&
    !accessToken
  ) {
    throw {
      title:
        "Authentication required",

      detail:
        "No API access token is available. " +
        "Sign in again and retry.",

      status:
        401,

      code:
        "ACCESS_TOKEN_UNAVAILABLE",

      retryable:
        false,
    } satisfies ApiError;
  }

  if (accessToken) {
    headers.set(
      "Authorization",
      `Bearer ${accessToken}`,
    );
  }

  return headers;
}

async function authenticatedFetch(
  input: RequestInfo | URL,
  init?: RequestInit,
): Promise<Response> {
  const headers =
    await createHeaders(
      init?.headers);

  return await fetch(
    input,
    {
      ...init,
      headers,
    },
  );
}

async function parseError(
  response: Response,
): Promise<ApiError> {
  try {
    const problem =
      (await response.json()) as ApiError;

    return {
      title:
        problem.title ??
        "Request failed",

      detail:
        problem.detail ??
        "An unexpected error occurred.",

      status:
        response.status,

      code:
        problem.code,

      retryable:
        problem.retryable ??
        response.status >= 500,

      errors:
        problem.errors,
    };
  } catch {
    return {
      title:
        response.status === 401
          ? "Authentication required"
          : response.status === 403
            ? "Access denied"
            : "Request failed",

      detail:
        response.status === 401
          ? "The API rejected the access token. " +
            "Sign out, sign in again, and retry."
          : response.status === 403
            ? "The signed-in account does not " +
              "have the required AgentOps role."
            : `The server returned HTTP ` +
              `${response.status}.`,

      status:
        response.status,

      retryable:
        response.status >= 500,
    };
  }
}

function normalizeStreamUrl(
  streamUrl: string,
): string {
  /*
   * Development and E2E requests use the Vite
   * same-origin /api proxy.
   *
   * This prevents browser CORS and hostname
   * mismatches when the API returns an absolute URL.
   */
  if (
    API_BASE_URL.startsWith("/")
  ) {
    const parsedUrl =
      new URL(
        streamUrl,
        window.location.origin,
      );

    const apiPathIndex =
      parsedUrl.pathname.indexOf(
        "/api/",
      );

    if (apiPathIndex >= 0) {
      return parsedUrl.pathname.slice(
        apiPathIndex,
      );
    }

    const versionedPathIndex =
      parsedUrl.pathname.indexOf(
        "/v1/",
      );

    if (versionedPathIndex >= 0) {
      return (
        API_BASE_URL.replace(
          /\/$/,
          "",
        ) +
        parsedUrl.pathname.slice(
          versionedPathIndex,
        )
      );
    }
  }

  return streamUrl;
}

export async function createRequest(
  input: CreateAgentRequest,
  signal?: AbortSignal,
): Promise<RequestAcceptedResponse> {
  const response =
    await authenticatedFetch(
      `${API_BASE_URL}/v1/requests`,
      {
        method:
          "POST",

        headers: {
          "Content-Type":
            "application/json",
        },

        body:
          JSON.stringify(input),

        signal,
      },
    );

  if (!response.ok) {
    throw await parseError(response);
  }

  const accepted =
    (await response.json()) as
      RequestAcceptedResponse;

  return {
    ...accepted,

    streamUrl:
      normalizeStreamUrl(
        accepted.streamUrl,
      ),
  };
}

export async function retryRequest(
  requestId: string,
  signal?: AbortSignal,
): Promise<RequestAcceptedResponse> {
  const response =
    await authenticatedFetch(
      `${API_BASE_URL}/v1/requests/` +
      `${requestId}/retry`,
      {
        method:
          "POST",

        signal,
      },
    );

  if (!response.ok) {
    throw await parseError(response);
  }

  const accepted =
    (await response.json()) as
      RequestAcceptedResponse;

  return {
    ...accepted,

    streamUrl:
      normalizeStreamUrl(
        accepted.streamUrl,
      ),
  };
}

export async function
  getRequestHistory():
  Promise<RequestDetails[]> {
  const response =
    await authenticatedFetch(
      `${API_BASE_URL}/v1/requests`,
    );

  if (!response.ok) {
    throw await parseError(response);
  }

  return (
    await response.json()
  ) as RequestDetails[];
}

function parseEventBlock(
  block: string,
): AgentStreamEvent | null {
  const lines =
    block
      .split("\n")
      .map((line) =>
        line.trimEnd(),
      );

  const eventName =
    lines
      .find((line) =>
        line.startsWith("event:"),
      )
      ?.slice(
        "event:".length,
      )
      .trim();

  const data =
    lines
      .filter((line) =>
        line.startsWith("data:"),
      )
      .map((line) =>
        line
          .slice(
            "data:".length,
          )
          .trimStart(),
      )
      .join("\n");

  if (!data) {
    return null;
  }

  const parsed =
    JSON.parse(data) as
      AgentStreamEvent;

  return {
    ...parsed,

    type:
      eventName ??
      parsed.type,
  };
}

export async function streamRequest(
  streamUrl: string,
  onEvent: (
    event: AgentStreamEvent,
  ) => void,
  signal: AbortSignal,
): Promise<void> {
  const normalizedUrl =
    normalizeStreamUrl(
      streamUrl);

  const response =
    await authenticatedFetch(
      normalizedUrl,
      {
        method:
          "GET",

        headers: {
          Accept:
            "text/event-stream",
        },

        signal,

        cache:
          "no-store",
      },
    );

  if (
    !response.ok ||
    !response.body
  ) {
    throw await parseError(response);
  }

  const contentType =
    response.headers.get(
      "content-type");

  if (
    !contentType
      ?.toLowerCase()
      .includes(
        "text/event-stream",
      )
  ) {
    throw {
      title:
        "Invalid stream response",

      detail:
        "The server did not return " +
        "a Server-Sent Events response.",

      status:
        response.status,

      code:
        "INVALID_STREAM_CONTENT_TYPE",

      retryable:
        true,
    } satisfies ApiError;
  }

  const reader =
    response.body.getReader();

  const decoder =
    new TextDecoder();

  let buffer =
    "";

  try {
    while (true) {
      const result =
        await reader.read();

      if (result.done) {
        buffer +=
          decoder.decode();

        break;
      }

      buffer +=
        decoder.decode(
          result.value,
          {
            stream:
              true,
          },
        );

      /*
       * Normalize CRLF emitted by ASP.NET Core
       * before splitting SSE event blocks.
       */
      buffer =
        buffer.replace(
          /\r\n/g,
          "\n",
        );

      const blocks =
        buffer.split(
          "\n\n");

      buffer =
        blocks.pop() ?? "";

      for (
        const block of blocks
      ) {
        const event =
          parseEventBlock(
            block);

        if (event) {
          onEvent(event);
        }
      }
    }

    const finalBlock =
      buffer
        .replace(
          /\r\n/g,
          "\n",
        )
        .trim();

    if (finalBlock) {
      const event =
        parseEventBlock(
          finalBlock);

      if (event) {
        onEvent(event);
      }
    }
  } finally {
    reader.releaseLock();
  }
}