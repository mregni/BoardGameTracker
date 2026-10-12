import type { FC, ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Game, Player } from "@/models";
import type { Loan } from "@/models/Loan/Loan";
import { renderWithProviders, screen, userEvent, within } from "@/test/test-utils";

interface CapturedRoute {
	component: FC;
	loader: (args: { context: { queryClient: { prefetchQuery: (options: unknown) => void } } }) => void;
}

const mocks = vi.hoisted(() => ({
	captured: {} as { config: CapturedRoute },
	canWrite: true,
	loans: [] as Loan[],
	isLoading: false,
	deleteLoan: vi.fn(() => Promise.resolve()),
	returnLoan: vi.fn(() => Promise.resolve()),
	updateLoan: vi.fn(() => Promise.resolve()),
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return config;
	},
	Link: ({ children, className }: { children: ReactNode; className?: string }) => (
		<a className={className} href="/">
			{children}
		</a>
	),
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canWrite: mocks.canWrite }),
}));

vi.mock("@/services/queries/loans", () => ({
	getLoans: () => ({ queryKey: ["loans"] }),
}));

vi.mock("@/routes/loans/-hooks/useLoans", () => ({
	useLoans: () => ({
		loans: mocks.loans,
		settings: { dateFormat: "dd-MM-yyyy" },
		isLoading: mocks.isLoading,
		deleteLoan: mocks.deleteLoan,
		returnLoan: mocks.returnLoan,
		updateLoan: mocks.updateLoan,
	}),
}));

const games = [
	{ id: 1, title: "Azul", image: null },
	{ id: 2, title: "Everdell", image: null },
	{ id: 3, title: "Catan", image: null },
	{ id: 4, title: "Carcassonne", image: null },
] as unknown as Game[];

const players = [{ id: 9, name: "Kathleen", image: null }] as unknown as Player[];

vi.mock("@/routes/-hooks/useGameById", () => ({
	useGameById: () => ({ gameById: (id: number) => games.find((game) => game.id === id) ?? null }),
}));

vi.mock("@/routes/-hooks/usePlayerById", () => ({
	usePlayerById: () => ({ playerById: (id: number) => players.find((player) => player.id === id) ?? null }),
}));

vi.mock("@/routes/loans/-modals/NewLoanModal", () => ({
	default: ({ open, close }: { open: boolean; close: () => void }) =>
		open ? (
			<button type="button" onClick={close}>
				close-new-loan
			</button>
		) : null,
}));

vi.mock("@/routes/loans/-modals/EditLoanModal", () => ({
	EditLoanModal: ({
		open,
		loan,
		close,
		onSave,
	}: {
		open: boolean;
		loan: Loan | null;
		close: () => void;
		onSave: (loan: Loan) => Promise<void>;
	}) =>
		open && loan ? (
			<div>
				<span>{`editing ${loan.id}`}</span>
				<button type="button" onClick={() => onSave(loan)}>
					save-edit
				</button>
				<button type="button" onClick={close}>
					close-edit
				</button>
			</div>
		) : null,
}));

vi.mock("@/routes/loans/-modals/ReturnLoanModal", () => ({
	ReturnLoanModal: ({
		open,
		loan,
		gameTitle,
		close,
		onReturn,
	}: {
		open: boolean;
		loan: Loan | null;
		gameTitle: string;
		close: () => void;
		onReturn: (loanId: number, date: Date) => Promise<void>;
	}) =>
		open && loan ? (
			<div>
				<span>{`returning ${gameTitle}`}</span>
				<button type="button" onClick={() => onReturn(loan.id, new Date(2026, 9, 11))}>
					confirm-return
				</button>
				<button type="button" onClick={close}>
					close-return
				</button>
			</div>
		) : null,
}));

vi.mock("@/routes/-modals/BgtDeleteModal", () => ({
	BgtDeleteModal: ({ open, heading, onDelete }: { open: boolean; heading: string; onDelete: () => void }) =>
		open ? (
			<div>
				<span>{heading}</span>
				<button type="button" onClick={onDelete}>
					confirm-delete
				</button>
			</div>
		) : null,
}));

await import("./index");

const loan = (id: number, gameId: number, overrides: Partial<Loan> = {}): Loan => ({
	id,
	loanDate: new Date(2026, 8, 26),
	dueDate: null,
	returnedDate: null,
	gameId,
	playerId: 9,
	isActive: true,
	...overrides,
});

const overdue = loan(1, 1, { dueDate: new Date(2026, 9, 8) });
const dueSoon = loan(2, 2, { dueDate: new Date(2026, 9, 15) });
const open = loan(3, 3);
const returned = loan(4, 4, { returnedDate: new Date(2026, 9, 1), isActive: false });

const renderPage = () => {
	const Page = mocks.captured.config.component;
	return renderWithProviders(<Page />);
};

const statistic = (title: string) => screen.getByText(title).closest("div.col-span-1") as HTMLElement;

