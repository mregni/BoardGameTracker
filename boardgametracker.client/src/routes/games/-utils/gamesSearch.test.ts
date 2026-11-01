import { describe, expect, it } from "vitest";
import { type Game, GameState } from "@/models";
import { filterByTitle, ownedPriceStats, parseGamesSearch, parseGamesTableSearch } from "./gamesSearch";

const game = (title: string, state: GameState, buyingPrice: number | null) =>
	({ title, state, buyingPrice }) as unknown as Game;

describe("parseGamesSearch", () => {
	it("keeps the name query and drops invalid filters", () => {
		expect(parseGamesSearch({ q: "cat", players: "0", weight: "extreme" })).toEqual({
			q: "cat",
			category: undefined,
			players: undefined,
			playTime: undefined,
			weight: undefined,
			age: undefined,
		});
	});
});

describe("parseGamesTableSearch", () => {
	it("defaults to any state and reads the table filters", () => {
		expect(parseGamesTableSearch({})).toEqual({
			q: undefined,
			state: undefined,
			language: undefined,
			inStock: undefined,
		});
		expect(parseGamesTableSearch({ state: "owned", language: "nl", inStock: true })).toEqual({
			q: undefined,
			state: GameState.Owned,
			language: "nl",
			inStock: true,
		});
	});

	it("ignores an unknown state", () => {
		expect(parseGamesTableSearch({ state: "lost" }).state).toBeUndefined();
	});
});

describe("filterByTitle", () => {
	it("matches case-insensitively", () => {
		const games = [game("Catan", GameState.Owned, null), game("Azul", GameState.Owned, null)];

		expect(filterByTitle(games, "CAT").map((g) => g.title)).toEqual(["Catan"]);
		expect(filterByTitle(games, undefined)).toHaveLength(2);
	});
});

describe("ownedPriceStats", () => {
	it("only counts priced owned games", () => {
		const stats = ownedPriceStats([
			game("A", GameState.Owned, 40),
			game("B", GameState.Owned, 20),
			game("C", GameState.Owned, null),
			game("D", GameState.Wanted, 100),
		]);

		expect(stats).toEqual({ total: 60, mean: 30, pricedCount: 2 });
	});
});
