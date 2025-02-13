export interface PlayerSession {
  sessionId: number;
  playerId: number;
  won: boolean;
  firstPlay: boolean;
  isBot: boolean;
  score?: number;
}
