import { beforeEach, describe, expect, it, vi } from "vitest";
import { type Game, GameState, type Player } from "@/models";
import type { Loan } from "@/models/Loan/Loan";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";
import NewLoanModal from "./NewLoanModal";

const gameButton = (title: string) =>
	screen.getByText(title, { selector: "span.truncate" }).closest("button") as HTMLElement;

const { saveLoan } = vi.hoisted(() => ({ saveLoan: vi.fn() }));

const games = [
	{ id: 1, title: "Everdell", image: null, state: GameState.Owned, isLoaned: true },
	{ id: 2, title: "Everdell Duo", image: null, state: GameState.Owned, isLoaned: false },
] as unknown as Game[];

const players = [{ id: 7, name: "Kathleen", image: null }] as unknown as Player[];

const loans: Loan[] = [
	{
		id: 3,
		loanDate: new Date(2026, 9, 1),
		dueDate: null,
		returnedDate: null,
		gameId: 1,
		playerId: 7,
		isActive: true,
	},
];

vi.mock("../-hooks/useNewLoanModal", () => ({
	useNewLoanModal: () => ({ games, players, loans, isLoading: false, saveLoan }),
}));

vi.mock("@/routes/players/-modals/CreatePlayerModal", () => ({
	CreatePlayerModal: () => null,
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ dateFormat: "yyyy-MM-dd", timeFormat: "HH:mm", uiLanguage: "en-us" }),
	}),
}));

describe("NewLoanModal", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("shows a game that is on loan as unavailable", () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);

		expect(gameButton("Everdell")).toBeDisabled();
		expect(gameButton("Everdell")).toHaveTextContent("new.game.on-loan-to");
		expect(gameButton("Everdell Duo")).toBeEnabled();
	});

	it("names the picked game on the submit button", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);

		expect(screen.getByRole("button", { name: "new.save" })).toBeInTheDocument();
		await userEvent.click(gameButton("Everdell Duo"));

		expect(gameButton("Everdell Duo")).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByRole("button", { name: "new.lend-game" })).toBeInTheDocument();
	});

	it("defaults to a two week due date and switches between presets", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);

		expect(screen.getByRole("button", { name: /^new\.due\.two-weeks · / })).toHaveAttribute("aria-pressed", "true");

		await userEvent.click(screen.getByRole("button", { name: "new.due.none" }));

		expect(screen.getByRole("button", { name: "new.due.none" })).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByRole("button", { name: "new.due.two-weeks" })).toHaveAttribute("aria-pressed", "false");
	});

	it("only asks for a date when pick date is chosen", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);
		const datePickers = () => screen.getAllByRole("group").length;
		const before = datePickers();

		await userEvent.click(screen.getByRole("button", { name: "new.due.custom" }));

		await waitFor(() => expect(datePickers()).toBeGreaterThan(before));
	});

	it("does not save without a game and a borrower", async () => {
		renderWithProviders(<NewLoanModal open close={vi.fn()} />);

		await userEvent.click(screen.getByRole("button", { name: "new.save" }));

		await waitFor(() => expect(saveLoan).not.toHaveBeenCalled());
	});
});
