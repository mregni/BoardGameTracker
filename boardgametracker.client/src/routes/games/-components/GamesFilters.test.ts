import { describe, expect, it } from "vitest";
import type { Game } from "@/models";
import { filterGames } from "./GamesFilters";

const game = (minAge: number | null): Game => ({ minAge }) as unknown as Game;

describe("filterGames age filter", () => {
	const games = [game(null), game(5), game(6), game(7), game(9), game(10), game(12), game(13), game(14)];

	it("bucket 0-6 matches min age up to 6", () => {
		expect(filterGames(games, { age: "0-6" }).map((g) => g.minAge)).toEqual([5, 6]);
	});

	it("bucket 7-9 matches min age 7 to 9", () => {
		expect(filterGames(games, { age: "7-9" }).map((g) => g.minAge)).toEqual([7, 9]);
	});

	it("bucket 10-12 matches min age 10 to 12", () => {
		expect(filterGames(games, { age: "10-12" }).map((g) => g.minAge)).toEqual([10, 12]);
	});

	it("bucket 13plus matches min age 13 and up", () => {
		expect(filterGames(games, { age: "13plus" }).map((g) => g.minAge)).toEqual([13, 14]);
	});

	it("excludes games with no min age from every bucket", () => {
		for (const age of ["0-6", "7-9", "10-12", "13plus"] as const) {
			expect(filterGames(games, { age }).some((g) => g.minAge === null)).toBe(false);
		}
	});

	it("returns everything when no age bucket is selected", () => {
		expect(filterGames(games, {})).toHaveLength(games.length);
	});
});
