import { duration } from "./format";
import { games, players, sessions } from "./seedData";

type Session = (typeof sessions)[number];
export type Outcome = "playerOne" | "playerTwo" | "other" | "sameTeam";

export const MIN_SESSIONS_FOR_AVERAGE = 3;
const MIN_PLAYERS_FOR_PODIUM = 4;
const PODIUM_PLACES = 3;
const BEST_GAMES = 3;
const MIN_STREAK = 2;
const RECENT_SESSIONS = 10;

export const playerId = (name: string) => players.indexOf(name) + 1;
export const gameId = (title: string) => games.findIndex((game) => game.title === title) + 1;
export const sessionId = (session: Session) => sessions.indexOf(session) + 1;

const percentFormat = new Intl.NumberFormat("en-US", { style: "percent", maximumFractionDigits: 0 });
export const percent = (fraction: number) => percentFormat.format(fraction);

const oneDecimal = new Intl.NumberFormat("en-US", { maximumFractionDigits: 1 });

const roundedToEven = (value: number) => {
	const scaled = value * 10;
	const isMidpoint = Math.abs(scaled - Math.trunc(scaled)) === 0.5;
	return (isMidpoint ? 2 * Math.round(scaled / 2) : Math.round(scaled)) / 10;
};

const sum = (values: number[]) => values.reduce((total, value) => total + value, 0);
const count = (subset: Session[], condition: (session: Session) => boolean) => subset.filter(condition).length;
const newestFirst = (subset: Session[]) => [...subset].sort((a, b) => b.start.getTime() - a.start.getTime());

const hasScoring = (session: Session) => games.find((game) => game.title === session.game)?.hasScoring === true;
const played = (session: Session, name: string) => session.scores.some((score) => score.player === name);
const won = (session: Session, name: string) =>
	played(session, name) && (session.winner === name || session.teamWon === true);
const scoreOf = (session: Session, name: string) =>
	session.scores.find((score) => score.player === name)?.score ?? null;

export const totals = (name: string, subset: Session[] = sessions) => {
	const own = subset.filter((session) => played(session, name));
	const wins = count(own, (session) => won(session, name));
	return { wins, sessions: own.length, winRate: own.length === 0 ? 0 : wins / own.length, minutes: minutesOf(own) };
};

const minutesOf = (subset: Session[]) => sum(subset.map((session) => session.minutes));

const averageScore = (subset: Session[], name: string) => {
	const scores = subset
		.filter((session) => hasScoring(session) && !session.isCoop)
		.map((session) => scoreOf(session, name))
		.filter((score) => score !== null);
	return scores.length === 0 ? "–" : oneDecimal.format(roundedToEven(sum(scores) / scores.length));
};

export const headToHead = (one: string, two: string) => {
	const together = newestFirst(sessions.filter((session) => played(session, one) && played(session, two)));
	const outcome = (session: Session): Outcome => {
		if (session.isCoop) return "sameTeam";
		if (session.winner === one) return "playerOne";
		return session.winner === two ? "playerTwo" : "other";
	};
	const countOf = (subset: Session[], wanted: Outcome) => count(subset, (session) => outcome(session) === wanted);

	const perGame = [...new Set(together.map((session) => session.game))]
		.map((title) => {
			const subset = together.filter((session) => session.game === title);
			const sameTeam = count(subset, (session) => session.isCoop);
			return {
				title,
				id: gameId(title),
				sessions: subset.length,
				oneWins: countOf(subset, "playerOne"),
				twoWins: countOf(subset, "playerTwo"),
				others: countOf(subset, "other"),
				sameTeam,
				sameTeamWins: count(subset, (session) => session.isCoop && session.teamWon === true),
				scored: count(subset, (session) => hasScoring(session) && !session.isCoop),
				oneAverage: averageScore(subset, one),
				twoAverage: averageScore(subset, two),
				lastPlayed: subset[0].start.getTime(),
				lead: Math.abs(countOf(subset, "playerOne") - countOf(subset, "playerTwo")),
			};
		})
		.sort((a, b) => b.sessions - a.sessions || b.lastPlayed - a.lastPlayed || a.id - b.id);

	const bestFor = (lead: (game: (typeof perGame)[number]) => number) =>
		perGame
			.filter((game) => game.sessions - game.sameTeam >= MIN_SESSIONS_FOR_AVERAGE && lead(game) > 0)
			.sort((a, b) => lead(b) - lead(a) || b.sessions - b.sameTeam - (a.sessions - a.sameTeam) || a.id - b.id)
			.slice(0, BEST_GAMES);

	const streak = () => {
		const decided = together.map(outcome).filter((result) => result !== "sameTeam");
		const [latest] = decided;
		if (latest === undefined || latest === "other") return null;
		const end = decided.findIndex((result) => result !== latest);
		const length = end === -1 ? decided.length : end;
		return length < MIN_STREAK ? null : { name: latest === "playerOne" ? one : two, length };
	};

	const closest = together
		.filter((session) => !session.isCoop && hasScoring(session))
		.map((session) => ({
			session,
			difference: Math.abs((scoreOf(session, one) ?? 0) - (scoreOf(session, two) ?? 0)),
		}))
		.sort((a, b) => a.difference - b.difference || b.session.start.getTime() - a.session.start.getTime())
		.at(0);

	const first = together.at(-1);

	return {
		together,
		outcome,
		oneWins: countOf(together, "playerOne"),
		twoWins: countOf(together, "playerTwo"),
		others: countOf(together, "other"),
		sameTeam: count(together, (session) => session.isCoop),
		sameTeamWon: count(together, (session) => session.isCoop && session.teamWon === true),
		minutes: minutesOf(together),
		first,
		perGame,
		biggestLead: [...perGame].sort((a, b) => b.lead - a.lead || b.sessions - a.sessions || a.id - b.id),
		bestOne: bestFor((game) => game.oneWins - game.twoWins),
		bestTwo: bestFor((game) => game.twoWins - game.oneWins),
		streak: streak(),
		closest,
		recent: together.slice(0, RECENT_SESSIONS),
		scoreOf,
	};
};

