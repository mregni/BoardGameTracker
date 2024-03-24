export interface TopPlayer {
    playerId: number;
    playCount: number;
    wins: number;
    winPercentage: number;
    trend: Trend;
}

export enum Trend {
  Up = 0,
  Down = 1,
  Equal = 2
}