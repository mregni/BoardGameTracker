export interface PlayerSession {
  sessionId: string;
  playerId: string;
  won: boolean;
  firstPlay: boolean;
  isBot: boolean;
  score?: number;
}
