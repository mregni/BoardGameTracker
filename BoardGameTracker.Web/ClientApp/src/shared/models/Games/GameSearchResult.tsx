import {Game, SearchResult} from '../';

export interface GameSearchResult {
  game: Game | null;
  result: SearchResult;
}