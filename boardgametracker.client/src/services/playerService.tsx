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

export const getPlayerCall = (id: string): Promise<Player> => {
  return axiosInstance.get<Player>(`${domain}/${id}`).then((response) => {
    return response.data;
  });
};

export const getPlayerStatisticsCall = (id: string): Promise<PlayerStatistics> => {
  return axiosInstance.get<PlayerStatistics>(`${domain}/${id}/stats`).then((response) => {
    return response.data;
  });
};

export const deletePlayerCall = (id: string): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
};

export const getPlayerSessionsCall = (id: string): Promise<Session[]> => {
  return axiosInstance.get<Session[]>(`${domain}/${id}/sessions`).then((response) => {
    return response.data;
  });
};
