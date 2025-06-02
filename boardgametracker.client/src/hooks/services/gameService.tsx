import { axiosInstance } from '../../utils/axiosInstance';
import { TopPlayer } from '../../models/Games/TopPlayer';
import { BggSearch, Game, GameStatistics, Session } from '../../models';

import { CreateGame } from '@/models/Games/CreateGame';

const domain = 'game';

export const getGames = (signal: AbortSignal): Promise<Game[]> => {
  return axiosInstance.get<Game[]>(domain, { signal }).then((response) => {
    return response.data;
  });
};

export const saveGameCall = (game: CreateGame): Promise<Game> => {
  return axiosInstance.post<Game>(domain, { ...game }).then((response) => {
    return response.data;
  });
};

export const getGame = (id: string, signal: AbortSignal): Promise<Game> => {
  return axiosInstance.get<Game>(`${domain}/${id}`, { signal }).then((response) => {
    return response.data;
  });
};

export const getGameSessions = (id: string, signal: AbortSignal): Promise<Session[]> => {
  return axiosInstance.get<Session[]>(`${domain}/${id}/sessions`, { signal }).then((response) => {
    return response.data;
  });
};

export const addGameWithBgg = (search: BggSearch): Promise<Game> => {
  return axiosInstance.post<Game>(`${domain}/bgg`, { ...search }).then((response) => {
    return response.data;
  });
};

export const getGameStatistics = (id: string, signal: AbortSignal): Promise<GameStatistics> => {
  return axiosInstance.get<GameStatistics>(`${domain}/${id}/stats`, { signal }).then((response) => {
    return response.data;
  });
};

export const getTopPlayers = (id: string, signal: AbortSignal): Promise<TopPlayer[]> => {
  return axiosInstance.get<TopPlayer[]>(`${domain}/${id}/top`, { signal }).then((response) => {
    return response.data;
  });
};

export const getChart = <T,>(id: string, chartName: string, signal: AbortSignal): Promise<T[]> => {
  return axiosInstance.get<T[]>(`${domain}/${id}/chart/${chartName}`, { signal }).then((response) => {
    return response.data;
  });
};

export const deleteGameCall = (id: string): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
};

export const updateGameCall = (game: Game): Promise<Game> => {
  return axiosInstance.put<Game>(domain, { ...game }).then((response) => {
    return response.data;
  });
};
