import { axiosInstance } from '../utils/axiosInstance';
import { BggSearch, Game, GameStatistics, Session } from '../models';

import { ExpansionLink, Expansion, ExpansionUpdate, CreateGame } from '@/models/';

const domain = 'game';

export const getGamesCall = (): Promise<Game[]> => {
  return axiosInstance.get<Game[]>(domain).then((response) => {
    return response.data;
  });
};

export const saveGameCall = (game: CreateGame): Promise<Game> => {
  return axiosInstance.post<Game>(domain, { ...game }).then((response) => {
    return response.data;
  });
};

export const getGameCall = (id: string): Promise<Game> => {
  return axiosInstance.get<Game>(`${domain}/${id}`).then((response) => {
    return response.data;
  });
};

export const getGameSessionsCall = (id: string): Promise<Session[]> => {
  return axiosInstance.get<Session[]>(`${domain}/${id}/sessions`).then((response) => {
    return response.data;
  });
};

export const addGameWithBggCall = (search: BggSearch): Promise<Game> => {
  return axiosInstance.post<Game>(`${domain}/bgg`, { ...search }).then((response) => {
    return response.data;
  });
};

export const getGameStatisticsCall = (id: string): Promise<GameStatistics> => {
  return axiosInstance.get<GameStatistics>(`${domain}/${id}/statistics`).then((response) => {
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

export const getGameExpansionsCall = (id: string): Promise<ExpansionLink[]> => {
  return axiosInstance.get<ExpansionLink[]>(`${domain}/${id}/expansions`).then((response) => {
    return response.data;
  });
};

export const saveGameExpansionCall = (expansionUpdate: ExpansionUpdate): Promise<Expansion[]> => {
  return axiosInstance
    .post<
      Expansion[]
    >(`${domain}/${expansionUpdate.gameId}/expansions`, { expansionBggIds: expansionUpdate.expansionBggIds })
    .then((response) => {
      return response.data;
    });
};
