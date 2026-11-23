import {
  expect,
  type APIRequestContext,
  type Locator,
  type Page,
} from "@playwright/test";

interface RequestHistoryRecord {
  requestId: string;
  message: string;
  attemptNumber: number;
  status: string;
  errorCode?: string | null;
}

export function createIncident(
  scenario: string,
): string {
  const uniqueId = [
    Date.now(),
    Math.random()
      .toString(16)
      .slice(2),
  ].join("-");

  return (
    "Checkout failures increased after the latest " +
    "PaymentService deployment. Investigate whether " +
    "a recent deployment could be related. " +
    `E2E scenario: ${scenario}. ` +
    `Run: ${uniqueId}.`
  );
}

export async function openApplication(
  page: Page,
): Promise<void> {
  await page.goto("/");

  await expect(
    page.getByRole("heading", {
      name:
        "Incident Intelligence Workspace",
    }),
  ).toBeVisible();

  await expect(
    page.getByRole("heading", {
      name:
        "New investigation",
    }),
  ).toBeVisible();

  await expect(
    page.getByRole("heading", {
      name:
        "Request history",
    }).first(),
  ).toBeVisible();
}

export function incidentInput(
  page: Page,
): Locator {
  return page
    .getByRole("textbox")
    .first();
}

export async function fillIncident(
  page: Page,
  message: string,
): Promise<void> {
  await incidentInput(page)
    .fill(message);

  await page
    .getByLabel("Environment")
    .selectOption(
      "Development",
    );

  await page
    .getByLabel("Severity")
    .selectOption(
      "High",
    );
}

export async function submitInvestigation(
  page: Page,
): Promise<void> {
  await page
    .getByRole("button", {
      name:
        "Start investigation",
    })
    .click();
}

export function historyCard(
  page: Page,
  message: string,
  attemptNumber: number,
): Locator {
  return page
    .locator(".history-list")
    .getByRole("group", {
      name:
        new RegExp(
          escapeRegularExpression(
            message,
          ) +
          `.*Attempt ${attemptNumber}.*`,
          "i",
        ),
    })
    .last();
}

export async function refreshHistory(
  page: Page,
): Promise<void> {
  const refreshButton =
    page.getByRole("button", {
      name:
        /Refresh history|Refreshing/i,
    });

  await expect(
    refreshButton,
  ).toBeEnabled({
    timeout:
      15_000,
  });

  await refreshButton.click();

  await expect(
    page.getByRole("button", {
      name:
        "Refresh history",
    }),
  ).toBeEnabled({
    timeout:
      15_000,
  });
}

export async function findRequest(
  request:
    APIRequestContext,
  message: string,
  attemptNumber: number,
): Promise<
  RequestHistoryRecord | undefined
> {
  const response =
    await request.get(
      "/api/v1/requests",
    );

  if (!response.ok()) {
    return undefined;
  }

  const records =
    (await response.json()) as
      RequestHistoryRecord[];

  return records.find(
    (record) =>
      record.message ===
        message &&
      record.attemptNumber ===
        attemptNumber,
  );
}

export async function waitForRequestStatus(
  request:
    APIRequestContext,
  message: string,
  attemptNumber: number,
  expectedStatus: string,
): Promise<RequestHistoryRecord> {
  let matchingRecord:
    RequestHistoryRecord |
    undefined;

  await expect
    .poll(
      async () => {
        matchingRecord =
          await findRequest(
            request,
            message,
            attemptNumber,
          );

        return (
          matchingRecord?.status ??
          "NOT_FOUND"
        );
      },
      {
        timeout:
          60_000,

        intervals: [
          250,
          500,
          1_000,
        ],
      },
    )
    .toBe(
      expectedStatus,
    );

  return matchingRecord!;
}

export async function waitForCompletion(
  page: Page,
  request:
    APIRequestContext,
  message: string,
  attemptNumber = 1,
): Promise<Locator> {
  await waitForRequestStatus(
    request,
    message,
    attemptNumber,
    "Completed",
  );

  /*
   * The backend is now the source of truth.
   * Allow React to receive and render the final
   * SSE fragments before refreshing history.
   */
  await expect(
    page.getByRole("button", {
      name:
        "Start investigation",
    }),
  ).toBeEnabled({
    timeout:
      15_000,
  });

  await refreshHistory(page);

  const card =
    historyCard(
      page,
      message,
      attemptNumber,
    );

  await expect(card)
    .toBeVisible({
      timeout:
        15_000,
    });

  await expect(
    card.getByText(
      "Completed",
      {
        exact:
          true,
      },
    ),
  ).toBeVisible({
    timeout:
      15_000,
  });

  return card;
}

function escapeRegularExpression(
  value: string,
): string {
  return value.replace(
    /[.*+?^${}()|[\]\\]/g,
    "\\$&",
  );
}