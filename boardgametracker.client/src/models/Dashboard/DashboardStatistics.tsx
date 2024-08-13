import { MostWinner } from '../Games/GameStatistics';

export interface DashboardStatistics {
  gameCount: number;
  playerCount: number;
  locationCount: number;
  sessionCount: number;
  totalCost: number | null;
  meanPayed: number | null;
  totalPlayTime: number;
  meanPlayTime: number;
  mostWinningPlayer: MostWinner;
}
