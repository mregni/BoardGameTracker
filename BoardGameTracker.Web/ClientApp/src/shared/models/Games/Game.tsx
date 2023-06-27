import {GameType} from './GameType';
import {ItemState} from './ItemState';

export interface Game {
  id: string;
  title: string;
  description: string;
  yearPublished: number;
  minPlayers: number;
  maxPlayers: number;
  playTime: number;
  minPlayTime: number;
  maxPlayTime: number;
  minAge: number;
  rating: number;
  thumbnail: string;
  bggid: number;
  gameState: ItemState;
  gameType: GameType;
}