describe("loans page", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		vi.useFakeTimers({ toFake: ["Date"] });
		vi.setSystemTime(new Date(2026, 9, 11, 12, 0));
		mocks.canWrite = true;
		mocks.isLoading = false;
		mocks.loans = [open, returned, dueSoon, overdue];
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("prefetches the loans in the loader", () => {
		const prefetchQuery = vi.fn();

		mocks.captured.config.loader({ context: { queryClient: { prefetchQuery } } });

		expect(prefetchQuery).toHaveBeenCalledWith({ queryKey: ["loans"] });
	});

	it("shows the loan statistics", () => {
		renderPage();

		expect(within(statistic("statistics.on-loan")).getByText("3")).toBeInTheDocument();
		expect(within(statistic("statistics.overdue")).getByText("1")).toBeInTheDocument();
		expect(within(statistic("statistics.due-this-week")).getByText("1")).toBeInTheDocument();
		expect(within(statistic("statistics.returned")).getByText("1")).toBeInTheDocument();
	});

	it("lists the active loans by due date and keeps the history closed", () => {
		renderPage();

		const titles = screen.getAllByRole("link", { name: /Azul|Everdell|Catan|Carcassonne/ }).map((x) => x.textContent);
		expect(titles).toEqual(["Azul", "Everdell", "Catan"]);
		expect(screen.getByText("history.summary")).toBeInTheDocument();
		expect(screen.queryByText("Carcassonne")).not.toBeInTheDocument();
	});

	it("opens and closes the history", async () => {
		renderPage();

		const toggle = screen.getByRole("button", { name: "history.show" });
		expect(toggle).toHaveAttribute("aria-expanded", "false");
		await userEvent.click(toggle);

		expect(screen.getByText("Carcassonne")).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "history.hide" })).toHaveAttribute("aria-expanded", "true");
		expect(screen.getAllByRole("button", { name: "returned-button" })).toHaveLength(3);

		await userEvent.click(screen.getByRole("button", { name: "history.hide" }));
		expect(screen.queryByText("Carcassonne")).not.toBeInTheDocument();
	});

	it("still lists loans of games and players it does not know", async () => {
		mocks.loans = [loan(5, 99, { playerId: 98 }), loan(6, 97, { playerId: 96, returnedDate: new Date(2026, 9, 2) })];
		renderPage();
		await userEvent.click(screen.getByRole("button", { name: "history.show" }));

		expect(screen.queryByRole("link")).not.toBeInTheDocument();
		expect(screen.getAllByRole("button", { name: "edit.button" })).toHaveLength(2);
	});

	it("says there are no active loans when everything was returned", () => {
		mocks.loans = [returned];
		renderPage();

		expect(screen.getByText("no-active-loans")).toBeInTheDocument();
	});

	it("opens the lend dialog from the header", async () => {
		renderPage();

		await userEvent.click(screen.getByRole("button", { name: "loans:new.button" }));
		expect(screen.getByRole("button", { name: "close-new-loan" })).toBeInTheDocument();

		await userEvent.click(screen.getByRole("button", { name: "close-new-loan" }));
		expect(screen.queryByRole("button", { name: "close-new-loan" })).not.toBeInTheDocument();
	});

	it("returns a loan through the return dialog", async () => {
		renderPage();

		await userEvent.click(screen.getAllByRole("button", { name: "returned-button" })[0]);
		expect(screen.getByText("returning Azul")).toBeInTheDocument();

		await userEvent.click(screen.getByRole("button", { name: "confirm-return" }));
		expect(mocks.returnLoan).toHaveBeenCalledWith(1, new Date(2026, 9, 11));

		await userEvent.click(screen.getByRole("button", { name: "close-return" }));
		expect(screen.queryByText("returning Azul")).not.toBeInTheDocument();
	});

	it("edits a returned loan from the history", async () => {
		renderPage();
		await userEvent.click(screen.getByRole("button", { name: "history.show" }));

		const editButtons = screen.getAllByRole("button", { name: "edit.button" });
		await userEvent.click(editButtons[editButtons.length - 1]);
		expect(screen.getByText("editing 4")).toBeInTheDocument();

		await userEvent.click(screen.getByRole("button", { name: "save-edit" }));
		expect(mocks.updateLoan).toHaveBeenCalledWith(returned);

		await userEvent.click(screen.getByRole("button", { name: "close-edit" }));
		expect(screen.queryByText("editing 4")).not.toBeInTheDocument();
	});

	it("deletes a loan after confirming", async () => {
		renderPage();

		await userEvent.click(screen.getAllByRole("button", { name: "delete.button" })[1]);
		expect(screen.getByText("delete.title")).toBeInTheDocument();

		await userEvent.click(screen.getByRole("button", { name: "confirm-delete" }));
		expect(mocks.deleteLoan).toHaveBeenCalledWith(2);
		expect(screen.queryByRole("button", { name: "confirm-delete" })).not.toBeInTheDocument();
	});

	it("hides every action without write access", async () => {
		mocks.canWrite = false;
		renderPage();
		await userEvent.click(screen.getByRole("button", { name: "history.show" }));

		expect(screen.queryByRole("button", { name: "loans:new.button" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "returned-button" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "edit.button" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "delete.button" })).not.toBeInTheDocument();
	});

	describe("without loans", () => {
		beforeEach(() => {
			mocks.loans = [];
		});

		it("shows the empty state with a way to lend a game", async () => {
			renderPage();

			expect(screen.getByText("empty.title")).toBeInTheDocument();
			await userEvent.click(screen.getByRole("button", { name: "new.button" }));

			expect(screen.getByRole("button", { name: "close-new-loan" })).toBeInTheDocument();
		});

		it("shows the empty state without the action for read-only users", () => {
			mocks.canWrite = false;
			renderPage();

			expect(screen.getByText("empty.title")).toBeInTheDocument();
			expect(screen.queryByRole("button", { name: "new.button" })).not.toBeInTheDocument();
		});
	});
});
