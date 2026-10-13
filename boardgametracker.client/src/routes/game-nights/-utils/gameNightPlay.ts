import { addHours, isAfter, max } from "date-fns";
import { type GameNight, type GameNightDetail, GameNightRsvpState, type Session } from "@/models";
import { sortRsvps } from "@/utils/sortRsvps";

const NIGHT_LENGTH_HOURS = 12;

export const isNightOver = (detail: GameNightDetail, now: Date = new Date()) =>
	detail.runningGames.length === 0 && isAfter(now, addHours(detail.gameNight.startDate, NIGHT_LENGTH_HOURS));

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

export const latestEnd = (sessions: Session[]) => (sessions.length === 0 ? null : max(sessions.map((x) => x.end)));
