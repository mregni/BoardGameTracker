export interface PlayerStatistics {
	playCount: number;
	winCount: number;
	mostPlayedGames: MostPlayedGame[];
	totalPlayedTime: number;
	distinctGameCount: number;
	coopWinCount: number;
	coopLossCount: number;
	rivals: PlayerRival[];
}

export interface PlayerRival {
	playerId: number;
	sessions: number;
	wins: number;
	losses: number;
}

export interface MostPlayedGame {
	id: number;
	image: string;
	title: string;
	totalWins: number;
	totalSessions: number;
	winningPercentage: number;
}
