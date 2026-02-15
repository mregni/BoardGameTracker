import { Player } from '../Player/Player';
import { Location } from '../Location/Location';
import { Game } from '../Games/Game';

export enum GameNightRsvpState {
  Accepted = 'accepted',
  Declined = 'declined',
  Pending = 'pending',
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
  id: number;
  gameNightId: number;
  playerId: number;
  state: GameNightRsvpState;
}

export interface GameNightStatistics {
  upcomingCount: number;
  pendingResponses: number;
  gamesPlanned: number;
}
