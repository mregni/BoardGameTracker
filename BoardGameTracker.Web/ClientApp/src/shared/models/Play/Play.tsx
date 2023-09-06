export interface Play {
  id: number;
  comment: string;
  ended: boolean;
  gameId: number;
  start: Date;
  minutes: number;
  players: PlayPlayer[];
  locationId: number;
}

export interface PlayPlayer {
  id: number;
  playerId: number;
  won: boolean;
  firstPlay: boolean;
  color?: string;
  score?: number;
  team?: string;
  characterName?: string;
}
