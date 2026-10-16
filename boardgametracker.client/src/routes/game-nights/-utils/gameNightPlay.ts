import { addHours, compareAsc, compareDesc, isAfter } from "date-fns";
import { type GameNight, type GameNightDetail, GameNightRsvpState, type Session } from "@/models";
import { sortRsvps } from "@/utils/sortRsvps";

const NIGHT_LENGTH_HOURS = 12;

export const isNightPast = (gameNight: Pick<GameNight, "startDate" | "endedAt">, now: Date = new Date()) =>
	gameNight.endedAt !== null || isAfter(now, addHours(gameNight.startDate, NIGHT_LENGTH_HOURS));

export const splitGameNights = (gameNights: GameNight[], now: Date = new Date()) => ({
	upcoming: gameNights
		.filter((gameNight) => !isNightPast(gameNight, now))
		.sort((a, b) => compareAsc(a.startDate, b.startDate)),
	past: gameNights
		.filter((gameNight) => isNightPast(gameNight, now))
		.sort((a, b) => compareDesc(a.startDate, b.startDate)),
});

export const isNightOver = (detail: GameNightDetail, now: Date = new Date()) =>
	detail.gameNight.endedAt !== null || (detail.runningGames.length === 0 && isNightPast(detail.gameNight, now));

export const attendeeIds = (gameNight: GameNight) =>
	sortRsvps(gameNight.invitedPlayers, gameNight.hostId)
		.filter((rsvp) => rsvp.state === GameNightRsvpState.Accepted)
		.map((rsvp) => rsvp.playerId);

export const nightGameIds = (detail: GameNightDetail) => [
	...new Set([
		...detail.sessions.map((x) => x.gameId),
		...detail.runningGames.map((x) => x.gameId),
		...detail.gameNight.suggestedGames.map((x) => x.id),
	]),
];

export const latestPlays = (sessions: Session[]) =>
	sessions.filter((session, index) => sessions.findIndex((x) => x.gameId === session.gameId) === index);
