import { axiosInstance } from '../../utils/axiosInstance';
import { Player, PlayerStatistics } from '../../models';

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

export const deletePlayer = (id: number): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
};
