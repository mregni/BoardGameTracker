export interface PlayPlayer {
  id: number;
  playerId: number;
  won: boolean;
  firstPlay: boolean;
  isBot: boolean;
  score?: number;
}
