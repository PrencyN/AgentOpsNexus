import {
  fireEvent,
  render,
  screen,
} from "@testing-library/react";
import {
  afterEach,
  describe,
  expect,
  it,
} from "vitest";
import { MarkdownResponse } from "../components/MarkdownResponse";

describe("MarkdownResponse", () => {
  afterEach(() => {
    document
      .querySelectorAll(
        '[id^="evidence-DEP-EV-"]',
      )
      .forEach((element) => {
        element.remove();
      });
  });

  it("renders Markdown headings and lists", () => {
    render(
      <MarkdownResponse
        content={`
## Summary

Deployment evidence was found.

- First item
- Second item
`}
      />,
    );

    expect(
      screen.getByRole("heading", {
        name: "Summary",
      }),
    ).toBeInTheDocument();

    expect(
      screen.getByText("First item"),
    ).toBeInTheDocument();

    expect(
      screen.getByText("Second item"),
    ).toBeInTheDocument();
  });

  it("renders an evidence reference as a badge", () => {
    render(
      <MarkdownResponse
        content={
          "The deployment is cited by [DEP-EV-001]."
        }
      />,
    );

    expect(
      screen.getByRole("button", {
        name: "Open evidence DEP-EV-001",
      }),
    ).toBeInTheDocument();
  });

  it("handles an evidence ID without brackets", () => {
    render(
      <MarkdownResponse
        content={
          "Evidence DEP-EV-002 supports the hypothesis."
        }
      />,
    );

    expect(
      screen.getByRole("button", {
        name: "Open evidence DEP-EV-002",
      }),
    ).toBeInTheDocument();
  });

  it("does not render content for an empty response", () => {
    const { container } = render(
      <MarkdownResponse content="   " />,
    );

    expect(container).toBeEmptyDOMElement();
  });

  it("handles incomplete streaming Markdown", () => {
    const streamingContent = `## Partial heading

- Incomplete`;

    render(
      <MarkdownResponse
        content={streamingContent}
      />,
    );

    expect(
      screen.getByRole("heading", {
        name: "Partial heading",
      }),
    ).toBeInTheDocument();

    expect(
      screen.getByText("Incomplete"),
    ).toBeInTheDocument();
  });

  it("evidence badge can be activated", () => {
    const target =
      document.createElement("div");

    target.id = "evidence-DEP-EV-001";
    target.tabIndex = -1;

    document.body.appendChild(target);

    render(
      <MarkdownResponse
        content="See [DEP-EV-001]."
      />,
    );

    fireEvent.click(
      screen.getByRole("button", {
        name: "Open evidence DEP-EV-001",
      }),
    );

    expect(document.activeElement).toBe(target);
  });
});