export const defaultPair = () => {
	const pairs = players.flatMap((one, index) =>
		players.slice(index + 1).map((two) => ({ one, two, sessions: headToHead(one, two).together.length })),
	);
	return pairs.reduce((best, pair) => (pair.sessions > best.sessions ? pair : best), pairs[0]);
};

export const allPairs = players.flatMap((one, index) => players.slice(index + 1).map((two) => [one, two] as const));

export type RankBy = "wins" | "winRate" | "sessions";

const podiumFinishers = (session: Session) => {
	const scored = session.scores.filter((score) => score.score !== null);
	if (session.isCoop || !hasScoring(session) || scored.length < MIN_PLAYERS_FOR_PODIUM) return [];
	const isBetter = (a: (typeof scored)[number], b: (typeof scored)[number]) => {
		const aWon = a.player === session.winner;
		const bWon = b.player === session.winner;
		if (aWon !== bWon) return aWon;
		return (a.score ?? 0) > (b.score ?? 0);
	};
	return scored
		.filter((seat) => 1 + scored.filter((other) => isBetter(other, seat)).length <= PODIUM_PLACES)
		.map((seat) => seat.player);
};

const byName = (a: { name: string }, b: { name: string }) => a.name.toLowerCase().localeCompare(b.name.toLowerCase());

export const leaderboard = (subset: Session[], minimum: number, rankBy: RankBy) => {
	const entries = players
		.map((name) => ({
			name,
			...totals(name, subset),
			podiums: sum(subset.map((session) => (podiumFinishers(session).includes(name) ? 1 : 0))),
		}))
		.filter((entry) => entry.sessions > 0);
	type Entry = (typeof entries)[number];

	const order: Record<RankBy, (a: Entry, b: Entry) => number> = {
		wins: (a, b) => b.wins - a.wins || b.winRate - a.winRate || b.sessions - a.sessions,
		winRate: (a, b) => b.winRate - a.winRate || b.wins - a.wins || b.sessions - a.sessions,
		sessions: (a, b) => b.sessions - a.sessions || b.wins - a.wins,
	};

	const ordered = entries
		.filter((entry) => entry.sessions >= minimum)
		.sort((a, b) => order[rankBy](a, b) || byName(a, b));
	const ranks: number[] = [];
	const ranked = ordered.map((entry, index) => {
		const previous = ordered[index - 1];
		const same = previous && previous.wins === entry.wins && previous.sessions === entry.sessions;
		ranks[index] = same ? ranks[index - 1] : index + 1;
		return { ...entry, rank: ranks[index] };
	});
	const unranked = entries
		.filter((entry) => entry.sessions < minimum)
		.sort((a, b) => b.sessions - a.sessions || b.wins - a.wins || byName(a, b));

	const first = <T>(list: T[], condition: (item: T) => boolean, compare: (a: T, b: T) => number) =>
		[...list].filter(condition).sort(compare).at(0) ?? null;

	return {
		entries,
		ranked,
		unranked,
		minimum,
		rankBy,
		mostWins: first(
			entries,
			(x) => x.wins > 0,
			(a, b) => b.wins - a.wins || b.winRate - a.winRate || byName(a, b),
		),
		mostSessions: first(
			entries,
			() => true,
			(a, b) => b.sessions - a.sessions || b.wins - a.wins || byName(a, b),
		),
		bestWinRate: first(
			ranked,
			(x) => x.wins > 0,
			(a, b) => b.winRate - a.winRate || b.wins - a.wins || byName(a, b),
		),
		mostTime: first(
			entries,
			(x) => x.minutes > 0,
			(a, b) => b.minutes - a.minutes || byName(a, b),
		),
		totalWins: sum(entries.map((entry) => entry.wins)),
		coopWins: sum(subset.filter((session) => session.isCoop && session.teamWon).map((x) => x.scores.length)),
	};
};

export type Standing = ReturnType<typeof leaderboard>;

export const brusselsDay = (date: Date) =>
	new Intl.DateTimeFormat("en-CA", { timeZone: "Europe/Brussels" }).format(date);

export const between = (from?: string, to?: string) =>
	sessions.filter((session) => {
		const day = brusselsDay(session.start);
		return (from === undefined || day >= from) && (to === undefined || day <= to);
	});

export const literal = (text: string) => text.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");

export const short = (minutes: number) => duration(Math.round(minutes));
