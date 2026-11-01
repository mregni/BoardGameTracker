import { describe, expect, it } from "vitest";
import type { PlayerRival, Session } from "@/models";
import { formOf, nemesisOf, winnerIdsOf } from "./rivalry";

const session = (id: number, day: number, winners: number[], isCoop = false, teamWon: boolean | null = null) =>
	({
		id,
		start: new Date(2026, 0, day),
		isCoop,
		teamWon,
		playerSessions: [1, 2, 3].map((playerId) => ({ playerId, won: winners.includes(playerId) })),
	}) as unknown as Session;

const rival = (playerId: number, sessions: number, wins: number, losses: number): PlayerRival => ({
	playerId,
	sessions,
	wins,
	losses,
});

describe("rivalry", () => {
	it("lists form oldest first and counts co-op team results", () => {
		const form = formOf([session(1, 3, [1]), session(2, 1, [2]), session(3, 2, [], true, true)], 1);

		expect(form).toEqual([
			{ id: 2, won: false },
			{ id: 3, won: true },
			{ id: 1, won: true },
		]);
	});

	it("keeps only the latest sessions in the form", () => {
		const sessions = Array.from({ length: 12 }, (_, index) => session(index + 1, index + 1, [1]));

		expect(formOf(sessions, 1).map((entry) => entry.id)).toEqual([3, 4, 5, 6, 7, 8, 9, 10, 11, 12]);
	});

	it("names the other winners and none for co-op", () => {
		expect(winnerIdsOf(session(1, 1, [2, 3]), 1)).toEqual([2, 3]);
		expect(winnerIdsOf(session(1, 1, [1, 2], true, true), 1)).toEqual([]);
	});

	it("picks the rival with the worst record as nemesis", () => {
		expect(nemesisOf([rival(2, 8, 2, 5), rival(3, 6, 0, 5), rival(4, 2, 0, 2)])).toEqual(rival(3, 6, 0, 5));
	});

	it("has no nemesis when nobody leads", () => {
		expect(nemesisOf([rival(2, 6, 3, 3), rival(3, 2, 0, 2)])).toBeNull();
	});
});
