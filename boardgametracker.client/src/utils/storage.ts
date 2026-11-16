import type { TFunction } from "i18next";
import {
	type BoxOrientation,
	type Expansion,
	type Game,
	GameState,
	type Location,
	StorageItemType,
	StorageLayout,
	type StorageLocation,
} from "@/models";

export interface StoragePlace {
	locationId: number;
	storageLocationId: number | null;
}

export interface StorageItem {
	key: string;
	type: StorageItemType;
	id: number;
	title: string;
	image: string | null;
	game: Game;
	expansion: Expansion | null;
	place: StoragePlace;
	position: number;
	orientation: BoxOrientation;
	isLoaned: boolean;
}

interface Stored {
	storedAtLocationId: number | null;
	storageLocationId: number | null;
}

export const canStore = (state: GameState) => state === GameState.Owned || state === GameState.ForTrade;

export const isCabinet = (node: StorageLocation | undefined) =>
	node?.layout === StorageLayout.Shelves || node?.layout === StorageLayout.Grid;

export const placeOf = (item: Stored): StoragePlace | null =>
	item.storedAtLocationId === null
		? null
		: { locationId: item.storedAtLocationId, storageLocationId: item.storageLocationId };

export const placeKey = (place: StoragePlace) => `${place.locationId}:${place.storageLocationId ?? ""}`;

export const parsePlaceKey = (key: string): StoragePlace | null => {
	const match = /^(\d+):(\d*)$/.exec(key);
	return match ? { locationId: Number(match[1]), storageLocationId: match[2] ? Number(match[2]) : null } : null;
};

export const itemKey = (type: StorageItemType, id: number) => `${type}-${id}`;

export const storageItems = (games: Game[]): StorageItem[] =>
	games
		.flatMap((game) => {
			const items: StorageItem[] = [];
			const place = placeOf(game);
			if (place) {
				items.push({
					key: itemKey(StorageItemType.Game, game.id),
					type: StorageItemType.Game,
					id: game.id,
					title: game.title,
					image: game.image,
					game,
					expansion: null,
					place,
					position: game.storagePosition,
					orientation: game.boxOrientation,
					isLoaned: game.isLoaned,
				});
			}
			for (const expansion of game.expansions) {
				const own = placeOf(expansion);
				if (own) {
					items.push({
						key: itemKey(StorageItemType.Expansion, expansion.id),
						type: StorageItemType.Expansion,
						id: expansion.id,
						title: expansion.title,
						image: expansion.image,
						game,
						expansion,
						place: own,
						position: expansion.storagePosition,
						orientation: expansion.boxOrientation,
						isLoaned: false,
					});
				}
			}
			return items;
		})
		.sort((a, b) => a.position - b.position || a.title.localeCompare(b.title));

const byName = (a: StorageLocation, b: StorageLocation) =>
	(a.row ?? 0) - (b.row ?? 0) || (a.column ?? 0) - (b.column ?? 0) || (a.name ?? "").localeCompare(b.name ?? "");

export const childrenOf = (nodes: StorageLocation[], parentId: number) =>
	nodes.filter((node) => node.parentId === parentId).sort(byName);

export const rootsOf = (nodes: StorageLocation[], locationId: number) =>
	nodes.filter((node) => node.locationId === locationId && node.parentId === null).sort(byName);

export const descendantIds = (nodes: StorageLocation[], id: number): number[] => [
	id,
	...childrenOf(nodes, id).flatMap((child) => descendantIds(nodes, child.id)),
];

export const ancestorsOf = (nodes: StorageLocation[], id: number | null): StorageLocation[] => {
	const chain: StorageLocation[] = [];
	let node = nodes.find((x) => x.id === id);
	while (node) {
		chain.unshift(node);
		const parentId = node.parentId;
		node = parentId === null ? undefined : nodes.find((x) => x.id === parentId);
	}
	return chain;
};

export const nodeName = (node: StorageLocation, nodes: StorageLocation[], t: TFunction) => {
	if (node.name) {
		return node.name;
	}
	const parent = nodes.find((x) => x.id === node.parentId);
	return parent?.layout === StorageLayout.Grid
		? t("storage:slot.cell", { row: node.row, column: node.column })
		: t("storage:slot.shelf", { number: node.row });
};

export const placeNames = (
	place: StoragePlace,
	nodes: StorageLocation[],
	locations: Location[],
	t: TFunction,
): string[] => [
	locations.find((location) => location.id === place.locationId)?.name ?? "",
	...ancestorsOf(nodes, place.storageLocationId).map((node) => nodeName(node, nodes, t)),
];

export const placeLabel = (place: StoragePlace, nodes: StorageLocation[], locations: Location[], t: TFunction) =>
	placeNames(place, nodes, locations, t).join(" › ");

export const isPlacedIn = (place: StoragePlace | null, target: StoragePlace, nodes: StorageLocation[]) => {
	if (!place || place.locationId !== target.locationId) {
		return false;
	}
	return (
		target.storageLocationId === null ||
		(place.storageLocationId !== null &&
			descendantIds(nodes, target.storageLocationId).includes(place.storageLocationId))
	);
};

export const itemsIn = (items: StorageItem[], target: StoragePlace, nodes: StorageLocation[]) =>
	items.filter((item) => isPlacedIn(item.place, target, nodes));

export const itemsAt = (items: StorageItem[], place: StoragePlace) =>
	items.filter(
		(item) => item.place.locationId === place.locationId && item.place.storageLocationId === place.storageLocationId,
	);

export const needsAPlace = (games: Game[]) => games.filter((game) => canStore(game.state) && placeOf(game) === null);

export const allPlaces = (nodes: StorageLocation[], locations: Location[]): StoragePlace[] =>
	[...locations]
		.sort((a, b) => a.name.localeCompare(b.name))
		.flatMap((location) => [
			{ locationId: location.id, storageLocationId: null },
			...rootsOf(nodes, location.id).flatMap((root) =>
				descendantIds(nodes, root.id).map((id) => ({ locationId: location.id, storageLocationId: id })),
			),
		]);
