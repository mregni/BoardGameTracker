export interface CompareResult {
  directWins: CompareRow<number>;
  mostWonGame: CompareRow<MostWonGame | null>;
  lastWonGame: LastWonGame | null;

  totalDuration: CompareRow<number>;
  winPercentage: CompareRow<number>;
  sessionCounts: CompareRow<number>;
  winCount: CompareRow<number>;
  totalSessionsTogether: number;
  minutesPlayed: number;
  preferredGame: PreferredGame | null;

  longestSessionTogether: number | null;
  firstGameTogether: FirstGameTogether | null;
  closestGame: ClosestGame | null;
}

export interface CompareRow<T> {
  playerOne: T;
  playerTwo: T;
}

export interface MostWonGame {
  gameId: number | null;
  count: number;
}

export interface LastWonGame {
  playerId: number | null;
  gameId: number | null;
}

export interface PreferredGame {
  gameId: number | null;
  sessionCount: number;
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
