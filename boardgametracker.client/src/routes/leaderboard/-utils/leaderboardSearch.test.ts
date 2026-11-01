import { describe, expect, it } from "vitest";
import { hasActiveFilters, leaderboardSearchSchema, periodRange, toLeaderboardParams } from "./leaderboardSearch";

const today = new Date(2026, 9, 16);

describe("leaderboardSearch", () => {
	describe("toLeaderboardParams", () => {
		it("should use all time, all games, five sessions and wins by default", () => {
			expect(toLeaderboardParams({}, today)).toEqual({ minSessions: 5, rankBy: "wins" });
		});

		it("should start this year on the first of January", () => {
			expect(toLeaderboardParams({ range: "year" }, today)).toEqual({
				from: "2026-01-01",
				minSessions: 5,
				rankBy: "wins",
			});
		});

		it("should go back twelve months for the last 12 months", () => {
			expect(toLeaderboardParams({ range: "12m", game: 4, min: 10, rank: "winRate" }, today)).toEqual({
				from: "2025-10-16",
				gameId: 4,
				minSessions: 10,
				rankBy: "winRate",
			});
		});

		it("should pass the custom dates and ignore them for other periods", () => {
			expect(toLeaderboardParams({ range: "custom", from: "2026-02-01", to: "2026-03-01" }, today)).toMatchObject({
				from: "2026-02-01",
				to: "2026-03-01",
			});
			expect(toLeaderboardParams({ from: "2026-02-01", to: "2026-03-01" }, today)).toEqual({
				minSessions: 5,
				rankBy: "wins",
			});
		});

		it("should use the current date when no date is given", () => {
			expect(toLeaderboardParams({ range: "year" }).from).toMatch(/^\d{4}-01-01$/);
		});
	});

	describe("periodRange", () => {
		it("should swap custom dates that are the wrong way round", () => {
			expect(periodRange({ range: "custom", from: "2026-05-01", to: "2026-04-01" }, today)).toEqual({
				from: "2026-04-01",
				to: "2026-05-01",
			});
		});

		it("should allow an open-ended custom period", () => {
			expect(periodRange({ range: "custom", from: "2026-05-01" }, today)).toEqual({
				from: "2026-05-01",
				to: undefined,
			});
		});
	});

	describe("leaderboardSearchSchema", () => {
		it("should drop values it does not understand", () => {
			expect(
				leaderboardSearchSchema.parse({ range: "week", from: "yesterday", game: -1, min: 0, rank: "podiums" }),
			).toEqual({});
		});

		it("should drop a minimum the filter does not offer", () => {
			expect(leaderboardSearchSchema.parse({ min: 20 })).toEqual({});
		});

		it("should keep valid values", () => {
			const search = { range: "custom", from: "2026-01-01", to: "2026-02-01", game: 3, min: 3, rank: "sessions" };
			expect(leaderboardSearchSchema.parse(search)).toEqual(search);
		});
	});

	describe("hasActiveFilters", () => {
		it("should only count the period and the game", () => {
			expect(hasActiveFilters({})).toBe(false);
			expect(hasActiveFilters({ range: "all", min: 10, rank: "sessions" })).toBe(false);
			expect(hasActiveFilters({ range: "year" })).toBe(true);
			expect(hasActiveFilters({ game: 2 })).toBe(true);
		});
	});
});
