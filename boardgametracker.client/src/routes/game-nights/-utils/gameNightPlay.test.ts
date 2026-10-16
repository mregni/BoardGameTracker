import { describe, expect, it } from "vitest";
import { type GameNight, type GameNightDetail, GameNightRsvpState, type Session } from "@/models";
import { attendeeIds, isNightOver, isNightPast, latestPlays, nightGameIds, splitGameNights } from "./gameNightPlay";

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

	it("counts a night as past once it ended or twelve hours after its start", () => {
		const start = new Date(2026, 9, 16, 20, 0);

		expect(isNightPast({ startDate: start, endedAt: null }, new Date(2026, 9, 16, 22, 0))).toBe(false);
		expect(isNightPast({ startDate: start, endedAt: null }, new Date(2026, 9, 17, 8, 1))).toBe(true);
		expect(isNightPast({ startDate: start, endedAt: new Date(2026, 9, 16, 23, 0) }, new Date(2026, 9, 16, 23, 5))).toBe(
			true,
		);
	});

	it("keeps started nights upcoming and orders both lists", () => {
		const night = (id: number, startDate: Date, endedAt: Date | null = null) =>
			({ id, startDate, endedAt }) as unknown as GameNight;
		const now = new Date(2026, 9, 16, 22, 0);
		const started = night(1, new Date(2026, 9, 16, 20, 0));
		const nextWeek = night(2, new Date(2026, 9, 23, 20, 0));
		const lastWeek = night(3, new Date(2026, 9, 9, 20, 0));
		const yesterday = night(4, new Date(2026, 9, 15, 20, 0));
		const endedTonight = night(5, new Date(2026, 9, 16, 18, 0), new Date(2026, 9, 16, 21, 0));

		const { upcoming, past } = splitGameNights([nextWeek, lastWeek, started, endedTonight, yesterday], now);

		expect(upcoming.map((x) => x.id)).toEqual([1, 2]);
		expect(past.map((x) => x.id)).toEqual([5, 4, 3]);
	});
});
