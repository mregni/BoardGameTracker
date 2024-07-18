export interface PlayerSession {
  id: number;
  playerId: number;
  won: boolean;
  firstPlay: boolean;
  isBot: boolean;
  score?: number;
}
