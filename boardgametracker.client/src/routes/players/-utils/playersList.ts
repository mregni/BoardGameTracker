import type { PlayerOverview } from "@/models";

export const PLAYER_SORTS = ["recent", "name", "sessions"] as const;
export type PlayerSort = (typeof PLAYER_SORTS)[number];

export interface PlayersSearch {
	q?: string;
	sort?: Exclude<PlayerSort, "recent">;
}

export const parsePlayersSearch = (search: Record<string, unknown>): PlayersSearch => {
	const q = typeof search.q === "string" && search.q.trim().length > 0 ? search.q : undefined;
	const sort = PLAYER_SORTS.find((x) => x !== "recent" && x === search.sort) as PlayersSearch["sort"];
	return { ...(q && { q }), ...(sort && { sort }) };
};

const lastPlayedTime = (player: PlayerOverview) => (player.lastPlayed ? new Date(player.lastPlayed).getTime() : 0);

const byName = (a: PlayerOverview, b: PlayerOverview) => a.name.localeCompare(b.name);

const compare: Record<PlayerSort, (a: PlayerOverview, b: PlayerOverview) => number> = {
	recent: (a, b) => lastPlayedTime(b) - lastPlayedTime(a) || byName(a, b),
	name: byName,
	sessions: (a, b) => b.playCount - a.playCount || byName(a, b),
};

export const sortPlayers = (players: PlayerOverview[], sort: PlayerSort): PlayerOverview[] =>
	[...players].sort(compare[sort]);

export const filterPlayers = (players: PlayerOverview[], query: string | undefined): PlayerOverview[] => {
	const needle = query?.trim().toLowerCase();
	return needle ? players.filter((x) => x.name.toLowerCase().includes(needle)) : players;
};
