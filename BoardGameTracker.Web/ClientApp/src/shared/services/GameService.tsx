import {Game, ListResult, SearchResult} from '../models';
import {axiosInstance} from './axiosInstance';

const domain = 'game';

export const getAllGames = (): Promise<ListResult<Game>> => {
  return axiosInstance
    .get<ListResult<Game>>(domain)
    .then((response) => {
      return response.data;
    });
};

export const getGame = (id: string): Promise<SearchResult<Game>> => {
  return axiosInstance
  .get<SearchResult<Game>>(`${domain}/${id}`)
  .then((response) => {
    return response.data;
  });
}

export const deleteGameCall = (id: number): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
}