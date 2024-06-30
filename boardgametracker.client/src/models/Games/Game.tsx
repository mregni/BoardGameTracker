import { PersonType } from './PersonType';
import { GameType } from './GameType';
import { GameState } from './GameState';

export interface Game {
  id: number;
  title: string;
  description: string;
  yearPublished: number | null;
  image: string;
  backgroundImage: string;
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
