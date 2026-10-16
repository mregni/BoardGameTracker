export enum GameNightStatisticsPeriod {
	AllTime = "allTime",
	ThisYear = "thisYear",
	Last12Months = "last12Months",
}

export interface NightWinnerStatistic {
	playerId: number;
	rank: number;
	nightsWon: number;
	nightsAttended: number;
	gamesWon: number;
}

export interface NightHistory {
	gameNightId: number;
	title: string;
	startDate: Date;
	locationId: number;
	gamesPlayed: number;
	mostWins: number;
	winnerIds: number[];
	winnerStreak: number;
	coopOnly: boolean;
	sharedThroughCoop: boolean;
}

export interface NightGameStatistic {
	gameId: number;
	timesPlayed: number;
	nights: number;
	timesPlanned: number;
}

export interface PlannedGameStatistic {
	gameId: number;
	timesPlanned: number;
}

export interface PlayerStreak {
	playerId: number;
	length: number;
	from: Date;
	to: Date;
}

export interface GameStreak {
	gameId: number;
	length: number;
	from: Date;
	to: Date;
	isCurrent: boolean;
}

export interface GameNightStreaks {
	currentWinner: PlayerStreak | null;
	longestWinner: PlayerStreak | null;
	currentAttendance: PlayerStreak | null;
	longestGame: GameStreak | null;
}

export interface GameNightReference {
	gameNightId: number;
	title: string;
	startDate: Date;
}

export interface NightAttendance {
	playerId: number;
	nightsAttended: number;
	nightsHosted: number;
	recentNights: boolean[];
}

export interface GameNightRecord {
	gameNightId: number;
	title: string;
	startDate: Date;
	gamesPlayed: number;
	value: number;
	gameId: number | null;
	at: Date | null;
}

export interface GameNightRecords {
	longestNight: GameNightRecord | null;
	mostGames: GameNightRecord | null;
	biggestTable: GameNightRecord | null;
	latestFinish: GameNightRecord | null;
}

export interface GameNightStatistics {
	period: GameNightStatisticsPeriod;
	totalNights: number;
	nightsWithSessions: number;
	nights: number;
	gamesPlayed: number;
	totalMinutes: number;
	averageTable: number;
	averageDaysBetweenNights: number | null;
	tiedNights: number;
	winners: NightWinnerStatistic[];
	history: NightHistory[];
	games: NightGameStatistic[];
	plannedNeverPlayed: PlannedGameStatistic[];
	streaks: GameNightStreaks;
	recentNights: GameNightReference[];
	attendance: NightAttendance[];
	records: GameNightRecords;
}
