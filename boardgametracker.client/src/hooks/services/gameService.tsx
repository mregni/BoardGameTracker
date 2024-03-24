import {BggSearch, Game, GameStatistics, ListResult, Play, Result} from '../../models';
import {TopPlayer} from '../../models/Games/TopPlayer';
import {axiosInstance} from '../../utils/axiosInstance';

const domain = 'game';

export const getGames = (signal: AbortSignal): Promise<ListResult<Game>> => {
  return axiosInstance
    .get<ListResult<Game>>(domain, { signal })
    .then((response) => {
      return response.data;
    });
};

export const getGame = (id: string, signal: AbortSignal): Promise<Result<Game>> => {
  return axiosInstance
    .get<Result<Game>>(`${domain}/${id}`, { signal })
    .then((response) => {
      return response.data;
    });
  }

export const addGameWithBgg = (search: BggSearch): Promise<Result<Game>> => {
  return axiosInstance
    .post<Result<Game>>(`${domain}/bgg`, { ...search })
    .then((response) => {
      return response.data;
    });
}

export const getGameStatistics = (id: string, signal: AbortSignal): Promise<Result<GameStatistics>> => {
  return axiosInstance
    .get<Result<GameStatistics>>(`${domain}/${id}/stats`, { signal })
    .then((response) => {
      return response.data;
    });
}

export const getGamePlays = (id: string, signal: AbortSignal): Promise<Result<Play[]>> => {
  return axiosInstance
    .get<Result<Play[]>>(`${domain}/${id}/plays`, { signal })
    .then((response) => {
      return response.data;
    });
}

export const getTopPlayers = (id: string, signal: AbortSignal): Promise<Result<TopPlayer[]>> => {
  return axiosInstance
    .get<Result<TopPlayer[]>>(`${domain}/${id}/top`, { signal })
    .then((response) => {
      return response.data;
    });
}