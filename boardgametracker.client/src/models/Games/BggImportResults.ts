import { GameState } from './GameState';

export interface BggImportResults {
  statusCode: number;
  games: ImportGame[];
}

export interface ImportGame {
  title: string;
  bggId: number;
  state: GameState;
  imageUrl: string;
  checked: boolean;
  inCollection: boolean;
  hasScoring: boolean;
  price: number;
  addedDate: Date;
  lastModified: string;
}
