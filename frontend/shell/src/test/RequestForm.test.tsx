import {
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { RequestForm } from "../components/RequestForm";

describe("RequestForm", () => {
  it("shows validation for a short incident description", async () => {
    const submit = vi.fn().mockResolvedValue(undefined);

    render(
      <RequestForm
        busy={false}
        onSubmit={submit}
        onCancel={vi.fn()}
      />,
    );

    const message = screen.getByLabelText(
      "Incident description",
    );

    fireEvent.change(message, {
      target: { value: "short" },
    });

    fireEvent.click(
      screen.getByRole("button", {
        name: "Start investigation",
      }),
    );

    expect(
      await screen.findByText("Enter at least 10 characters."),
    ).toBeInTheDocument();

    expect(submit).not.toHaveBeenCalled();
  });

  it("submits a valid request", async () => {
    const submit = vi.fn().mockResolvedValue(undefined);

    render(
      <RequestForm
        busy={false}
        onSubmit={submit}
        onCancel={vi.fn()}
      />,
    );

    fireEvent.change(
      screen.getByLabelText("Incident description"),
      {
        target: {
          value:
            "Checkout errors increased after the latest deployment.",
        },
      },
    );

    fireEvent.click(
      screen.getByRole("button", {
        name: "Start investigation",
      }),
    );

    await waitFor(() => {
      expect(submit).toHaveBeenCalledTimes(1);
    });

    expect(submit).toHaveBeenCalledWith(
      expect.objectContaining({
        environment: "Development",
        severity: "High",
      }),
      expect.anything(),
    );
  });

  it("disables submission when busy", () => {
    render(
      <RequestForm
        busy
        onSubmit={vi.fn()}
        onCancel={vi.fn()}
      />,
    );

    expect(
      screen.getByRole("button", {
        name: "Investigation running...",
      }),
    ).toBeDisabled();

    expect(
      screen.getByRole("button", {
        name: "Cancel",
      }),
    ).toBeInTheDocument();
  });
});