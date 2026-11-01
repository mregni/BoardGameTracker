import type { Session } from "@/models";

export interface Tally {
	id: number;
	count: number;
}

export interface Ranking {
	top: Tally[];
	more: number;
}

const rank = (ids: number[], limit: number): Ranking => {
	const counts = new Map<number, number>();
	for (const id of ids) counts.set(id, (counts.get(id) ?? 0) + 1);
	const sorted = [...counts].map(([id, count]) => ({ id, count })).sort((a, b) => b.count - a.count);
	return { top: sorted.slice(0, limit), more: Math.max(0, sorted.length - limit) };
};

const newestFirst = (sessions: Session[]) =>
	[...sessions].sort((a, b) => new Date(b.start).getTime() - new Date(a.start).getTime());

export const mostPlayedGames = (sessions: Session[], limit = 5): Ranking =>
	rank(
		newestFirst(sessions).map((session) => session.gameId),
		limit,
	);

export const topWinners = (sessions: Session[], limit = 5): Ranking =>
	rank(
		newestFirst(sessions).flatMap((session) => session.playerSessions.filter((x) => x.won).map((x) => x.playerId)),
		limit,
	);
