import { describe, expect, it } from "vitest";
import { applyPlayMode, coopDefaults } from "./coopSession";

const players = [
	{ playerId: 1, won: true, firstPlay: true, score: 30 },
	{ playerId: 2, won: false, firstPlay: false, score: 12 },
];

describe("coopDefaults", () => {
	it("starts as co-op only for a game with co-op mode that defaults to it", () => {
		expect(coopDefaults({ hasCoopMode: true, coopByDefault: true }).isCoop).toBe(true);
		expect(coopDefaults({ hasCoopMode: true, coopByDefault: false }).isCoop).toBe(false);
		expect(coopDefaults({ hasCoopMode: false, coopByDefault: true }).isCoop).toBe(false);
		expect(coopDefaults()).toEqual({ isCoop: false, teamWon: null, teamScore: null });
	});
});

describe("applyPlayMode", () => {
	it("gives every player the team result and drops their scores in co-op", () => {
		const result = applyPlayMode({ isCoop: true, teamWon: true, teamScore: 18, playerSessions: players }, true);

		expect(result.teamScore).toBe(18);
		expect(result.playerSessions).toEqual([
			{ playerId: 1, won: true, firstPlay: true, score: null },
			{ playerId: 2, won: true, firstPlay: false, score: null },
		]);
	});

	it("drops the team score for a game without scoring", () => {
		const result = applyPlayMode({ isCoop: true, teamWon: false, teamScore: 18, playerSessions: players }, false);

		expect(result.teamScore).toBeNull();
		expect(result.playerSessions.every((player) => !player.won)).toBe(true);
	});

	it("keeps the player results and clears the team result when not co-op", () => {
		const result = applyPlayMode({ isCoop: false, teamWon: true, teamScore: 18, playerSessions: players }, true);

		expect(result.teamWon).toBeNull();
		expect(result.teamScore).toBeNull();
		expect(result.playerSessions).toEqual(players);
	});

	it("drops the player scores for a game without scoring", () => {
		const result = applyPlayMode({ isCoop: false, teamWon: null, playerSessions: players }, false);

		expect(result.playerSessions.map((player) => player.score)).toEqual([null, null]);
		expect(result.playerSessions.map((player) => player.won)).toEqual([true, false]);
	});
});
