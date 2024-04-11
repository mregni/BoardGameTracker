import {ListResult, Player, PlayerStatistics, Result} from '../../models';
import {axiosInstance} from '../../utils/axiosInstance';

const domain = 'player';

export const getPlayers = (signal: AbortSignal): Promise<ListResult<Player>> => {
  return axiosInstance
    .get<ListResult<Player>>(domain, { signal })
    .then((response) => {
      return response.data;
    });
};

export const addPlayer = (player: Player): Promise<Result<Player>> => {
  return axiosInstance
    .post<Result<Player>>(domain, player)
    .then((response) => {
      return response.data;
    });
};

export const getPlayer = (id: string, signal: AbortSignal): Promise<Result<Player>> => {
  return axiosInstance
    .get<Result<Player>>(`${domain}/${id}`, { signal })
    .then((response) => {
      return response.data;
    });
}

export const getPlayerStatistics = (id: string, signal: AbortSignal): Promise<Result<PlayerStatistics>> => {
  return axiosInstance
    .get<Result<PlayerStatistics>>(`${domain}/${id}/stats`, { signal })
    .then((response) => {
      return response.data;
    });
}

export const deletePlayer = (id: number): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
}