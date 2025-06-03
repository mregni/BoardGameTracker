import { axiosInstance } from '../../utils/axiosInstance';
import { Player, PlayerStatistics, Session } from '../../models';

const domain = 'player';

export const getPlayers = (signal: AbortSignal): Promise<Player[]> => {
  return axiosInstance.get<Player[]>(domain, { signal }).then((response) => {
    return response.data;
  });
};

export const addPlayer = (player: Player): Promise<Player> => {
  return axiosInstance.post<Player>(domain, player).then((response) => {
    return response.data;
  });
};

export const updatePlayer = (player: Player): Promise<Player> => {
  return axiosInstance.put<Player>(domain, player).then((response) => {
    return response.data;
  });
};

export const getPlayer = (id: string, signal: AbortSignal): Promise<Player> => {
  return axiosInstance.get<Player>(`${domain}/${id}`, { signal }).then((response) => {
    return response.data;
  });
};

export const getPlayerStatistics = (id: string, signal: AbortSignal): Promise<PlayerStatistics> => {
  return axiosInstance.get<PlayerStatistics>(`${domain}/${id}/stats`, { signal }).then((response) => {
    return response.data;
  });
};

export const deletePlayer = (id: string): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
};

export const getPlayerSessions = (id: string, signal: AbortSignal): Promise<Session[]> => {
  return axiosInstance.get<Session[]>(`${domain}/${id}/sessions`, { signal }).then((response) => {
    return response.data;
  });
};
