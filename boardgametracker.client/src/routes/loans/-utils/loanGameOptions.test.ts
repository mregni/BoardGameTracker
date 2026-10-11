import { describe, expect, it } from "vitest";
import { type Game, GameState, type Player } from "@/models";
import type { Loan } from "@/models/Loan/Loan";
import { buildLoanGameOptions } from "./loanGameOptions";

const game = (id: number, title: string, overrides: Partial<Game> = {}): Game =>
	({ id, title, image: `${title}.png`, state: GameState.Owned, isLoaned: false, ...overrides }) as Game;

const player = (id: number, name: string): Player => ({ id, name }) as Player;

const loan = (gameId: number, playerId: number, returnedDate: Date | null = null): Loan => ({
	id: gameId * 10,
	loanDate: new Date(2026, 9, 1),
	dueDate: null,
	returnedDate,
	gameId,
	playerId,
	isActive: returnedDate === null,
});

describe("buildLoanGameOptions", () => {
	it("lists owned games with the available ones first", () => {
		const options = buildLoanGameOptions(
			[game(1, "Everdell"), game(2, "Everdell Duo"), game(3, "Azul"), game(4, "Wishlist", { state: GameState.Wanted })],
			[loan(1, 7)],
			[player(7, "Kathleen")],
		);

		expect(options).toEqual([
			{ id: 3, title: "Azul", image: "Azul.png", borrower: null, available: true },
			{ id: 2, title: "Everdell Duo", image: "Everdell Duo.png", borrower: null, available: true },
			{ id: 1, title: "Everdell", image: "Everdell.png", borrower: "Kathleen", available: false },
		]);
	});

	it("ignores returned loans", () => {
		const [option] = buildLoanGameOptions(
			[game(1, "Catan")],
			[loan(1, 7, new Date(2026, 9, 5))],
			[player(7, "Kathleen")],
		);

		expect(option.available).toBe(true);
		expect(option.borrower).toBeNull();
	});

	it("keeps a game flagged as loaned unavailable when its loan is unknown", () => {
		const [option] = buildLoanGameOptions([game(1, "Catan", { isLoaned: true })], [], []);

		expect(option).toEqual(expect.objectContaining({ available: false, borrower: null }));
	});
});
