import {SearchResultType} from './';

export interface SearchResult<T> {
  model: T | null;
  result: SearchResultType;
}