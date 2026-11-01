import type { FC, ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Game, Player } from "@/models";
import type { Loan } from "@/models/Loan/Loan";
import { renderWithProviders, screen, userEvent, within } from "@/test/test-utils";

interface CapturedRoute {
	component: FC;
	loader: (args: { context: { queryClient: { query: (options: unknown) => Promise<unknown> } } }) => void;
}

const mocks = vi.hoisted(() => ({
	captured: {} as { config: CapturedRoute },
	canWrite: true,
	loans: [] as Loan[],
	isLoading: false,
	deleteLoan: vi.fn(() => Promise.resolve()),
	returnLoans: vi.fn(() => Promise.resolve()),
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
		returnLoans: mocks.returnLoans,
		updateLoan: mocks.updateLoan,
	}),
}));

const games = [
	{ id: 1, title: "Azul", image: null },
	{ id: 2, title: "Everdell", image: null },
	{ id: 3, title: "Catan", image: null },
	{ id: 4, title: "Carcassonne", image: null },
] as unknown as Game[];

const players = [
	{ id: 9, name: "Kathleen", image: null },
	{ id: 8, name: "Ruben", image: null },
] as unknown as Player[];

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
		loans,
		title,
		close,
		onReturn,
	}: {
		open: boolean;
		loans: Loan[];
		title: string;
		close: () => void;
		onReturn: (loanIds: number[], date: Date) => Promise<void>;
	}) =>
		open ? (
			<div>
				<span>{`returning ${title}`}</span>
				<button
					type="button"
					onClick={() =>
						onReturn(
							loans.map((loan) => loan.id),
							new Date(2026, 9, 11),
						)
					}
				>
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

const statistic = (title: string) => screen.getByText(title).closest("button") as HTMLElement;

const openMenu = async (game: string) => {
	const row = screen.getByText(game).closest("li") as HTMLElement;
	await userEvent.click(within(row).getByRole("button", { name: "row.actions" }));
};

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
		const query = vi.fn().mockResolvedValue(undefined);

		mocks.captured.config.loader({ context: { queryClient: { query } } });

		expect(query).toHaveBeenCalledWith({ queryKey: ["loans"] });
	});

	it("shows the loan statistics", () => {
		renderPage();

		expect(within(statistic("statistics.on-loan")).getByText("3")).toBeInTheDocument();
		expect(within(statistic("statistics.overdue")).getByText("1")).toBeInTheDocument();
		expect(within(statistic("statistics.due-this-week")).getByText("1")).toBeInTheDocument();
		expect(screen.queryByText("statistics.returned")).not.toBeInTheDocument();
		expect(within(statistic("statistics.overdue")).getByText("1")).toHaveClass("text-amber-500");
	});

	it("groups the active loans per borrower with the overdue count", () => {
		mocks.loans = [open, loan(5, 4, { playerId: 8 }), overdue];
		renderPage();

		const groups = screen.getAllByRole("region");
		expect(groups.map((group) => within(group).getByRole("heading").textContent)).toEqual(["Kathleen", "Ruben"]);
		expect(within(groups[0]).getByText("group.games")).toBeInTheDocument();
		expect(within(groups[0]).getByText(/group.overdue/)).toBeInTheDocument();
		expect(within(groups[1]).queryByText(/group.overdue/)).not.toBeInTheDocument();
	});

	it("returns every loan of a borrower at once", async () => {
		renderPage();

		await userEvent.click(screen.getByRole("button", { name: "mark-all-returned-button" }));
		expect(screen.getByText("returning return.title-all")).toBeInTheDocument();

		await userEvent.click(screen.getByRole("button", { name: "confirm-return" }));
		expect(mocks.returnLoans).toHaveBeenCalledWith([1, 2, 3], new Date(2026, 9, 11));
	});

	it("offers no bulk return for a borrower with one loan", () => {
		mocks.loans = [overdue];
		renderPage();

		expect(screen.queryByRole("button", { name: "mark-all-returned-button" })).not.toBeInTheDocument();
	});

	it("filters on a statistic and clears the filter when clicked again", async () => {
		renderPage();

		await userEvent.click(statistic("statistics.overdue"));
		expect(statistic("statistics.overdue")).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByText("Azul")).toBeInTheDocument();
		expect(screen.queryByText("Everdell")).not.toBeInTheDocument();

		await userEvent.click(statistic("statistics.overdue"));
		expect(statistic("statistics.on-loan")).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByText("Everdell")).toBeInTheDocument();
	});

	it("shows the active filter and clears it from there", async () => {
		renderPage();

		await userEvent.click(statistic("statistics.overdue"));
		await userEvent.click(screen.getByRole("button", { name: /filter.showing/ }));

		expect(screen.queryByRole("button", { name: /filter.showing/ })).not.toBeInTheDocument();
		expect(screen.getByText("Everdell")).toBeInTheDocument();
	});

	it("filters from the compact chips as well", async () => {
		renderPage();

		await userEvent.click(screen.getByRole("button", { name: "chip.overdue" }));

		expect(screen.getByRole("button", { name: "chip.overdue" })).toHaveAttribute("aria-pressed", "true");
		expect(screen.queryByText("Everdell")).not.toBeInTheDocument();
	});

	it("turns a statistic without loans into a plain card", () => {
		mocks.loans = [open];
		renderPage();

		expect(statistic("statistics.overdue")).toBeNull();
		expect(screen.getByRole("button", { name: "chip.overdue" })).toBeDisabled();
		expect(
			within(screen.getByText("statistics.overdue").parentElement?.parentElement as HTMLElement).getByText("0"),
		).toHaveClass("text-cyan-500");
	});

	it("turns the overdue count red once a loan is more than a month late", () => {
		mocks.loans = [loan(1, 1, { dueDate: new Date(2026, 7, 1) })];
		renderPage();

		expect(within(statistic("statistics.overdue")).getByText("1")).toHaveClass("text-bad");
	});

	it("opens the history when a search only finds returned loans", async () => {
		renderPage();

		await userEvent.type(screen.getByPlaceholderText("search.placeholder"), "carc");

		expect(screen.getByText("no-search-matches")).toBeInTheDocument();
		expect(screen.getByText("history.summary-filtered")).toBeInTheDocument();
		expect(screen.getByRole("link", { name: "Carcassonne" })).toBeInTheDocument();
	});

	it("searches on game and borrower and offers a way back when nothing matches", async () => {
		renderPage();
		const search = screen.getByPlaceholderText("search.placeholder");

		await userEvent.type(search, "ever");
		expect(screen.getByText("Everdell")).toBeInTheDocument();
		expect(screen.queryByText("Azul")).not.toBeInTheDocument();

		await userEvent.clear(search);
		await userEvent.type(search, "kath");
		expect(screen.getByText("Azul")).toBeInTheDocument();

		await userEvent.type(search, "zzz");
		expect(screen.getByText("no-search-matches")).toBeInTheDocument();
		await userEvent.click(screen.getByRole("button", { name: "clear-filter" }));
		expect(screen.getByText("Azul")).toBeInTheDocument();
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
		const hide = screen.getByRole("button", { name: "history.hide" });
		expect(hide).toHaveAttribute("aria-expanded", "true");
		expect(document.getElementById(hide.getAttribute("aria-controls") ?? "")).toContainElement(
			screen.getByText("Carcassonne"),
		);
		expect(screen.getAllByRole("button", { name: /^mark-returned-button/ })).toHaveLength(3);

		await userEvent.click(screen.getByRole("button", { name: "history.hide" }));
		expect(screen.queryByText("Carcassonne")).not.toBeInTheDocument();
	});

	it("still lists loans of games and players it does not know", async () => {
		mocks.loans = [loan(5, 99, { playerId: 98 }), loan(6, 97, { playerId: 96, returnedDate: new Date(2026, 9, 2) })];
		renderPage();
		await userEvent.click(screen.getByRole("button", { name: "history.show" }));

		expect(screen.queryByRole("link")).not.toBeInTheDocument();
		expect(screen.getAllByText("unknown-game")).toHaveLength(2);
		expect(screen.getAllByRole("button", { name: "row.actions" })).toHaveLength(2);
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

		await userEvent.click(screen.getAllByRole("button", { name: /^mark-returned-button/ })[0]);
		expect(screen.getByText("returning return.title")).toBeInTheDocument();

		await userEvent.click(screen.getByRole("button", { name: "confirm-return" }));
		expect(mocks.returnLoans).toHaveBeenCalledWith([1], new Date(2026, 9, 11));

		await userEvent.click(screen.getByRole("button", { name: "close-return" }));
		expect(screen.queryByText("returning return.title")).not.toBeInTheDocument();
	});

	it("edits a returned loan from the history", async () => {
		renderPage();
		await userEvent.click(screen.getByRole("button", { name: "history.show" }));

		await openMenu("Carcassonne");
		await userEvent.click(screen.getByRole("menuitem", { name: "edit.button" }));
		expect(screen.getByText("editing 4")).toBeInTheDocument();

		await userEvent.click(screen.getByRole("button", { name: "save-edit" }));
		expect(mocks.updateLoan).toHaveBeenCalledWith(returned);

		await userEvent.click(screen.getByRole("button", { name: "close-edit" }));
		expect(screen.queryByText("editing 4")).not.toBeInTheDocument();
	});

	it("deletes a loan after confirming", async () => {
		renderPage();

		await openMenu("Everdell");
		await userEvent.click(screen.getByRole("menuitem", { name: "delete.button" }));
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
		expect(screen.queryByRole("button", { name: /^mark-returned-button/ })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "row.actions" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "mark-all-returned-button" })).not.toBeInTheDocument();
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
