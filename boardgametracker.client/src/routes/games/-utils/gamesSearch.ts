import { type Game, GameState } from "@/models";
import {
	type AgeBucket,
	filterGames,
	type GamesFilterSearch,
	type ShowOption,
	type SortOption,
	type WeightBucket,
} from "../-components/GamesFilters";

export interface GamesTableSearch {
	q?: string;
	state?: GameState;
	language?: string;
	inStock?: boolean;
}

const WEIGHT_BUCKETS: WeightBucket[] = ["light", "medium", "heavy"];
const AGE_BUCKETS: AgeBucket[] = ["0-6", "7-9", "10-12", "13plus"];
const SHOW_OPTIONS: ShowOption[] = ["wanted", "all"];
const SORT_OPTIONS: SortOption[] = ["recent", "plays", "added"];
const GAME_STATES = Object.values(GameState) as string[];

const parsePositiveInt = (value: unknown): number | undefined => {
	const parsed = Number(value);
	return Number.isInteger(parsed) && parsed > 0 ? parsed : undefined;
};

const parseText = (value: unknown): string | undefined => {
	if (typeof value === "number") return String(value);
	if (typeof value !== "string") return undefined;
	return value.trim().length > 0 ? value : undefined;
};

export const parseGamesSearch = (search: Record<string, unknown>): GamesFilterSearch => {
	const weight = search.weight as WeightBucket;
	const age = search.age as AgeBucket;
	const show = search.show as ShowOption;
	const sort = search.sort as SortOption;
	return {
		q: parseText(search.q),
		category: parseText(search.category),
		players: parsePositiveInt(search.players),
		playTime: parsePositiveInt(search.playTime),
		weight: WEIGHT_BUCKETS.includes(weight) ? weight : undefined,
		age: AGE_BUCKETS.includes(age) ? age : undefined,
		show: SHOW_OPTIONS.includes(show) ? show : undefined,
		sort: SORT_OPTIONS.includes(sort) ? sort : undefined,
	};
};

export const parseGamesTableSearch = (search: Record<string, unknown>): GamesTableSearch => ({
	q: parseText(search.q),
	state: GAME_STATES.includes(search.state as string) ? (search.state as GameState) : undefined,
	language: parseText(search.language),
	inStock: search.inStock === true || search.inStock === "true" ? true : undefined,
});

export const withoutUndefined = <T extends object>(value: T): T =>
	Object.fromEntries(Object.entries(value).filter(([, entry]) => entry !== undefined)) as T;

export const filterByTitle = (games: Game[], query: string | undefined): Game[] => {
	const needle = query?.trim().toLowerCase();
	if (!needle) return games;
	return games.filter((game) => game.title.toLowerCase().includes(needle));
};

export const hasGridFilters = ({ sort: _sort, ...search }: GamesFilterSearch): boolean =>
	Object.values(search).some((value) => value !== undefined);

export interface PlaySummary {
	playCount: number;
	lastPlayed: string | null;
}

const matchesShow = (game: Game, show: ShowOption | undefined) => {
	if (show === "all") return true;
	return show === "wanted" ? game.state === GameState.Wanted : game.state !== GameState.Wanted;
};

export const applyGridFilters = (games: Game[], search: GamesFilterSearch): Game[] => {
	const { q, category, players, playTime, weight, age, show } = search;
	const result = filterByTitle(games, q).filter(
		(game) =>
			matchesShow(game, show) && (category === undefined || game.categories.some((cat) => cat.name === category)),
	);
	return filterGames(result, { playerCount: players, maxPlayTime: playTime, weight, age });
};

const timeOf = (value: Date | string | null | undefined) => (value ? new Date(value).getTime() : 0);

export const sortGames = (games: Game[], sort: SortOption | undefined, plays: Map<number, PlaySummary>): Game[] => {
	if (sort === undefined || sort === "name") return games;
	const key = (game: Game) => {
		if (sort === "plays") return plays.get(game.id)?.playCount ?? 0;
		if (sort === "recent") return timeOf(plays.get(game.id)?.lastPlayed);
		return timeOf(game.additionDate);
	};
	return [...games].sort((a, b) => key(b) - key(a) || a.title.localeCompare(b.title));
};

export const ownedPriceStats = (games: Game[]) => {
	const priced = games.filter((game) => game.state === GameState.Owned && game.buyingPrice != null);
	const total = priced.reduce((sum, game) => sum + (game.buyingPrice ?? 0), 0);
	return { total, mean: priced.length > 0 ? total / priced.length : 0, pricedCount: priced.length };
};
