import { axiosInstance } from '../utils/axiosInstance';
import { BggImportResults, BggSearch, Game, GameStatistics, ImportGame, Session, Shame } from '../models';

import { ShameStatistics } from '@/models/Games/ShameStatistics';
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

export const getGameCall = (id: number): Promise<Game> => {
  return axiosInstance.get<Game>(`${domain}/${id}`).then((response) => {
    return response.data;
  });
};

export const getGameSessionsCall = (id: number, count?: number): Promise<Session[]> => {
  return axiosInstance.get<Session[]>(`${domain}/${id}/sessions`, { params: { count } }).then((response) => {
    return response.data;
  });
};

export const addGameWithBggCall = (search: BggSearch): Promise<Game> => {
  return axiosInstance.post<Game>(`${domain}/bgg/search`, { ...search }).then((response) => {
    return response.data;
  });
};

export const getBggCollectionCall = (username: string): Promise<BggImportResults> => {
  return axiosInstance.get<BggImportResults>(`${domain}/bgg/import`, { params: { username } }).then((response) => {
    return response.data;
  });
};

export const getGameStatisticsCall = (id: number): Promise<GameStatistics> => {
  return axiosInstance.get<GameStatistics>(`${domain}/${id}/statistics`).then((response) => {
    return response.data;
  });
};

export const deleteGameCall = (id: number): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
};

export const updateGameCall = (game: Game): Promise<Game> => {
  return axiosInstance.put<Game>(domain, { ...game }).then((response) => {
    return response.data;
  });
};

export const getGameExpansionsCall = (id: number): Promise<ExpansionLink[]> => {
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

export const importGamesCall = (games: ImportGame[]): Promise<boolean> => {
  return axiosInstance.post<boolean>(`${domain}/bgg/import`, { games: [...games] }).then((response) => {
    return response.data;
  });
};

export const deleteExpansionCall = (id: number, gameId: number): Promise<void> => {
  return axiosInstance.delete(`${domain}/${gameId}/expansion/${id}`);
};

export const getShamesCall = (): Promise<Shame[]> => {
  return axiosInstance.get<Shame[]>(`${domain}/shames`).then((response) => {
    return response.data;
  });
};

export const getShameStatisticsCall = (): Promise<ShameStatistics> => {
  return axiosInstance.get<ShameStatistics>(`${domain}/shames/statistics`).then((response) => {
    return response.data;
  });
};
