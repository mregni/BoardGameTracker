import type { TFunction } from "i18next";
import { describe, expect, it } from "vitest";
import {
	BoxOrientation,
	type Expansion,
	type Game,
	GameState,
	type Location,
	StorageItemType,
	StorageLayout,
	type StorageLocation,
} from "@/models";
import {
	allPlaces,
	isPlacedIn,
	itemsIn,
	needsAPlace,
	parsePlaceKey,
	placeKey,
	placeLabel,
	storageItems,
} from "./storage";

const t = ((key: string, values?: Record<string, unknown>) =>
	`${key}${values ? JSON.stringify(values) : ""}`) as TFunction;

const node = (id: number, parentId: number | null, overrides: Partial<StorageLocation> = {}): StorageLocation => ({
	id,
	name: `Node ${id}`,
	locationId: 1,
	parentId,
	layout: StorageLayout.Free,
	rows: null,
	columns: null,
	row: null,
	column: null,
	...overrides,
});

const nodes = [
	node(1, null, { name: "Living room" }),
	node(2, 1, { name: "Kallax", layout: StorageLayout.Grid, rows: 1, columns: 2 }),
	node(3, 2, { name: null, layout: StorageLayout.Slot, row: 1, column: 2 }),
	node(4, null, { name: "Bench", locationId: 2 }),
];

const locations = [{ id: 1, name: "Home" } as Location, { id: 2, name: "Trailer" } as Location];

const expansion = (id: number, overrides: Partial<Expansion> = {}): Expansion => ({
	id,
	title: `Expansion ${id}`,
	bggId: null,
	gameId: 1,
	image: null,
	state: GameState.Owned,
	buyingPrice: null,
	soldPrice: null,
	additionDate: null,
	storedAtLocationId: null,
	storageLocationId: null,
	storagePosition: 0,
	boxOrientation: BoxOrientation.Front,
	...overrides,
});

const game = (id: number, overrides: Partial<Game> = {}) =>
	({
		id,
		title: `Game ${id}`,
		image: "",
		state: GameState.Owned,
		isLoaned: false,
		expansions: [],
		storedAtLocationId: null,
		storageLocationId: null,
		storagePosition: 0,
		boxOrientation: BoxOrientation.Side,
		...overrides,
	}) as Game;

describe("storage", () => {
	it("lists base games with a place and expansions with a place of their own, in shelf order", () => {
		const items = storageItems([
			game(1, {
				storedAtLocationId: 1,
				storageLocationId: 3,
				storagePosition: 2,
				expansions: [expansion(5), expansion(6, { storedAtLocationId: 1, storageLocationId: 3, storagePosition: 0 })],
			}),
			game(2),
		]);

		expect(items.map((item) => item.key)).toEqual(["expansion-6", "game-1"]);
		expect(items[0]).toMatchObject({ type: StorageItemType.Expansion, title: "Expansion 6" });
	});

	it("treats a place inside a storage location as being in it and in its location", () => {
		const cell = { locationId: 1, storageLocationId: 3 };

		expect(isPlacedIn(cell, { locationId: 1, storageLocationId: 1 }, nodes)).toBe(true);
		expect(isPlacedIn(cell, { locationId: 1, storageLocationId: null }, nodes)).toBe(true);
		expect(isPlacedIn(cell, { locationId: 2, storageLocationId: null }, nodes)).toBe(false);
		expect(isPlacedIn({ locationId: 1, storageLocationId: null }, { locationId: 1, storageLocationId: 1 }, nodes)).toBe(
			false,
		);
		expect(isPlacedIn(null, { locationId: 1, storageLocationId: null }, nodes)).toBe(false);
	});

	it("names a cell after its row and column and builds the full path", () => {
		expect(placeLabel({ locationId: 1, storageLocationId: 3 }, nodes, locations, t)).toBe(
			'Home › Living room › Kallax › storage:slot.cell{"row":1,"column":2}',
		);
		expect(placeLabel({ locationId: 2, storageLocationId: null }, nodes, locations, t)).toBe("Trailer");
	});

	it("lists every place in tree order, each location first", () => {
		expect(allPlaces(nodes, locations).map(placeKey)).toEqual(["1:", "1:1", "1:2", "1:3", "2:", "2:4"]);
	});

	it("round-trips a place key", () => {
		expect(parsePlaceKey("4:")).toEqual({ locationId: 4, storageLocationId: null });
		expect(parsePlaceKey("4:9")).toEqual({ locationId: 4, storageLocationId: 9 });
		expect(parsePlaceKey("none")).toBeNull();
	});

	it("counts items below a storage location", () => {
		const items = storageItems([
			game(1, { storedAtLocationId: 1, storageLocationId: 3 }),
			game(2, { storedAtLocationId: 1 }),
		]);

		expect(itemsIn(items, { locationId: 1, storageLocationId: 2 }, nodes)).toHaveLength(1);
		expect(itemsIn(items, { locationId: 1, storageLocationId: null }, nodes)).toHaveLength(2);
	});

	it("only asks a place for owned games and games for trade", () => {
		const games = [
			game(1),
			game(2, { state: GameState.ForTrade }),
			game(3, { state: GameState.Wanted }),
			game(4, { storedAtLocationId: 1 }),
		];

		expect(needsAPlace(games).map((x) => x.id)).toEqual([1, 2]);
	});
});
