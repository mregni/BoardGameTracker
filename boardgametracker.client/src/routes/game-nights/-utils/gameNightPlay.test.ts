import { describe, expect, it } from "vitest";
import { type GameNight, type GameNightDetail, GameNightRsvpState, type Session } from "@/models";
import { attendeeIds, isNightOver, latestEnd, latestPlays, nightGameIds } from "./gameNightPlay";

const session = (id: number, gameId: number, end: Date) =>
	({ id, gameId, start: end, end, minutes: 30, playerSessions: [] }) as unknown as Session;

const gameNight = {
	id: 4,
	hostId: 2,
	startDate: new Date(2026, 9, 16, 20, 0),
	endedAt: null,
	suggestedGames: [{ id: 9 }, { id: 7 }],
	invitedPlayers: [
		{ id: 1, playerId: 5, state: GameNightRsvpState.Accepted, player: { name: "Anna" } },
		{ id: 2, playerId: 2, state: GameNightRsvpState.Accepted, player: { name: "Mikhaël" } },
		{ id: 3, playerId: 6, state: GameNightRsvpState.Declined, player: { name: "Bob" } },
	],
} as unknown as GameNight;

const detail = (sessions: Session[], runningGameIds: number[] = []) =>
	({
		gameNight,
		sessions,
		runningGames: runningGameIds.map((gameId, index) => ({ id: index, gameId })),
	}) as unknown as GameNightDetail;

describe("gameNightPlay", () => {
	it("keeps the night open for twelve hours or while a game runs", () => {
		const late = new Date(2026, 9, 17, 9, 0);

		expect(isNightOver(detail([]), new Date(2026, 9, 17, 1, 0))).toBe(false);
		expect(isNightOver(detail([]), late)).toBe(true);
		expect(isNightOver(detail([], [3]), late)).toBe(false);
	});

	it("is over as soon as the night was ended", () => {
		const ended = { ...detail([]), gameNight: { ...gameNight, endedAt: new Date(2026, 9, 16, 23, 0) } };

		expect(isNightOver(ended, new Date(2026, 9, 16, 23, 5))).toBe(true);
	});

	it("lists the players who are coming with the host first", () => {
		expect(attendeeIds(gameNight)).toEqual([2, 5]);
	});

	it("orders the games played, running and planned without duplicates", () => {
		const played = [session(1, 7, new Date()), session(2, 3, new Date())];

		expect(nightGameIds(detail(played, [8, 3]))).toEqual([7, 3, 8, 9]);
	});

	it("keeps the newest play of every game", () => {
		const newest = session(3, 7, new Date(2026, 9, 16, 23, 0));
		const other = session(2, 3, new Date(2026, 9, 16, 22, 0));
		const oldest = session(1, 7, new Date(2026, 9, 16, 21, 0));

		expect(latestPlays([newest, other, oldest])).toEqual([newest, other]);
	});

	it("finds when the last game ended", () => {
		const end = new Date(2026, 9, 16, 23, 0);

		expect(latestEnd([])).toBeNull();
		expect(latestEnd([session(1, 7, new Date(2026, 9, 16, 21, 0)), session(2, 3, end)])).toEqual(end);
	});
});
