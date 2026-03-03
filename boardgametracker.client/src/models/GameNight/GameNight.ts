import type { Game } from "../Games/Game";
import type { Location } from "../Location/Location";
import type { Player } from "../Player/Player";

export enum GameNightRsvpState {
  Accepted = "accepted",
  Declined = "declined",
  Pending = "pending",
}

export interface GameNightRsvps {
  id: number;
  playerId: number;
  player: Player;
  gameNightId: string;
  state: GameNightRsvpState;
}

export interface GameNight {
  id: number;
  title: string;
  startDate: Date;
  location: Location;
  locationId: number;
  host: Player;
  hostId: number;
  linkId: string;
  invitedPlayers: GameNightRsvps[];
  suggestedGames: Game[];
  notes: string;
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

export interface UpdateGameNightRsvp {
  gameNightId: number;
  playerId: number;
  state: GameNightRsvpState;
}

export interface GameNightStatistics {
  upcomingCount: number;
  pendingResponses: number;
  gamesPlanned: number;
}
