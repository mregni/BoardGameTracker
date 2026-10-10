import { describe, expect, it, vi } from "vitest";
import type { Loan } from "@/models/Loan/Loan";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";
import { EditLoanModal } from "./EditLoanModal";

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ dateFormat: "yyyy-MM-dd", timeFormat: "HH:mm", uiLanguage: "en-us" }),
	}),
}));

const loan = (dueDate: Date | null): Loan => ({
	id: 7,
	loanDate: new Date(2026, 8, 1),
	dueDate,
	returnedDate: null,
	gameId: 3,
	playerId: 4,
	isActive: true,
});

describe("EditLoanModal", () => {
	it("keeps a loan without a due date open-ended when it is saved", async () => {
		const onSave = vi.fn(() => Promise.resolve());
		renderWithProviders(<EditLoanModal loan={loan(null)} open close={vi.fn()} onSave={onSave} />);

		await userEvent.click(screen.getByRole("button", { name: "edit.save" }));

		await waitFor(() => expect(onSave).toHaveBeenCalledTimes(1));
		expect(onSave).toHaveBeenCalledWith(expect.objectContaining({ id: 7, dueDate: null }));
	});

	it("lets the user clear an existing due date", async () => {
		const onSave = vi.fn(() => Promise.resolve());
		renderWithProviders(<EditLoanModal loan={loan(new Date(2026, 8, 20))} open close={vi.fn()} onSave={onSave} />);

		await userEvent.click(screen.getByRole("button", { name: "common:clear-date" }));
		await userEvent.click(screen.getByRole("button", { name: "edit.save" }));

		await waitFor(() => expect(onSave).toHaveBeenCalledTimes(1));
		expect(onSave).toHaveBeenCalledWith(expect.objectContaining({ dueDate: null }));
	});

	it("only offers the returned date for a returned loan", () => {
		renderWithProviders(<EditLoanModal loan={loan(null)} open close={vi.fn()} onSave={vi.fn()} />);

		expect(screen.queryByText("return.date")).not.toBeInTheDocument();
	});

	it("keeps the returned date of a returned loan when it is saved", async () => {
		const onSave = vi.fn(() => Promise.resolve());
		const returnedDate = new Date(2026, 8, 10);
		renderWithProviders(
			<EditLoanModal loan={{ ...loan(null), returnedDate, isActive: false }} open close={vi.fn()} onSave={onSave} />,
		);

		expect(screen.getByText("return.date")).toBeInTheDocument();
		await userEvent.click(screen.getByRole("button", { name: "edit.save" }));

		await waitFor(() => expect(onSave).toHaveBeenCalledTimes(1));
		const saved = (onSave.mock.calls[0] as unknown as [Loan])[0];
		expect(saved.returnedDate?.getDate()).toBe(10);
	});
});
