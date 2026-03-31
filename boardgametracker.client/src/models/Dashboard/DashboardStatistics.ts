import type { GameState } from "../Games/GameState";
import type { PlayByDayChartData } from "../Games/GameStatistics";
import type { MostPlayedGame } from "../Player/PlayerStatistics";

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
	start: Date;
	playerCount: number;
	winnerName: string;
	winnerId: number;
	durationInMinutes: number;
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
