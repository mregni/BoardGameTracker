import { axiosInstance } from '../utils/axiosInstance';
import { Player, PlayerStatistics, Session } from '../models';

const domain = 'player';

export const getPlayersCall = (): Promise<Player[]> => {
  return axiosInstance.get<Player[]>(domain).then((response) => {
    return response.data;
  });
};

export const addPlayerCall = (player: Player): Promise<Player> => {
  return axiosInstance.post<Player>(domain, player).then((response) => {
    return response.data;
  });
};

export const updatePlayerCall = (player: Player): Promise<Player> => {
  return axiosInstance.put<Player>(domain, player).then((response) => {
    return response.data;
  });
};

export const getPlayerCall = (id: number): Promise<Player> => {
  return axiosInstance.get<Player>(`${domain}/${id}`).then((response) => {
    return response.data;
  });
};

export const getPlayerStatisticsCall = (id: number): Promise<PlayerStatistics> => {
  return axiosInstance.get<PlayerStatistics>(`${domain}/${id}/statistics`).then((response) => {
    return response.data;
  });
};

export const deletePlayerCall = (id: number): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
};

export const getPlayerSessionsCall = (id: number, count?: number): Promise<Session[]> => {
  return axiosInstance.get<Session[]>(`${domain}/${id}/sessions`, { params: { count } }).then((response) => {
    return response.data;
  });
};
