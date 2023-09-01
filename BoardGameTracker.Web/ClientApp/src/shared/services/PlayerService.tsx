import {CreationResult, ListResult, Play, Player, PlayerStatistics, SearchResult} from '../models';
import {axiosInstance} from './axiosInstance';

const domain = 'player';

export const getAllPlayer = (): Promise<ListResult<Player>> => {
  return axiosInstance
    .get<ListResult<Player>>(domain)
    .then((response) => {
      return response.data;
    });
};

export const addPlayer = (player: Player): Promise<CreationResult<Player>> => {
  return axiosInstance
    .post<CreationResult<Player>>(domain, player)
    .then((response) => {
      return response.data;
    });
};

export const getPlayer = (id: string): Promise<SearchResult<Player>> => {
  return axiosInstance
  .get<SearchResult<Player>>(`${domain}/${id}`)
  .then((response) => {
    return response.data;
  });
}

export const deletePlayerCall = (id: number): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
}

export const getPlayerStatistics = (id: number): Promise<SearchResult<PlayerStatistics>> => {
  return axiosInstance
    .get<SearchResult<PlayerStatistics>>(`${domain}/${id}/stats`)
    .then((response) => {
      return response.data;
    });
}
