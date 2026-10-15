import type { Expansion } from "../Games/Expansion";

import type { PlayerSession } from "./PlayerSession";

export interface Session {
	id: number;
	comment: string;
	gameId: number;
	isCoop: boolean;
	teamWon: boolean | null;
	teamScore: number | null;
	start: Date;
	end: Date;
	minutes: number;
	playerSessions: PlayerSession[];
	expansions: Expansion[];
	locationId: number | null;
	gameNightId: number | null;
}
