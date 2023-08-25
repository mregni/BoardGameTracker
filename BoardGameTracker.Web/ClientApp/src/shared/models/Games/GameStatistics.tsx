import {Player} from '../';

export interface GameStatistics {
    playCount: number;
    totalPlayedTime: number;
    pricePerPlay: number | null;
    uniquePlayerCount: number;
    highScore: number | null;
    averageScore: number | null;
    mostWinsPlayer: Player | null;
}