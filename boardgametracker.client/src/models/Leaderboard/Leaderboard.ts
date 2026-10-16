export type LeaderboardRankBy = "wins" | "winRate" | "sessions";

export interface LeaderboardEntry {
	rank: number | null;
	playerId: number;
	name: string;
	image: string | null;
	playCount: number;
	winCount: number;
	podiumCount: number;
	winPercentage: number;
	minutesPlayed: number;
	isRanked: boolean;
	sessionsToRank: number;
}

export interface Leaderboard {
	mostPlays: LeaderboardEntry | null;
	mostWins: LeaderboardEntry | null;
	bestWinRate: LeaderboardEntry | null;
	mostTimePlayed: LeaderboardEntry | null;
	minimumSessions: number;
	minimumPlayersForPodium: number;
	rankBy: LeaderboardRankBy;
	showPodium: boolean;
	rankedCount: number;
	winCount: number;
	coopWinCount: number;
	players: LeaderboardEntry[];
}

export interface LeaderboardParams {
	from?: string;
	to?: string;
	gameId?: number;
	minSessions: number;
	rankBy: LeaderboardRankBy;
}
