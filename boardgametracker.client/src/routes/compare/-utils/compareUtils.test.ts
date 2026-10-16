import { describe, expect, it } from "vitest";
import { gameRow } from "../-test/compareTestSetup";
import { initialOf, isSide, leaderOf, resolveComparePlayers, sortGames } from "./compareUtils";

describe("resolveComparePlayers", () => {
	const ids = [1, 2, 3, 4];

	it("keeps a valid pair from the URL", () => {
		expect(resolveComparePlayers(ids, 3, 1, [2, 4])).toEqual([3, 1]);
	});

	it("uses the pair with the most games together when the URL has no players", () => {
		expect(resolveComparePlayers(ids, undefined, undefined, [2, 4])).toEqual([2, 4]);
	});

	it("fills only the missing side, preferring the default pair", () => {
		expect(resolveComparePlayers(ids, 2, undefined, [2, 4])).toEqual([2, 4]);
		expect(resolveComparePlayers(ids, 4, undefined, [2, 4])).toEqual([4, 2]);
		expect(resolveComparePlayers(ids, undefined, 2, [2, 4])).toEqual([4, 2]);
		expect(resolveComparePlayers(ids, 3, undefined, [2, 4])).toEqual([3, 2]);
	});

	it("never puts the same player on both sides", () => {
		expect(resolveComparePlayers(ids, 2, 2, [2, 4])).toEqual([2, 4]);
	});

	it("replaces players that no longer exist", () => {
		expect(resolveComparePlayers(ids, 99, 98, [3, 4])).toEqual([3, 4]);
	});

	it("falls back to the first players without a default pair", () => {
		expect(resolveComparePlayers(ids, undefined, undefined)).toEqual([1, 2]);
		expect(resolveComparePlayers(ids, undefined, undefined, [99, 98])).toEqual([1, 2]);
	});

	it("returns no pair with fewer than two players", () => {
		expect(resolveComparePlayers([1], 1, undefined, [])).toEqual([0, 0]);
		expect(resolveComparePlayers([], undefined, undefined, [])).toEqual([0, 0]);
	});
});

describe("leaderOf", () => {
	it("names the side with more wins, or nobody on a tie", () => {
		expect(leaderOf({ playerOne: 23, playerTwo: 17 })).toBe("playerOne");
		expect(leaderOf({ playerOne: 1, playerTwo: 4 })).toBe("playerTwo");
		expect(leaderOf({ playerOne: 3, playerTwo: 3 })).toBeNull();
	});
});

describe("initialOf", () => {
	it("takes the first letter in upper case", () => {
		expect(initialOf("kathleen")).toBe("K");
		expect(initialOf("  Émile")).toBe("É");
		expect(initialOf("")).toBe("?");
	});
});

describe("isSide", () => {
	it("is true only for a player", () => {
		expect(isSide("playerOne")).toBe(true);
		expect(isSide("playerTwo")).toBe(true);
		expect(isSide("both")).toBe(false);
		expect(isSide("other")).toBe(false);
	});
});

describe("sortGames", () => {
	const games = [
		gameRow({ gameId: 1, sessionCount: 11, playerOneWins: 4, playerTwoWins: 7 }),
		gameRow({ gameId: 2, sessionCount: 7, playerOneWins: 5, playerTwoWins: 1 }),
		gameRow({ gameId: 3, sessionCount: 7, playerOneWins: 2, playerTwoWins: 2, lastPlayed: new Date(2026, 9, 12) }),
		gameRow({ gameId: 4, sessionCount: 3, playerOneWins: 0, playerTwoWins: 3 }),
	];

	it("puts the most played first, the most recent first on a tie", () => {
		expect(sortGames(games, "most-played").map((game) => game.gameId)).toEqual([1, 3, 2, 4]);
	});

	it("puts the biggest lead first, whoever has it", () => {
		expect(sortGames(games, "biggest-lead").map((game) => game.gameId)).toEqual([2, 1, 4, 3]);
	});

	it("does not change the list it was given", () => {
		sortGames(games, "biggest-lead");
		expect(games.map((game) => game.gameId)).toEqual([1, 2, 3, 4]);
	});
});
