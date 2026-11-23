import {
  expect,
  test,
} from "@playwright/test";

import {
  createIncident,
  fillIncident,
  historyCard,
  openApplication,
  submitInvestigation,
  waitForCompletion,
} from "./helpers";

test(
  "keeps a completed request in history after page reload",
  async ({
    page,
    request,
  }) => {
    const incident =
      createIncident(
        "persistent history",
      );

    await openApplication(page);

    await fillIncident(
      page,
      incident,
    );

    await submitInvestigation(page);

    await waitForCompletion(
      page,
      request,
      incident,
      1,
    );

    await page.reload();

    await expect(
      page.getByRole("heading", {
        name:
          "Incident Intelligence Workspace",
      }),
    ).toBeVisible();

    const persistedCard =
      historyCard(
        page,
        incident,
        1,
      );

    await expect(
      persistedCard,
    ).toBeVisible({
      timeout: 15_000,
    });

    await expect(
      persistedCard.getByText(
        incident,
        {
          exact: true,
        },
      ),
    ).toBeVisible();

    await expect(
      persistedCard.getByText(
        "Completed",
        {
          exact: true,
        },
      ),
    ).toBeVisible();

    await expect(
      persistedCard,
    ).toContainText(
      "Attempt 1",
    );
  },
);