export interface Play {
  comment: string;
  endend: boolean;
  gameId: number;
  players: PlayPlayer[];
  sessions: PlaySessions[];
}

export interface PlayPlayer {
  playerId: number;
  won: boolean;
  firstPlay: boolean;
  color?: string;
  score?: number;
  team?: string;
  characterName?: string;
}

export interface PlaySessions {
  start: Date;
  end: Date;
}