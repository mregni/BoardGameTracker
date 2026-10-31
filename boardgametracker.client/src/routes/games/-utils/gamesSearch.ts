import { type Game, GameState } from "@/models";
import type { AgeBucket, GameSort, GamesFilterSearch, WeightBucket } from "../-components/GamesFilters";

const WEIGHT_BUCKETS: WeightBucket[] = ["light", "medium", "heavy"];
const AGE_BUCKETS: AgeBucket[] = ["0-6", "7-9", "10-12", "13plus"];
export const GAME_SORTS: GameSort[] = ["title", "plays", "last-played", "added", "rating", "weight"];
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
	const sort = search.sort as GameSort;
	return {
		view: search.view === "table" ? "table" : undefined,
		q: parseText(search.q),
		category: parseText(search.category),
		players: parsePositiveInt(search.players),
		playTime: parsePositiveInt(search.playTime),
		weight: WEIGHT_BUCKETS.includes(weight) ? weight : undefined,
		age: AGE_BUCKETS.includes(age) ? age : undefined,
		state: GAME_STATES.includes(search.state as string) ? (search.state as GameState) : undefined,
		language: parseText(search.language),
		inStock: search.inStock === true || search.inStock === "true" ? true : undefined,
		sort: sort !== "title" && GAME_SORTS.includes(sort) ? sort : undefined,
	};
};

export const withoutUndefined = <T extends object>(value: T): T =>
	Object.fromEntries(Object.entries(value).filter(([, entry]) => entry !== undefined)) as T;

export const filterByTitle = (games: Game[], query: string | undefined): Game[] => {
	const needle = query?.trim().toLowerCase();
	if (!needle) return games;
	return games.filter((game) => game.title.toLowerCase().includes(needle));
};

export const hasActiveFilters = ({ view, sort, ...filters }: GamesFilterSearch): boolean =>
	Object.values(filters).some((value) => value !== undefined);

const descendingNullsLast = (a: number | null, b: number | null) => (b ?? -Infinity) - (a ?? -Infinity);
const time = (date: Date | null) => (date ? new Date(date).getTime() : null);

export const sortGames = (games: Game[], sort: GameSort | undefined): Game[] => {
	const byTitle = (a: Game, b: Game) => a.title.localeCompare(b.title);
	const compare: Record<GameSort, (a: Game, b: Game) => number> = {
		title: byTitle,
		plays: (a, b) => b.playCount - a.playCount,
		"last-played": (a, b) => descendingNullsLast(time(a.lastPlayed), time(b.lastPlayed)),
		added: (a, b) => descendingNullsLast(time(a.additionDate), time(b.additionDate)),
		rating: (a, b) => descendingNullsLast(a.rating, b.rating),
		weight: (a, b) => descendingNullsLast(a.weight, b.weight),
	};
	const primary = compare[sort ?? "title"];
	return [...games].sort((a, b) => primary(a, b) || byTitle(a, b));
};

export const ownedPriceStats = (games: Game[]) => {
	const priced = games.filter((game) => game.state === GameState.Owned && (game.buyingPrice ?? 0) > 0);
	const total = priced.reduce((sum, game) => sum + (game.buyingPrice ?? 0), 0);
	return { total, mean: priced.length > 0 ? total / priced.length : 0, pricedCount: priced.length };
};
