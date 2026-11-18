import type { Expansion, Game, GameState, StorageLocation } from "@/models";
import { LANGUAGE_NONE } from "@/utils/languageUtils";
import { canStore, isPlacedIn, parsePlaceKey, placeOf, type StoragePlace } from "@/utils/storage";
import {
	filterGames,
	type GameSort,
	type GamesFilterSearch,
	type GameType,
	NO_STORAGE,
} from "../-components/GamesFilters";
import { sortGames } from "./gamesSearch";

export interface FoundGames {
	filteredGames: Game[];
	dimmed: Map<number, number[]>;
	expansionCards: { game: Game; expansion: Expansion }[];
}

export const storageGoal = (storage: string | undefined): StoragePlace | null =>
	storage && storage !== NO_STORAGE ? parsePlaceKey(storage) : null;

const matchesLanguage = (game: Game, language: string | undefined) => {
	if (language === undefined) return true;
	if (language === LANGUAGE_NONE) return !game.language;
	return game.language === language;
};

const matchesFacts = (game: Game, { category, language }: GamesFilterSearch) =>
	(category === undefined || game.categories.some((cat) => cat.name === category)) && matchesLanguage(game, language);

const placeFilter = (storage: string | undefined, nodes: StorageLocation[]) => {
	if (storage === NO_STORAGE) return (item: Game | Expansion) => canStore(item.state) && placeOf(item) === null;
	const goal = storageGoal(storage);
	return (item: Game | Expansion) => goal === null || isPlacedIn(placeOf(item), goal, nodes);
};

const dimmedGames = (candidates: Game[], base: Game[], expansionsOf: (game: Game) => Expansion[]) => {
	const dimmed = new Map<number, number[]>();
	for (const game of candidates) {
		const matching = base.includes(game) ? [] : expansionsOf(game);
		if (matching.length > 0)
			dimmed.set(
				game.id,
				matching.map((expansion) => expansion.id),
			);
	}
	return dimmed;
};

export const findGames = (
	games: Game[],
	search: GamesFilterSearch,
	nodes: StorageLocation[],
	inStock: (game: Game) => boolean,
): FoundGames => {
	const needle = search.q?.trim().toLowerCase() ?? "";
	const titled = (title: string) => title.toLowerCase().includes(needle);
	const inState = (value: GameState) => search.state === undefined || value === search.state;
	const placed = placeFilter(search.storage, nodes);
	const candidates = filterGames(
		games.filter((game) => matchesFacts(game, search) && inStock(game)),
		{ playerCount: search.players, maxPlayTime: search.playTime, weight: search.weight, age: search.age },
	);
	const expansionsOf = (game: Game) =>
		search.storage === NO_STORAGE
			? []
			: game.expansions.filter(
					(expansion) =>
						(titled(expansion.title) || titled(game.title)) && inState(expansion.state) && placed(expansion),
				);
	const base = candidates.filter((game) => titled(game.title) && inState(game.state) && placed(game));
	const dimmed = storageGoal(search.storage)
		? dimmedGames(candidates, base, expansionsOf)
		: new Map<number, number[]>();
	return {
		filteredGames: sortGames([...base, ...candidates.filter((game) => dimmed.has(game.id))], search.sort),
		dimmed,
		expansionCards: candidates
			.flatMap((game) => expansionsOf(game).map((expansion) => ({ game, expansion })))
			.sort((a, b) => a.expansion.title.localeCompare(b.expansion.title)),
	};
};

export const buildCards = (
	{ filteredGames, dimmed, expansionCards }: FoundGames,
	type: GameType | undefined,
	sort: GameSort | undefined,
) => {
	const showBase = type !== "expansions";
	const showExpansions = type !== undefined;
	const cards = [
		...(showBase ? filteredGames.filter((game) => !dimmed.has(game.id)) : []).map((game) => ({
			game,
			expansion: null as Expansion | null,
		})),
		...(showExpansions ? expansionCards : []),
	];
	if (showBase && showExpansions && (sort ?? "title") === "title") {
		cards.sort((a, b) => (a.expansion?.title ?? a.game.title).localeCompare(b.expansion?.title ?? b.game.title));
	}
	return cards;
};
