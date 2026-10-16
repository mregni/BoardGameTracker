import type { GameState } from "../Games/GameState";

export interface DashboardStatistics {
	totalGames: number;
	ownedGames: number;
	activePlayers: number;
	sessionsPlayed: number;
	totalPlayedTime: number;
	totalCollectionValue: number | null;
	pricedGameCount: number;
	expansionsOwned: number;
	avgSessionTime: number;
	sessionsThisMonth: number;
	activePlayersThisMonth: number;

	linkedPlayer: DashboardLinkedPlayer | null;
	nextGameNight: DashboardGameNight | null;
	plannedGameNights: number;
	shelfOfShame: DashboardShelfOfShame | null;

	recentActivities: RecentActivity[];
	collection: GameStateChart[];
	mostPlayedGames: DashboardMostPlayedGame[];
	mostPlayedGamesThisMonth: DashboardMostPlayedGame[];
	topPlayers: DashboardTopPlayer[];
	recentAddedGames: RecentGame[];
}

export interface DashboardLinkedPlayer {
	id: number;
	name: string;
	sessionsThisMonth: number;
	winsThisMonth: number;
}

export interface DashboardGame {
	id: number;
	title: string;
	image: string | null;
}

export interface DashboardGameNight {
	id: number;
	title: string;
	startDate: Date | string;
	locationName: string;
	hostId: number;
	hostName: string;
	acceptedCount: number;
	pendingCount: number;
	declinedCount: number;
	suggestedGames: DashboardGame[];
}

export interface DashboardMostPlayedGame {
	id: number;
	title: string;
	image: string | null;
	totalSessions: number;
}

export interface DashboardShelfOfShame {
	count: number;
	neverPlayedCount: number;
	totalValue: number | null;
}

export interface GameStateChart {
	type: GameState;
	gameCount: number;
}

export interface DashboardWinner {
	id: number;
	name: string;
	image: string | null;
}

export interface RecentActivity {
	id: number;
	gameId: number;
	gameTitle: string;
	gameImage: string | null;
	start: Date | string;
	playerCount: number;
	durationInMinutes: number;
	winners: DashboardWinner[];
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
	additionDate: Date | string | null;
	price: number | null;
}
