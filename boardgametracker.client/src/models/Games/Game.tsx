import { GameState } from './GameState';
import { GameType } from './GameType';
import { PersonType } from './PersonType';

export interface Game {
  id: number;
  title: string;
  description: string;
  yearPublished: number | null;
  image: string;
  thumbnail: string;
  minPlayers: number | null;
  maxPlayers: number | null;
  minPlayTime: number | null;
  maxPlayTime: number | null;
  minAge: number | null;
  rating: number | null;
  weight: number | null;
  bggId: number | null;
  type: GameType;
  state: GameState;
  baseGameId: number | null;
  baseGame: Game;
  expansions: Game[];
  categories: GameLink[];
  mechanics: GameLink[];
  people: GamePerson[];
  hasScoring: boolean;
}

export interface GameLink {
  id: number;
  name: string;
}

export interface GamePerson extends GameLink {
  type: PersonType;
}
