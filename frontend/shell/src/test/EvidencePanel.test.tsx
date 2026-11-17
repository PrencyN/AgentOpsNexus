import {
  render,
  screen,
} from "@testing-library/react";
import {
  describe,
  expect,
  it,
} from "vitest";
import { EvidencePanel } from "../components/EvidencePanel";
import {
  createEvidenceReferences,
  evidenceIdFromHref,
  extractEvidenceIds,
  isEvidenceHref,
  transformEvidenceCitations,
} from "../utils/evidenceParser";

describe("evidenceParser", () => {
  it("extracts unique evidence IDs", () => {
    const result = extractEvidenceIds(
      "Use [DEP-EV-001], DEP-EV-002 and DEP-EV-001.",
    );

    expect(result).toEqual([
      "DEP-EV-001",
      "DEP-EV-002",
    ]);
  });

  it("returns no evidence for empty content", () => {
    expect(extractEvidenceIds("")).toEqual([]);

    expect(
      extractEvidenceIds("No citations"),
    ).toEqual([]);
  });

  it(
    "transforms a bracketed evidence ID into a Markdown link",
    () => {
      const result =
        transformEvidenceCitations(
          "See [DEP-EV-001] for deployment evidence.",
        );

      const expected =
        "See " +
        "[DEP-EV-001]" +
        "(#evidence-DEP-EV-001)" +
        " for deployment evidence.";

      expect(result).toBe(expected);
    },
  );

  it(
    "transforms an unbracketed evidence ID into a Markdown link",
    () => {
      const result =
        transformEvidenceCitations(
          "Evidence DEP-EV-002 supports the hypothesis.",
        );

      const expected =
        "Evidence " +
        "[DEP-EV-002]" +
        "(#evidence-DEP-EV-002)" +
        " supports the hypothesis.";

      expect(result).toBe(expected);
    },
  );

  it("recognizes evidence links", () => {
    expect(
      isEvidenceHref(
        "#evidence-DEP-EV-001",
      ),
    ).toBe(true);

    expect(
      evidenceIdFromHref(
        "#evidence-DEP-EV-001",
      ),
    ).toBe("DEP-EV-001");

    expect(
      isEvidenceHref(
        "https://example.com",
      ),
    ).toBe(false);

    expect(
      evidenceIdFromHref(
        "https://example.com",
      ),
    ).toBeNull();
  });

  it("does not transform unrelated text", () => {
    const content =
      "No deployment evidence is available.";

    expect(
      transformEvidenceCitations(content),
    ).toBe(content);
  });
});

describe("EvidencePanel", () => {
  it("renders each unique evidence item", () => {
    const evidence =
      createEvidenceReferences(
        "[DEP-EV-001] and [DEP-EV-002]",
      );

    render(
      <EvidencePanel
        evidence={evidence}
      />,
    );

    expect(
      screen.getByRole("heading", {
        name: "Evidence cited",
      }),
    ).toBeInTheDocument();

    expect(
      screen.getByText("DEP-EV-001"),
    ).toBeInTheDocument();

    expect(
      screen.getByText("DEP-EV-002"),
    ).toBeInTheDocument();

    expect(
      screen.getByText("Mock evidence"),
    ).toBeInTheDocument();
  });

  it("does not render when evidence is empty", () => {
    const { container } = render(
      <EvidencePanel evidence={[]} />,
    );

    expect(container).toBeEmptyDOMElement();
  });

  it("labels evidence as demonstration data", () => {
    const evidence =
      createEvidenceReferences(
        "[DEP-EV-001]",
      );

    render(
      <EvidencePanel
        evidence={evidence}
      />,
    );

    expect(
      screen.getByText(
        /deterministic demonstration data/i,
      ),
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "Demonstration only",
      ),
    ).toBeInTheDocument();
  });
});