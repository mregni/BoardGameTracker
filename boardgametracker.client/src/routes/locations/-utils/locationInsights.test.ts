import { describe, expect, it } from "vitest";
import type { Session } from "@/models";
import { mostPlayedGames, topWinners } from "./locationInsights";

const session = (day: number, gameId: number, winners: number[], losers: number[] = []) =>
	({
		start: new Date(2026, 9, day),
		gameId,
		playerSessions: [
			...winners.map((playerId) => ({ playerId, won: true })),
			...losers.map((playerId) => ({ playerId, won: false })),
		],
	}) as unknown as Session;

describe("locationInsights", () => {
	const sessions = [
		session(1, 1, [10], [11]),
		session(2, 2, [11], [10]),
		session(3, 1, [10]),
		session(4, 3, [], [10, 11]),
	];

	it("ranks games by how often they were played, most recent first on ties", () => {
		expect(mostPlayedGames(sessions).top).toEqual([
			{ id: 1, count: 2 },
			{ id: 3, count: 1 },
			{ id: 2, count: 1 },
		]);
	});

	it("ranks players by wins and ignores losses", () => {
		expect(topWinners(sessions).top).toEqual([
			{ id: 10, count: 2 },
			{ id: 11, count: 1 },
		]);
	});

	it("counts what falls outside the limit", () => {
		expect(mostPlayedGames(sessions, 1)).toEqual({ top: [{ id: 1, count: 2 }], more: 2 });
	});
});
