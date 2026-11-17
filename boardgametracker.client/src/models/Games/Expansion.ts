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
