export interface LeaderboardEntry {
	rank: number;
	playerId: number;
	name: string;
	image: string | null;
	playCount: number;
	winCount: number;
	podiumCount: number;
	winPercentage: number;
	minutesPlayed: number;
}

export interface Leaderboard {
	mostPlays: LeaderboardEntry | null;
	mostWins: LeaderboardEntry | null;
	bestWinRate: LeaderboardEntry | null;
	mostTimePlayed: LeaderboardEntry | null;
	minimumPlaysForWinRate: number;
	players: LeaderboardEntry[];
}
