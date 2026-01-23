import { PlayByDayChartData } from '../Games/GameStatistics';
import { GameState } from '../Games/GameState';

export interface DashboardStatistics {
  totalGames: number;
  activePlayers: number;
  sessionsPlayed: number;
  totalPlayedTime: number;
  totalCollectionValue: number | null;
  avgGamePrice: number | null;
  expansionsOwned: number;
  avgSessionTime: number;

  recentActivities: RecentActivity[];
  collection: GameStateChart[];
  mostPlayedGames: MostPlayedGame[];
  topPlayers: DashboardTopPlayer[];
  recentAddedGames: RecentGame[];
  sessionsByDayOfWeek: PlayByDayChartData[];
}

export interface GameStateChart {
  type: GameState;
  gameCount: number;
}

export interface RecentActivity {
  id: number;
  gameId: number;
  gameTitle: string;
  gameImage: string | null;
  start: Date | string;
  playerCount: number;
  winnerName: string;
  winnerId: string;
  durationInMinutes: number;
}

export interface MostPlayedGame {
  id: number;
  title: string;
  image: string | null;
  totalWins: number | null;
  totalSessions: number;
  winningPercentage: number | null;
}

export interface DashboardTopPlayer {
  id: number;
  name: string;
  image: string | null;
  playCount: number;
  winCount: number;
}

export interface RecentGame {
  id: number;
  title: string;
  image: string | null;
  additionDate: Date;
  price: number | null;
}
