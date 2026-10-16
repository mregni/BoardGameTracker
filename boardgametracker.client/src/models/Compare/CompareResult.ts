export type CompareOutcome = "playerOne" | "playerTwo" | "both" | "other";

export type CompareSide = "playerOne" | "playerTwo";

export interface CompareResult {
	winCount: CompareRow<number>;
	winPercentage: CompareRow<number>;
	sessionCounts: CompareRow<number>;
	totalDuration: CompareRow<number>;
	directWins: CompareRow<number>;
	wonByOthers: number;
	totalSessionsTogether: number;
	minutesPlayed: number;
	preferredGame: PreferredGame | null;
	longestSessionTogether: LongestSession | null;
	firstGameTogether: FirstGameTogether | null;
	closestGame: ClosestGame | null;
	streak: CompareStreak | null;
	games: CompareGame[];
	recentSessions: CompareSession[];
	bestGames: CompareRow<number[]>;
}

export interface CompareRow<T> {
	playerOne: T;
	playerTwo: T;
}

export interface PreferredGame {
	gameId: number | null;
	sessionCount: number;
}

export interface LongestSession {
	gameId: number;
	minutes: number;
}

export interface ClosestGame {
	playerId: number | null;
	gameId: number | null;
	scoringDifference: number;
}

export interface FirstGameTogether {
	gameId: number | null;
	startDate: Date | null;
}

export interface CompareStreak {
	side: CompareOutcome;
	count: number;
}

export interface CompareGame {
	gameId: number;
	sessionCount: number;
	playerOneWins: number;
	playerTwoWins: number;
	otherWins: number;
	playerOneAverageScore: number | null;
	playerTwoAverageScore: number | null;
	lastPlayed: Date;
	lastOutcome: CompareOutcome;
}

export interface CompareSession {
	sessionId: number;
	gameId: number;
	start: Date;
	minutes: number;
	playerOneScore: number | null;
	playerTwoScore: number | null;
	outcome: CompareOutcome;
	otherWinnerId: number | null;
}

export interface ComparePair {
	playerOneId: number;
	playerTwoId: number;
	sessionCount: number;
}
