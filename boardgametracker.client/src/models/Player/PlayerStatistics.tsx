import { Game } from '../Games/Game';

export interface PlayerStatistics {
  playCount: number;
  winCount: number;
  mostWinsGame: BestGame;
  totalPlayedTime: number;
  distinctGameCount: number;
}

export interface BestGame extends Game {
  totalWins: number;
}
