import { Player } from '../';

export interface GameStatistics {
  playCount: number;
  totalPlayedTime: number;
  pricePerPlay: number | null;
  highScore: number | null;
  averageScore: number | null;
  mostWinsPlayer: Player | null;
  lastPlayed: string | null;
}
