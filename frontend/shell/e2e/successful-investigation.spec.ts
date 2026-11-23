import {
  expect,
  test,
} from "@playwright/test";

import {
  createIncident,
  fillIncident,
  openApplication,
  submitInvestigation,
  waitForCompletion,
} from "./helpers";

test(
  "submits and completes a grounded investigation",
  async ({
    page,
    request,
  }) => {
    const incident =
      createIncident(
        "successful investigation",
      );

    await openApplication(page);

    await fillIncident(
      page,
      incident,
    );

    await submitInvestigation(page);

    const card =
      await waitForCompletion(
        page,
        request,
        incident,
        1,
      );

    await expect(
      card.getByText(
        incident,
        {
          exact: true,
        },
      ),
    ).toBeVisible();

    await expect(card)
      .toContainText(
        "Attempt 1",
      );

    await expect(
      card.getByText(
        "Completed",
        {
          exact: true,
        },
      ),
    ).toBeVisible();

    await expect(
      page.getByText(
        "DEP-EV-001",
        {
          exact: true,
        },
      ).first(),
    ).toBeVisible();
  },
);