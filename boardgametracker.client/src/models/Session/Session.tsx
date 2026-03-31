import type { Expansion } from "../Games/Expansion";

import type { PlayerSession } from "./PlayerSession";

export interface Session {
	id: number;
	comment: string;
	ended: boolean;
	gameId: number;
	start: Date;
	minutes: number;
	playerSessions: PlayerSession[];
	expansions: Expansion[];
	locationId: number;
}
