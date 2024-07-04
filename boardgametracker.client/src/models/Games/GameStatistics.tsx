import { Player } from '../';

export interface GameStatistics {
  playCount: number;
  totalPlayedTime: number;
  pricePerPlay: number | null;
  highScore: number | null;
  averageScore: number | null;
  averagePlayTime: number | null;
  mostWinsPlayer: MostWinner | null;
  lastPlayed: string | null;
}

export interface MostWinner extends Player {
  totalWins: number;
}
