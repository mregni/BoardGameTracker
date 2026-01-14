export interface PlayerStatistics {
  playCount: number;
  winCount: number;
  mostPlayedGames: MostPlayedGame[];
  totalPlayedTime: number;
  distinctGameCount: number;
}

export interface MostPlayedGame {
  id: number;
  image: string;
  title: string;
  totalWins: number;
  totalSessions: number;
  winningPercentage: number;
}
