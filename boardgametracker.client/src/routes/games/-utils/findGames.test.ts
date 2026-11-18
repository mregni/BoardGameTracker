import { describe, expect, it } from "vitest";
import { type Expansion, type Game, GameState, StorageLayout, type StorageLocation } from "@/models";
import { LANGUAGE_NONE } from "@/utils/languageUtils";
import { NO_STORAGE } from "../-components/GamesFilters";
import { buildCards, findGames, storageGoal } from "./findGames";

const expansion = (id: number, title: string, overrides: Partial<Expansion> = {}) =>
	({ id, title, state: GameState.Owned, storedAtLocationId: null, storageLocationId: null, ...overrides }) as Expansion;

const game = (id: number, title: string, overrides: Partial<Game> = {}) =>
	({
		id,
		title,
		state: GameState.Owned,
		categories: [],
		language: null,
		expansions: [],
		storedAtLocationId: null,
		storageLocationId: null,
		...overrides,
	}) as unknown as Game;

const nodes: StorageLocation[] = [
	{
		id: 5,
		name: "Kallax",
		locationId: 1,
		parentId: null,
		layout: StorageLayout.Free,
		rows: null,
		columns: null,
		row: null,
		column: null,
	},
];

const titles = (list: { title: string }[]) => list.map((item) => item.title);
const always = () => true;

describe("findGames", () => {
	const azul = game(1, "Azul", { language: "nl", categories: [{ name: "Tiles" }] as Game["categories"] });
	const brass = game(2, "Brass", {
		storedAtLocationId: 1,
		storageLocationId: 5,
		expansions: [expansion(20, "Brass: Iron")],
	});
	const catan = game(3, "Catan", {
		state: GameState.Wanted,
		expansions: [expansion(30, "Seafarers", { storedAtLocationId: 1, storageLocationId: 5 })],
	});
	const games = [catan, brass, azul];

	it("filters on title, state, category, language and stock", () => {
		expect(titles(findGames(games, {}, nodes, always).filteredGames)).toEqual(["Azul", "Brass", "Catan"]);
		expect(titles(findGames(games, { q: " bra " }, nodes, always).filteredGames)).toEqual(["Brass"]);
		expect(titles(findGames(games, { state: GameState.Wanted }, nodes, always).filteredGames)).toEqual(["Catan"]);
		expect(titles(findGames(games, { category: "Tiles" }, nodes, always).filteredGames)).toEqual(["Azul"]);
		expect(titles(findGames(games, { language: "nl" }, nodes, always).filteredGames)).toEqual(["Azul"]);
		expect(titles(findGames(games, { language: LANGUAGE_NONE }, nodes, always).filteredGames)).toEqual([
			"Brass",
			"Catan",
		]);
		expect(titles(findGames(games, {}, nodes, (item) => item.id === 2).filteredGames)).toEqual(["Brass"]);
	});

	it("lists the expansions whose own title or base game matches", () => {
		expect(
			titles(findGames(games, { q: "brass" }, nodes, always).expansionCards.map((card) => card.expansion)),
		).toEqual(["Brass: Iron"]);
	});

	it("keeps only owned games without a place for no storage, without expansions", () => {
		const found = findGames(games, { storage: NO_STORAGE }, nodes, always);

		expect(titles(found.filteredGames)).toEqual(["Azul"]);
		expect(found.expansionCards).toEqual([]);
		expect(found.dimmed.size).toBe(0);
	});

	it("dims a base game that is only listed for an expansion stored at the place", () => {
		const found = findGames(games, { storage: "1:5" }, nodes, always);

		expect(titles(found.filteredGames)).toEqual(["Brass", "Catan"]);
		expect(found.dimmed).toEqual(new Map([[3, [30]]]));
		expect(titles(found.expansionCards.map((card) => card.expansion))).toEqual(["Seafarers"]);
	});
});

describe("storageGoal", () => {
	it("reads a place and ignores no storage or nothing", () => {
		expect(storageGoal("1:5")).toEqual({ locationId: 1, storageLocationId: 5 });
		expect(storageGoal(NO_STORAGE)).toBeNull();
		expect(storageGoal(undefined)).toBeNull();
	});
});

describe("buildCards", () => {
	const brass = game(2, "Brass");
	const catan = game(3, "Catan");
	const iron = expansion(20, "Aaron's Iron");
	const cards = {
		filteredGames: [brass, catan],
		dimmed: new Map([[3, [30]]]),
		expansionCards: [{ game: brass, expansion: iron }],
	};
	const shown = (list: ReturnType<typeof buildCards>) => list.map((card) => (card.expansion ?? card.game).title);

	it("shows base games that are not dimmed, expansions on request, sorted together by title", () => {
		expect(shown(buildCards(cards, undefined, undefined))).toEqual(["Brass"]);
		expect(shown(buildCards(cards, "expansions", undefined))).toEqual(["Aaron's Iron"]);
		expect(shown(buildCards(cards, "any", undefined))).toEqual(["Aaron's Iron", "Brass"]);
		expect(shown(buildCards(cards, "any", "plays"))).toEqual(["Brass", "Aaron's Iron"]);
	});
});
