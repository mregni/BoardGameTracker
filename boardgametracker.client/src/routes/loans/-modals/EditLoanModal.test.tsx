import { describe, expect, it, vi } from "vitest";
import type { Loan } from "@/models/Loan/Loan";
import { renderWithProviders, screen, userEvent, waitFor, within } from "@/test/test-utils";
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
	const footer = () => document.querySelector("[data-slot='dialog-footer']") as HTMLElement;

	it("puts cancel on the left and the save submit on the right", () => {
		renderWithProviders(<EditLoanModal loan={loan(null)} open close={vi.fn()} onSave={vi.fn()} />);

		const buttons = within(footer()).getAllByRole("button");
		expect(buttons.map((button) => button.textContent)).toEqual(["common:cancel", "edit.save"]);
		expect(buttons[1]).toHaveAttribute("type", "submit");
		expect(buttons[1]).toHaveClass("bg-primary/60");
	});

	it("closes from cancel without saving", async () => {
		const close = vi.fn();
		const onSave = vi.fn();
		renderWithProviders(<EditLoanModal loan={loan(null)} open close={close} onSave={onSave} />);

		await userEvent.click(within(footer()).getByRole("button", { name: "common:cancel" }));

		expect(close).toHaveBeenCalledTimes(1);
		expect(onSave).not.toHaveBeenCalled();
	});

	it("closes after the footer submit saved the loan", async () => {
		const close = vi.fn();
		renderWithProviders(<EditLoanModal loan={loan(null)} open close={close} onSave={() => Promise.resolve()} />);

		await userEvent.click(within(footer()).getByRole("button", { name: "edit.save" }));

		await waitFor(() => expect(close).toHaveBeenCalledTimes(1));
	});

	it("disables both footer buttons when the dialog is disabled", () => {
		renderWithProviders(<EditLoanModal loan={loan(null)} open disabled close={vi.fn()} onSave={vi.fn()} />);

		expect(within(footer()).getByRole("button", { name: "common:cancel" })).toBeDisabled();
		expect(within(footer()).getByRole("button", { name: "edit.save" })).toBeDisabled();
	});

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

	describe("loading the loan's dates", () => {
		const apiLoan = (id: number, loanDate: string, dueDate: string | null, returnedDate: string | null): Loan => ({
			id,
			loanDate: new Date(loanDate),
			dueDate: dueDate ? new Date(dueDate) : null,
			returnedDate: returnedDate ? new Date(returnedDate) : null,
			gameId: 3,
			playerId: 4,
			isActive: returnedDate === null,
		});

		const segments = (name: string) =>
			within(screen.getByRole("group", { name }))
				.getAllByRole("spinbutton")
				.map((segment) => segment.getAttribute("aria-valuenow"));

		const returnedLoan = apiLoan(11, "2026-09-01T10:00:00Z", "2026-09-15T10:00:00Z", "2026-09-10T10:00:00Z");
		const activeLoan = apiLoan(12, "2026-08-03T10:00:00Z", "2026-08-20T10:00:00Z", null);

		it("shows the start, due and returned date of a returned loan", async () => {
			renderWithProviders(<EditLoanModal loan={returnedLoan} open close={vi.fn()} onSave={vi.fn()} />);

			await waitFor(() => expect(segments("new.start.label")).toEqual(["2026", "9", "1"]));
			expect(segments("new.end.label")).toEqual(["2026", "9", "15"]);
			expect(segments("return.date")).toEqual(["2026", "9", "10"]);
		});

		it("accepts the dates as the iso strings the api sends", async () => {
			const raw = {
				...returnedLoan,
				loanDate: "2026-09-01T10:00:00Z",
				dueDate: "2026-09-15T10:00:00Z",
				returnedDate: "2026-09-10T10:00:00Z",
			} as unknown as Loan;
			renderWithProviders(<EditLoanModal loan={raw} open close={vi.fn()} onSave={vi.fn()} />);

			await waitFor(() => expect(segments("new.start.label")).toEqual(["2026", "9", "1"]));
			expect(segments("new.end.label")).toEqual(["2026", "9", "15"]);
			expect(segments("return.date")).toEqual(["2026", "9", "10"]);
		});

		it("shows the dates of the loan it is opened for after another loan was edited", async () => {
			const close = vi.fn();
			const { rerender } = renderWithProviders(
				<EditLoanModal loan={null} open={false} close={close} onSave={vi.fn()} />,
			);

			rerender(<EditLoanModal loan={activeLoan} open close={close} onSave={vi.fn()} />);
			await waitFor(() => expect(segments("new.start.label")).toEqual(["2026", "8", "3"]));
			expect(segments("new.end.label")).toEqual(["2026", "8", "20"]);
			expect(screen.queryByRole("group", { name: "return.date" })).not.toBeInTheDocument();

			rerender(<EditLoanModal loan={activeLoan} open={false} close={close} onSave={vi.fn()} />);
			rerender(<EditLoanModal loan={returnedLoan} open close={close} onSave={vi.fn()} />);

			await waitFor(() => expect(segments("new.start.label")).toEqual(["2026", "9", "1"]));
			expect(segments("new.end.label")).toEqual(["2026", "9", "15"]);
			expect(segments("return.date")).toEqual(["2026", "9", "10"]);
		});

		it("drops unsaved changes when the dialog is closed and opened again", async () => {
			const close = vi.fn();
			const { rerender } = renderWithProviders(
				<EditLoanModal loan={returnedLoan} open close={close} onSave={vi.fn()} />,
			);

			await userEvent.click(screen.getByRole("button", { name: "common:clear-date" }));
			expect(
				within(screen.getByRole("group", { name: "new.end.label" })).getAllByRole("spinbutton")[0],
			).not.toHaveAttribute("aria-valuenow");

			rerender(<EditLoanModal loan={returnedLoan} open={false} close={close} onSave={vi.fn()} />);
			rerender(<EditLoanModal loan={returnedLoan} open close={close} onSave={vi.fn()} />);

			await waitFor(() => expect(segments("new.end.label")).toEqual(["2026", "9", "15"]));
		});
	});
});
