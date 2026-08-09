import type { GameState } from "./GameState";

export interface ImportGame {
	title: string;
	bggId: number;
	state: GameState;
	imageUrl: string;
	checked: boolean;
	inCollection: boolean;
	hasScoring: boolean;
	price: number;
	addedDate: Date;
	lastModified: Date;
}
