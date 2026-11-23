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
  "navigates from an evidence citation to its evidence card",
  async ({
    page,
    request,
  }) => {
    const incident =
      createIncident(
        "evidence navigation",
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

    await expect(
      page.getByRole("heading", {
        name: "Evidence cited",
      }),
    ).toBeVisible({
      timeout: 15_000,
    });

    await expect(
      page.getByText(
        "Mock evidence",
        {
          exact: true,
        },
      ),
    ).toBeVisible();

    const evidenceReference =
      page.getByRole("button", {
        name: /DEP-EV-001/,
      });

    await expect(
      evidenceReference,
    ).toBeVisible();

    await evidenceReference.click();

    const evidenceCard =
      page.locator(
        "#evidence-DEP-EV-001",
      );

    await expect(
      evidenceCard,
    ).toBeVisible();

    await expect(
      evidenceCard,
    ).toBeFocused();

    await expect(
      evidenceCard.getByText(
        "Mock deployment evidence",
      ),
    ).toBeVisible();

    await expect(
      evidenceCard.getByText(
        "Demonstration only",
      ),
    ).toBeVisible();

    await expect(
      page.getByRole("note"),
    ).toContainText(
      "Production decisions require verification",
    );
  },
);