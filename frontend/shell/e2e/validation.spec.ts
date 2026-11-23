import {
  expect,
  test,
} from "@playwright/test";

import {
  incidentInput,
  openApplication,
} from "./helpers";

test(
  "rejects an incident description that is too short",
  async ({ page }) => {
    await openApplication(page);

    const historyCards =
      page.locator(
        ".history-list > *",
      );

    const initialCount =
      await historyCards.count();

    await incidentInput(page)
      .fill("Failure");

    await page
      .getByRole("button", {
        name:
          "Start investigation",
      })
      .click();

    await expect(
      page.getByText(
        /at least 10 characters/i,
      ),
    ).toBeVisible();

    await expect(
      incidentInput(page),
    ).toHaveValue("Failure");

    await expect(
      historyCards,
    ).toHaveCount(initialCount);

    await expect(
      page.getByRole("button", {
        name:
          "Start investigation",
      }),
    ).toBeEnabled();
  },
);