import type { Game } from "../Games/Game";
import type { Location } from "../Location/Location";
import type { Player } from "../Player/Player";
import type { CreateSessionPlayer } from "../Session/CreateSession";
import type { Session } from "../Session/Session";

export enum GameNightRsvpState {
	Accepted = "accepted",
	Declined = "declined",
	Pending = "pending",
}

export interface GameNightRsvps {
	id: number;
	playerId: number;
	player: Player;
	gameNightId: number;
	state: GameNightRsvpState;
}

export interface GameNight {
	id: number;
	title: string;
	startDate: Date;
	endedAt: Date | null;
	location: Location;
	locationId: number;
	host: Player;
	hostId: number;
	linkId: string;
	invitedPlayers: GameNightRsvps[];
	suggestedGames: Game[];
	notes: string;
	summary: GameNightSummary;
}

export interface GameNightSummary {
	gamesPlayed: number;
	totalMinutes: number;
	mostWins: number;
	winnerIds: number[];
}

export interface RunningGame {
	id: number;
	gameNightId: number;
	gameId: number;
	start: Date;
	playerIds: number[];
}

export interface GameNightDetail {
	gameNight: GameNight;
	sessions: Session[];
	runningGames: RunningGame[];
}

export interface StartRunningGame {
	gameId: number;
	playerIds: number[];
}

export interface FinishRunningGame {
	playerSessions: CreateSessionPlayer[];
	start: Date;
	minutes: number;
}

export interface CreateGameNight {
	title: string;
	notes: string;
	startDate: Date;
	hostId: number;
	locationId: number;
	suggestedGameIds: number[];
	invitedPlayerIds: number[];
}

export interface UpdateGameNight extends CreateGameNight {
	id: number;
}

export interface UpdateGameNightRsvp {
	id: number;
	gameNightId: number;
	playerId: number;
	state: GameNightRsvpState;
}

export interface SendInvitesResult {
	sent: number;
}
