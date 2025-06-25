import { Player } from '..';

export interface MostWinner extends Player {
  totalWins: number;
}

export interface GameStatistics {
  gameStats: GameStats;
  topPlayers: TopPlayer[];
  playByDayChart: PlayByDayChartData[];
  playerCountChart: PlayerCountChartData[];
  playerScoringChart: PlayerScoringChartData[];
  scoreRankChart: ScoreRankChartData[];
}

export interface GameStats {
  playCount: number;
  totalPlayedTime: number;
  pricePerPlay: number | null;
  highScore: number | null;
  averageScore: number | null;
  averagePlayTime: number | null;
  mostWinsPlayer: MostWinner | null;
  lastPlayed: string | null;
  expansionCount: number | null;
}

export interface TopPlayer {
  playerId: number;
  playCount: number;
  wins: number;
  winPercentage: number;
  trend: Trend;
}

export interface PlayByDayChartData {
  dayOfWeek: number;
  playCount: number;
}

export interface PlayerCountChartData {
  players: number;
  playCount: number;
}

export interface PlayerScoringChartData {
  dateTime: Date | string;
  series: XValue[];
}

export interface ScoreRankChartData {
  key: string;
  score: number;
  playerId: number;
}

export interface XValue {
  id: string;
  value: number;
}

export enum Trend {
  Up = 0,
  Down = 1,
  Equal = 2,
}
