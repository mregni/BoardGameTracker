import type { Expansion } from "./Expansion";
import type { GameState } from "./GameState";
import type { PersonType } from "./PersonType";

export interface Game {
	id: number;
	title: string;
	description: string;
	yearPublished: number | null;
	image: string;
	shopUrl: string | null;
	changeDetectionWatchId: string | null;
	language: string | null;
	minPlayers: number | null;
	maxPlayers: number | null;
	minPlayTime: number | null;
	maxPlayTime: number | null;
	minAge: number | null;
	rating: number | null;
	weight: number | null;
	bggId: number | null;
	state: GameState;
	isLoaned: boolean;
	expansions: Expansion[];
	categories: GameLink[];
	mechanics: GameLink[];
	people: GamePerson[];
	hasScoring: boolean;
	buyingPrice: number | null;
	soldPrice: number | null;
	additionDate: Date | null;
}

export interface GameLink {
	id: number;
	name: string;
}

export interface GamePerson extends GameLink {
	type: PersonType;
}
