import {BggSearch, Game, SearchResult} from '../models';
import {axiosInstance} from './axiosInstance';

const domain = 'bgg';

export const addGame = (search: BggSearch): Promise<SearchResult<Game>> => {
  return axiosInstance
    .post<SearchResult<Game>>(`${domain}/search`, {...search})
    .then((response) => {
      return response.data;
    });
};
