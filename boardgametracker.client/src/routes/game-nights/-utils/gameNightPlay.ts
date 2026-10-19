import { addHours, compareAsc, compareDesc, isAfter, isBefore, subHours } from "date-fns";
import { type GameNight, type GameNightDetail, GameNightRsvpState, type RunningGame, type Session } from "@/models";
import { sortRsvps } from "@/utils/sortRsvps";

const NIGHT_LENGTH_HOURS = 12;
export const PLAY_OPENS_HOURS_BEFORE = 12;

type NightTiming = Pick<GameNight, "startDate"> & { endedAt?: Date | null };

export const isNightPast = (gameNight: NightTiming, now: Date = new Date()) =>
	(gameNight.endedAt ?? null) !== null || isAfter(now, addHours(gameNight.startDate, NIGHT_LENGTH_HOURS));

export const hasPlayOpened = (gameNight: Pick<GameNight, "startDate">, now: Date = new Date()) =>
	!isBefore(now, subHours(gameNight.startDate, PLAY_OPENS_HOURS_BEFORE));

export const isNightInProgress = (gameNight: NightTiming, now: Date = new Date()) =>
	!isNightPast(gameNight, now) && !isBefore(now, gameNight.startDate);

export const isInsidePlayWindow = (gameNight: Pick<GameNight, "startDate">, now: Date = new Date()) =>
	isBefore(now, addHours(gameNight.startDate, NIGHT_LENGTH_HOURS));

export const splitGameNights = (gameNights: GameNight[], now: Date = new Date()) => ({
	live: gameNights
		.filter((gameNight) => isNightInProgress(gameNight, now))
		.sort((a, b) => compareAsc(a.startDate, b.startDate)),
	upcoming: gameNights
		.filter((gameNight) => !isNightPast(gameNight, now) && !isNightInProgress(gameNight, now))
		.sort((a, b) => compareAsc(a.startDate, b.startDate)),
	past: gameNights
		.filter((gameNight) => isNightPast(gameNight, now))
		.sort((a, b) => compareDesc(a.startDate, b.startDate)),
});

export const isNightOver = (detail: GameNightDetail, now: Date = new Date()) =>
	detail.gameNight.endedAt !== null || (detail.runningGames.length === 0 && isNightPast(detail.gameNight, now));

export const comingIds = (gameNight: GameNight) =>
	sortRsvps(gameNight.invitedPlayers, gameNight.hostId)
		.filter((rsvp) => rsvp.state === GameNightRsvpState.Accepted)
		.map((rsvp) => rsvp.playerId);

export const nightTable = (detail: GameNightDetail) => {
	const expected =
		!detail.gameNight.summary.attendanceRecorded && detail.sessions.length === 0 && detail.runningGames.length === 0;
	return {
		expected,
		playerIds: expected ? comingIds(detail.gameNight) : detail.gameNight.summary.attendeeIds,
	};
};

export const playedPlayerIds = (detail: GameNightDetail) => [
	...new Set([
		...detail.sessions.flatMap((session) => session.playerSessions.map((x) => x.playerId)),
		...detail.runningGames.flatMap((runningGame) => runningGame.playerIds),
	]),
];

export const busyPlayers = (runningGames: RunningGame[]) => {
	const busy = new Map<number, number>();
	for (const runningGame of runningGames) {
		for (const playerId of runningGame.playerIds) {
			if (!busy.has(playerId)) busy.set(playerId, runningGame.gameId);
		}
	}
	return busy;
};

export const freePlayerIds = (playerIds: number[], runningGames: RunningGame[]) => {
	const busy = busyPlayers(runningGames);
	return playerIds.filter((id) => !busy.has(id));
};

export const nightGameIds = (detail: GameNightDetail) => [
	...new Set([
		...detail.sessions.map((x) => x.gameId),
		...detail.runningGames.map((x) => x.gameId),
		...detail.gameNight.suggestedGames.map((x) => x.id),
	]),
];

export const latestPlays = (sessions: Session[]) =>
	sessions.filter((session, index) => sessions.findIndex((x) => x.gameId === session.gameId) === index);
