import { type Game, GameState } from "@/models";
import type { AgeBucket, GamesFilterSearch, WeightBucket } from "../-components/GamesFilters";

export interface GamesTableSearch {
	q?: string;
	state?: GameState;
	language?: string;
	inStock?: boolean;
}

const WEIGHT_BUCKETS: WeightBucket[] = ["light", "medium", "heavy"];
const AGE_BUCKETS: AgeBucket[] = ["0-6", "7-9", "10-12", "13plus"];
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
	return {
		q: parseText(search.q),
		category: parseText(search.category),
		players: parsePositiveInt(search.players),
		playTime: parsePositiveInt(search.playTime),
		weight: WEIGHT_BUCKETS.includes(weight) ? weight : undefined,
		age: AGE_BUCKETS.includes(age) ? age : undefined,
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

export const hasGridFilters = (search: GamesFilterSearch): boolean =>
	Object.values(search).some((value) => value !== undefined);

export const ownedPriceStats = (games: Game[]) => {
	const priced = games.filter((game) => game.state === GameState.Owned && game.buyingPrice != null);
	const total = priced.reduce((sum, game) => sum + (game.buyingPrice ?? 0), 0);
	return { total, mean: priced.length > 0 ? total / priced.length : 0, pricedCount: priced.length };
};
