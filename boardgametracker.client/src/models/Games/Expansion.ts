import type { BoxOrientation } from "../Storage/StorageLocation";
import type { GameState } from "./GameState";

export interface Expansion {
	id: number;
	title: string;
	bggId: number | null;
	gameId: number | null;
	image: string | null;
	state: GameState;
	buyingPrice: number | null;
	soldPrice: number | null;
	additionDate: Date | null;
	storedAtLocationId: number | null;
	storageLocationId: number | null;
	storagePosition: number;
	boxOrientation: BoxOrientation;
}

export interface ExpansionUpdate {
	gameId: number;
	expansionBggIds: number[];
}

export interface UpdateExpansion {
	state: GameState;
	buyingPrice: number | null;
	soldPrice: number | null;
	additionDate: Date | null;
}
