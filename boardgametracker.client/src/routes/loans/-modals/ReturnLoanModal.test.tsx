import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Loan } from "@/models/Loan/Loan";
import { renderWithProviders, screen, userEvent, waitFor, within } from "@/test/test-utils";
import { ReturnLoanModal } from "./ReturnLoanModal";

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ dateFormat: "yyyy-MM-dd", timeFormat: "HH:mm", uiLanguage: "en-us" }),
	}),
}));

const loan: Loan = {
	id: 7,
	loanDate: new Date(2026, 9, 1),
	dueDate: null,
	returnedDate: null,
	gameId: 3,
	playerId: 4,
	isActive: true,
};

const renderModal = (overrides: Partial<Parameters<typeof ReturnLoanModal>[0]> = {}) => {
	const props = {
		loan,
		gameTitle: "Everdell",
		open: true,
		close: vi.fn(),
		onReturn: vi.fn(() => Promise.resolve()),
		...overrides,
	};
	renderWithProviders(<ReturnLoanModal {...props} />);
	return props;
};

const footer = () => document.querySelector("[data-slot='dialog-footer']") as HTMLElement;

describe("ReturnLoanModal", () => {
	beforeEach(() => {
		vi.useFakeTimers({ toFake: ["Date"] });
		vi.setSystemTime(new Date(2026, 9, 11, 12, 0));
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("puts cancel on the left and the return submit on the right", () => {
		renderModal();

		const buttons = within(footer()).getAllByRole("button");
		expect(buttons.map((button) => button.textContent)).toEqual(["common:cancel", "return.confirm"]);
		expect(buttons[1]).toHaveAttribute("type", "submit");
		expect(buttons[1]).toHaveClass("bg-primary/60");
	});

	it("closes from cancel without returning the loan", async () => {
		const { close, onReturn } = renderModal();

		await userEvent.click(within(footer()).getByRole("button", { name: "common:cancel" }));

		expect(close).toHaveBeenCalledTimes(1);
		expect(onReturn).not.toHaveBeenCalled();
	});

	it("returns the loan today and closes when the footer submit is clicked", async () => {
		const { close, onReturn } = renderModal();

		await userEvent.click(within(footer()).getByRole("button", { name: "return.confirm" }));

		await waitFor(() => expect(onReturn).toHaveBeenCalledWith(7, new Date(2026, 9, 11)));
		await waitFor(() => expect(close).toHaveBeenCalledTimes(1));
	});

	it("does not return anything without a loan", async () => {
		const { onReturn } = renderModal({ loan: null });

		await userEvent.click(within(footer()).getByRole("button", { name: "return.confirm" }));

		await waitFor(() => expect(onReturn).not.toHaveBeenCalled());
		expect(screen.getByText("return.title")).toBeInTheDocument();
	});
});
