import {
  expect,
  test,
} from "@playwright/test";

import {
  createIncident,
  fillIncident,
  historyCard,
  openApplication,
  refreshHistory,
  submitInvestigation,
  waitForCompletion,
  waitForRequestStatus,
} from "./helpers";

test(
  "shows a simulated failure and completes retry attempt two",
  async ({
    page,
    request,
  }) => {
    const incident =
      createIncident(
        "failure and retry",
      );

    await openApplication(page);

    await fillIncident(
      page,
      incident,
    );

    await page
      .getByRole("checkbox", {
        name:
          "Simulate AI service failure",
      })
      .check();

    await submitInvestigation(page);

    await expect(
      page.getByRole("button", {
        name: "Retry",
      }),
    ).toBeVisible({
      timeout: 30_000,
    });

    await expect(
      page.getByText(
        /AGENT_SERVICE_UNAVAILABLE/,
      ).first(),
    ).toBeVisible({
      timeout: 30_000,
    });

    await waitForRequestStatus(
      request,
      incident,
      1,
      "Failed",
    );

    await refreshHistory(page);

    const failedCard =
      historyCard(
        page,
        incident,
        1,
      );

    await expect(
      failedCard,
    ).toBeVisible();

    await expect(
      failedCard.getByText(
        "Failed",
        {
          exact: true,
        },
      ),
    ).toBeVisible();

    await expect(
      failedCard.getByText(
        "AGENT_SERVICE_UNAVAILABLE",
        {
          exact: true,
        },
      ),
    ).toBeVisible();

    await page
      .getByRole("button", {
        name: "Retry",
      })
      .click();

    const retryCard =
      await waitForCompletion(
        page,
        request,
        incident,
        2,
      );

    await expect(retryCard)
      .toContainText(
        "Attempt 2",
      );

    await expect(
      retryCard.getByText(
        "Completed",
        {
          exact: true,
        },
      ),
    ).toBeVisible();

    await expect(
      failedCard.getByText(
        "Failed",
        {
          exact: true,
        },
      ),
    ).toBeVisible();
  },
);