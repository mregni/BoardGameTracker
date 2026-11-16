export enum StorageLayout {
	Free = "free",
	Shelves = "shelves",
	Grid = "grid",
	Slot = "slot",
}

export enum BoxOrientation {
	Front = "front",
	Side = "side",
	Flat = "flat",
}

export enum StorageItemType {
	Game = "game",
	Expansion = "expansion",
}

export interface StorageLocation {
	id: number;
	name: string | null;
	locationId: number;
	parentId: number | null;
	layout: StorageLayout;
	rows: number | null;
	columns: number | null;
	row: number | null;
	column: number | null;
}

export interface CreateStorageLocation {
	name: string;
	locationId: number;
	parentId: number | null;
	layout: StorageLayout;
	rows: number | null;
	columns: number | null;
}

export interface UpdateStorageLocation {
	name: string | null;
	rows: number | null;
	columns: number | null;
}

export interface MoveStorageLocation {
	locationId: number;
	parentId: number | null;
}

export interface StorageItemReference {
	type: StorageItemType;
	id: number;
}

export interface AssignStorage {
	items: StorageItemReference[];
	locationId: number | null;
	storageLocationId: number | null;
}

export interface ArrangeStorage {
	storageLocationId: number;
	items: StorageItemReference[];
}

export interface UpdateBoxOrientation {
	item: StorageItemReference;
	orientation: BoxOrientation;
}
