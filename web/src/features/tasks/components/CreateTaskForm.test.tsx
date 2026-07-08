import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactElement } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { CreateTaskForm } from "./CreateTaskForm";

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn() }),
}));

function renderWithClient(ui: ReactElement) {
  const client = new QueryClient({ defaultOptions: { mutations: { retry: false } } });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

describe("CreateTaskForm", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("renders API field errors inline on a 400 problem+json response", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 400,
        json: async () => ({
          title: "One or more validation errors occurred.",
          status: 400,
          errors: {
            title: ["Title is required and must be 1-200 characters."],
          },
        }),
      }),
    );

    renderWithClient(<CreateTaskForm />);

    await userEvent.type(screen.getByLabelText(/title/i), "x");
    await userEvent.click(screen.getByRole("button", { name: /create/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent(/title is required/i);
  });
});
