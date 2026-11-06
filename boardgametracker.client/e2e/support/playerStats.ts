import { formatDistanceToNow } from "date-fns";
import badges from "../../public/locales/en-US/badges.json" with { type: "json" };
import { displayDate } from "./format";
import { games, players, sessions } from "./seedData";

type SeedSession = (typeof sessions)[number];

const LEVELS = ["green", "blue", "red", "gold"] as const;
const FORM_LENGTH = 10;
const NEMESIS_MIN_SESSIONS = 3;

export const playerId = (name: string) => players.indexOf(name) + 1;
export const gameId = (title: string) => games.findIndex((game) => game.title === title) + 1;
export const sessionId = (session: SeedSession) => sessions.indexOf(session) + 1;

export const percent = (part: number, total: number) => `${Math.round((part / total) * 100)}%`;

const seated = (session: SeedSession, name: string) => session.scores.some((score) => score.player === name);

export const won = (session: SeedSession, name: string) =>
	session.isCoop ? session.teamWon === true : session.winner === name;

export const sessionsOf = (name: string) =>
	sessions.filter((session) => seated(session, name)).sort((a, b) => b.start.getTime() - a.start.getTime());

const rivalsOf = (name: string) => {
	const competitive = sessionsOf(name).filter((session) => !session.isCoop);
	return players
		.filter((other) => other !== name)
		.map((other) => {
			const together = competitive.filter((session) => seated(session, other));
			return {
				name: other,
				sessions: together.length,
				wins: together.filter((session) => session.winner === name).length,
				losses: together.filter((session) => session.winner === other).length,
			};
		})
		.filter((rival) => rival.sessions > 0)
		.sort((a, b) => b.sessions - a.sessions || playerId(a.name) - playerId(b.name));
};

export type Rival = ReturnType<typeof rivalsOf>[number];

const nemesisOf = (rivals: Rival[]) =>
	rivals
		.filter((rival) => rival.sessions >= NEMESIS_MIN_SESSIONS && rival.losses > rival.wins)
		.sort(
			(a, b) => b.losses - b.wins - (a.losses - a.wins) || b.losses - a.losses || playerId(a.name) - playerId(b.name),
		)[0];

const gameRecordsOf = (name: string) => {
	const played = sessionsOf(name);
	return [...new Set(played.map((session) => session.game))]
		.map((title) => {
			const ofGame = played.filter((session) => session.game === title);
			return {
				title,
				sessions: ofGame.length,
				wins: ofGame.filter((session) => won(session, name)).length,
			};
		})
		.sort((a, b) => b.sessions - a.sessions);
};

const playersMetBy = (name: string) =>
	new Set(sessionsOf(name).flatMap((session) => session.scores.map((score) => score.player))).size - 1;

export const statsOf = (name: string) => {
	const played = sessionsOf(name);
	const coop = played.filter((session) => session.isCoop);
	const rivals = rivalsOf(name);
	return {
		sessions: played.length,
		wins: played.filter((session) => won(session, name)).length,
		minutes: played.reduce((total, session) => total + session.minutes, 0),
		distinctGames: new Set(played.map((session) => session.game)).size,
		coopWon: coop.filter((session) => session.teamWon).length,
		coopLost: coop.filter((session) => !session.teamWon).length,
		rivals,
		nemesis: nemesisOf(rivals),
		gameRecords: gameRecordsOf(name),
		last: played[0],
		form: played
			.slice(0, FORM_LENGTH)
			.reverse()
			.map((session) => (won(session, name) ? "W" : "L")),
		metPlayers: playersMetBy(name),
	};
};

export type PlayerStats = ReturnType<typeof statsOf>;

export const sessionLine = (session: SeedSession, name: string) => {
	const score = session.scores.find((seat) => seat.player === name)?.score;
	const hasWon = won(session, name);
	const details = [session.game, displayDate(session.start)].join(" ");
	const winner = !hasWon && !session.isCoop && session.winner ? ` · ${session.winner} won` : "";
	const note = session.isCoop ? " co-op" : score != null ? ` ${score} points` : "";
	return `${details} · ${session.scores.length} players${winner} ${hasWon ? "Won" : "Lost"}${note}`;
};

export const relativeTime = (date: Date) => {
	const text = formatDistanceToNow(date, { addSuffix: true });
	return /minute/.test(text) ? "(less than a minute|\\d+ minutes?) ago" : text;
};

const milestoneOf = (type: keyof typeof goalsOf, value: number) => {
	const goals = goalsOf[type];
	const earned = goals.filter((goal) => value >= goal).length;
	const levels = badges[type];
	return {
		earned,
		title: levels[LEVELS[Math.max(earned - 1, 0)]].title,
		goal: earned < LEVELS.length ? `Next: ${levels[LEVELS[earned]].description}` : "All levels earned",
	};
};

const goalsOf = {
	"different-games": [3, 10, 20, 50],
	sessions: [5, 10, 50, 100],
	wins: [3, 10, 25, 50],
	duration: [300, 600, 3000, 6000],
	"social-player": [5, 10, 25, 50],
};

export const milestonesOf = (stats: PlayerStats) => [
	milestoneOf("different-games", stats.distinctGames),
	milestoneOf("sessions", stats.sessions),
	milestoneOf("wins", stats.wins),
	milestoneOf("duration", stats.minutes),
	milestoneOf("social-player", stats.metPlayers),
];

const scoredSeats = (session: SeedSession) =>
	session.scores
		.map((seat) => seat.score)
		.filter((score) => score !== null)
		.sort((a, b) => b - a);

const margin = (session: SeedSession, name: string) => {
	const seats = scoredSeats(session);
	const own = session.scores.find((seat) => seat.player === name)?.score ?? 0;
	return session.winner === name ? seats[0] - seats[1] : seats[0] - own;
};

const firstPlays = (name: string) =>
	[...new Set(sessions.filter((session) => seated(session, name)).map((session) => session.game))].map((title) =>
		sessions.find((session) => session.game === title && seated(session, name)),
	);

export const featsOf = (name: string) => {
	const scored = sessionsOf(name).filter((session) => session.winner && scoredSeats(session).length > 1);
	const close = scored.filter((session) => margin(session, name) > 0 && margin(session, name) <= 2);
	const feats: { title: string; earned: boolean }[] = [
		{ title: badges["close-win"].title, earned: close.some((session) => session.winner === name) },
		{ title: badges["close-loss"].title, earned: close.some((session) => session.winner !== name) },
		{ title: badges["first-try"].title, earned: firstPlays(name).some((session) => session && won(session, name)) },
	];
	return feats;
};

export const FEAT_DESCRIPTION = badges["close-win"].description;
export const MILESTONE_TYPES = 8;
export const FEAT_TYPES = 7;
