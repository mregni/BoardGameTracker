export interface CompareResult {
  directWins: CompareRow<number>;
  mostWonGame: CompareRow<MostWonGame | null>;
  sessionCounts: CompareRow<number>;
  winCount: CompareRow<number>;
  winPercentageCount: CompareRow<number>;
  totalDuration: CompareRow<number>;
}

export interface CompareRow<T> {
  left: T;
  right: T;
}

export interface MostWonGame {
  gameId: number | null;
  count: number;
}
