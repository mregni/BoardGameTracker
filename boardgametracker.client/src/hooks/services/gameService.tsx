import { axiosInstance } from '../../utils/axiosInstance';
import { TopPlayer } from '../../models/Games/TopPlayer';
import { BggSearch, Game, GameStatistics, ListResult, Session, Result } from '../../models';

const domain = 'game';

export const getGames = (signal: AbortSignal): Promise<ListResult<Game>> => {
  return axiosInstance.get<ListResult<Game>>(domain, { signal }).then((response) => {
    return response.data;
  });
};

export const getGame = (id: string, signal: AbortSignal): Promise<Result<Game>> => {
  return axiosInstance.get<Result<Game>>(`${domain}/${id}`, { signal }).then((response) => {
    return response.data;
  });
};

export const addGameWithBgg = (search: BggSearch): Promise<Result<Game>> => {
  return axiosInstance.post<Result<Game>>(`${domain}/bgg`, { ...search }).then((response) => {
    return response.data;
  });
};

export const getGameStatistics = (id: string, signal: AbortSignal): Promise<Result<GameStatistics>> => {
  return axiosInstance.get<Result<GameStatistics>>(`${domain}/${id}/stats`, { signal }).then((response) => {
    return response.data;
  });
};

export const getSessions = (
  id: string,
  page: number,
  pageCount: number,
  signal: AbortSignal
): Promise<ListResult<Session>> => {
  const skip = page * pageCount;
  return axiosInstance
    .get<ListResult<Session>>(`${domain}/${id}/sessions`, { params: { skip: skip, take: pageCount }, signal })
    .then((response) => {
      return response.data;
    });
};

export const getTopPlayers = (id: string, signal: AbortSignal): Promise<Result<TopPlayer[]>> => {
  return axiosInstance.get<Result<TopPlayer[]>>(`${domain}/${id}/top`, { signal }).then((response) => {
    return response.data;
  });
};

export const getChart = <T,>(id: string, chartName: string, signal: AbortSignal): Promise<Result<T[]>> => {
  return axiosInstance.get<Result<T[]>>(`${domain}/${id}/chart/${chartName}`, { signal }).then((response) => {
    return response.data;
  });
};

export const deleteGame = (id: string): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
};
