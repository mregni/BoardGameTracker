import { describe, expect, it } from "vitest";
import { type Game, GameState } from "@/models";
import {
	filterByTitle,
	freshWeight,
	hasActiveFilters,
	ownedPriceStats,
	parseGamesSearch,
	parseIds,
	pickWheelGames,
	sortGames,
} from "./gamesSearch";

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

	it("only accepts player counts and play times the filters offer", () => {
		expect(parseGamesSearch({ players: "4", playTime: "15" })).toMatchObject({ players: 4, playTime: undefined });
		expect(parseGamesSearch({ players: "12", playTime: "60" })).toMatchObject({ players: undefined, playTime: 60 });
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
			game("E", GameState.Owned, 0),
			game("D", GameState.Wanted, 100),
		]);

		expect(stats).toEqual({ total: 60, mean: 30, pricedCount: 2 });
	});
});

describe("pickWheelGames", () => {
	const candidate = (title: string, state: GameState, isLoaned = false) =>
		({ title, state, isLoaned }) as unknown as Game;

	it("never puts a lent, wanted or sold game on the wheel", () => {
		const games = [
			candidate("Wanted", GameState.Wanted),
			candidate("Lent", GameState.Owned, true),
			candidate("Sold", GameState.PreviouslyOwned),
			candidate("Azul", GameState.Owned),
			candidate("Catan", GameState.Owned),
		];

		expect(
			pickWheelGames(games)
				.map((g) => g.title)
				.sort(),
		).toEqual(["Azul", "Catan"]);
	});

	it("caps the wheel at the given number of games", () => {
		const games = Array.from({ length: 15 }, (_, i) => candidate(`Game ${i}`, GameState.Owned));

		expect(pickWheelGames(games, 10)).toHaveLength(10);
	});

	it("gives heavier games the better draw", () => {
		const games = [candidate("Played", GameState.Owned), candidate("Unplayed", GameState.Owned)];

		const picked = pickWheelGames(
			games,
			1,
			() => 0.5,
			(g) => (g.title === "Unplayed" ? 3 : 1),
		);

		expect(picked.map((g) => g.title)).toEqual(["Unplayed"]);
	});
});

describe("freshWeight", () => {
	const now = new Date("2026-11-01").getTime();
	const played = (playCount: number, lastPlayed?: string) => ({ playCount, lastPlayed }) as unknown as Game;

	it("weights never played over long unplayed over recently played", () => {
		expect(freshWeight(played(0), now)).toBe(3);
		expect(freshWeight(played(4, "2026-01-01"), now)).toBe(2);
		expect(freshWeight(played(4, "2026-10-20"), now)).toBe(1);
	});
});

describe("parseIds", () => {
	it("keeps positive whole ids and drops everything else", () => {
		expect(parseIds([3, "7", -1, 2.5, "x"])).toEqual([3, 7]);
		expect(parseIds("3,7")).toBeUndefined();
		expect(parseIds([])).toBeUndefined();
	});
});
