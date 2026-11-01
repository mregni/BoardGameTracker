import { describe, expect, it } from "vitest";
import { type Game, GameState } from "@/models";
import {
	applyGridFilters,
	filterByTitle,
	hasGridFilters,
	ownedPriceStats,
	parseGamesSearch,
	parseGamesTableSearch,
	sortGames,
} from "./gamesSearch";

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

describe("applyGridFilters show", () => {
	const games = [
		{ ...game("Catan", GameState.Owned, null), categories: [] },
		{ ...game("Azul", GameState.Wanted, null), categories: [] },
		{ ...game("Brass", GameState.ForTrade, null), categories: [] },
	] as Game[];

	it("hides wanted games by default", () => {
		expect(applyGridFilters(games, {}).map((g) => g.title)).toEqual(["Catan", "Brass"]);
	});

	it("shows only wanted games or everything on request", () => {
		expect(applyGridFilters(games, { show: "wanted" }).map((g) => g.title)).toEqual(["Azul"]);
		expect(applyGridFilters(games, { show: "all" })).toHaveLength(3);
	});
});

describe("sortGames", () => {
	const withId = (id: number, title: string, additionDate: string | null) =>
		({ id, title, additionDate }) as unknown as Game;
	const games = [withId(1, "Azul", "2024-01-01"), withId(2, "Brass", null), withId(3, "Catan", "2025-06-01")];
	const plays = new Map([
		[1, { playCount: 3, lastPlayed: "2026-01-01" }],
		[2, { playCount: 9, lastPlayed: "2025-01-01" }],
	]);

	it("keeps the incoming name order by default", () => {
		expect(sortGames(games, undefined, plays)).toBe(games);
	});

	it("sorts by plays, last played and addition date, unplayed last", () => {
		expect(sortGames(games, "plays", plays).map((g) => g.id)).toEqual([2, 1, 3]);
		expect(sortGames(games, "recent", plays).map((g) => g.id)).toEqual([1, 2, 3]);
		expect(sortGames(games, "added", plays).map((g) => g.id)).toEqual([3, 1, 2]);
	});
});

describe("hasGridFilters", () => {
	it("does not count sort as a filter", () => {
		expect(hasGridFilters({ sort: "plays" })).toBe(false);
		expect(hasGridFilters({ show: "all" })).toBe(true);
	});
});
