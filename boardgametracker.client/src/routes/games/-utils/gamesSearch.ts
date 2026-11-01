import { type Game, GameState } from "@/models";
import {
	type AgeBucket,
	type GameSort,
	type GamesFilterSearch,
	PLAY_TIMES,
	PLAYER_COUNTS,
	type WeightBucket,
} from "../-components/GamesFilters";

const WEIGHT_BUCKETS: WeightBucket[] = ["light", "medium", "heavy"];
const AGE_BUCKETS: AgeBucket[] = ["0-6", "7-9", "10-12", "13plus"];
export const GAME_SORTS: GameSort[] = ["title", "plays", "last-played", "added", "rating", "weight"];
const GAME_STATES = Object.values(GameState) as string[];

const parsePositiveInt = (value: unknown): number | undefined => {
	const parsed = Number(value);
	return Number.isInteger(parsed) && parsed > 0 ? parsed : undefined;
};

const parseOneOf = (value: unknown, allowed: number[]): number | undefined => {
	const parsed = parsePositiveInt(value);
	return parsed !== undefined && allowed.includes(parsed) ? parsed : undefined;
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
		players: parseOneOf(search.players, PLAYER_COUNTS),
		playTime: parseOneOf(search.playTime, PLAY_TIMES),
		weight: WEIGHT_BUCKETS.includes(weight) ? weight : undefined,
		age: AGE_BUCKETS.includes(age) ? age : undefined,
		state: GAME_STATES.includes(search.state as string) ? (search.state as GameState) : undefined,
		language: parseText(search.language),
		inStock: search.inStock === true || search.inStock === "true" ? true : undefined,
		sort: sort !== "title" && GAME_SORTS.includes(sort) ? sort : undefined,
	};
};

export const parseIds = (value: unknown): number[] | undefined => {
	if (!Array.isArray(value)) return undefined;
	const ids = value.map(Number).filter((id) => Number.isInteger(id) && id > 0);
	return ids.length > 0 ? ids : undefined;
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

export const isPlayable = (game: Game): boolean => game.state === GameState.Owned && !game.isLoaned;

const STALE_DAYS = 180;

export const freshWeight = (game: Game, now = Date.now()): number => {
	if (game.playCount === 0 || !game.lastPlayed) return 3;
	return now - new Date(game.lastPlayed).getTime() > STALE_DAYS * 86_400_000 ? 2 : 1;
};

export const pickWheelGames = (
	games: Game[],
	max = 10,
	random = Math.random,
	weight: (game: Game) => number = () => 1,
): Game[] =>
	games
		.filter(isPlayable)
		.map((game) => ({ game, key: random() ** (1 / weight(game)) }))
		.sort((a, b) => b.key - a.key)
		.slice(0, max)
		.map(({ game }) => game);

export const ownedPriceStats = (games: Game[]) => {
	const priced = games.filter((game) => game.state === GameState.Owned && (game.buyingPrice ?? 0) > 0);
	const total = priced.reduce((sum, game) => sum + (game.buyingPrice ?? 0), 0);
	return { total, mean: priced.length > 0 ? total / priced.length : 0, pricedCount: priced.length };
};
