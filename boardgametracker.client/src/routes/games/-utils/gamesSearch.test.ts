import { describe, expect, it } from "vitest";
import { type Game, GameState } from "@/models";
import { filterByTitle, hasActiveFilters, ownedPriceStats, parseGamesSearch, sortGames } from "./gamesSearch";

const game = (title: string, state: GameState, buyingPrice: number | null) =>
	({ title, state, buyingPrice }) as unknown as Game;

describe("parseGamesSearch", () => {
	it("keeps the name query and drops invalid filters", () => {
		expect(parseGamesSearch({ q: "cat", players: "0", weight: "extreme", state: "lost", sort: "random" })).toEqual({
			view: undefined,
			q: "cat",
			category: undefined,
			players: undefined,
			playTime: undefined,
			weight: undefined,
			age: undefined,
			state: undefined,
			language: undefined,
			inStock: undefined,
			sort: undefined,
		});
	});

	it("reads the view, the table filters and the sort", () => {
		expect(
			parseGamesSearch({ view: "table", state: "owned", language: "nl", inStock: "true", sort: "plays" }),
		).toMatchObject({ view: "table", state: GameState.Owned, language: "nl", inStock: true, sort: "plays" });
	});

	it("drops the default title sort so the URL stays clean", () => {
		expect(parseGamesSearch({ sort: "title" }).sort).toBeUndefined();
	});
});

describe("hasActiveFilters", () => {
	it("ignores the view and the sort", () => {
		expect(hasActiveFilters({ view: "table", sort: "plays" })).toBe(false);
		expect(hasActiveFilters({ view: "table", state: GameState.Owned })).toBe(true);
	});
});

describe("sortGames", () => {
	const played = (title: string, playCount: number, lastPlayed: Date | null) =>
		({ title, playCount, lastPlayed }) as unknown as Game;
	const games = [
		played("Brass", 2, new Date(2026, 0, 1)),
		played("Azul", 5, new Date(2025, 0, 1)),
		played("Catan", 0, null),
		played("Agricola", 2, new Date(2026, 5, 1)),
	];

	it("sorts by title by default", () => {
		expect(sortGames(games, undefined).map((g) => g.title)).toEqual(["Agricola", "Azul", "Brass", "Catan"]);
	});

	it("puts the most played first and breaks ties by title", () => {
		expect(sortGames(games, "plays").map((g) => g.title)).toEqual(["Azul", "Agricola", "Brass", "Catan"]);
	});

	it("puts the most recently played first and never-played games last", () => {
		expect(sortGames(games, "last-played").map((g) => g.title)).toEqual(["Agricola", "Brass", "Azul", "Catan"]);
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
