import { axiosInstance } from '../../utils/axiosInstance';
import { ListResult, Player, PlayerStatistics, Result, Session } from '../../models';

const domain = 'player';

export const getPlayers = (signal: AbortSignal): Promise<ListResult<Player>> => {
  return axiosInstance.get<ListResult<Player>>(domain, { signal }).then((response) => {
    return response.data;
  });
};

export const addPlayer = (player: Player): Promise<Result<Player>> => {
  return axiosInstance.post<Result<Player>>(domain, player).then((response) => {
    return response.data;
  });
};

export const getPlayer = (id: string, signal: AbortSignal): Promise<Result<Player>> => {
  return axiosInstance.get<Result<Player>>(`${domain}/${id}`, { signal }).then((response) => {
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

export const getPlayerStatistics = (id: string, signal: AbortSignal): Promise<Result<PlayerStatistics>> => {
  return axiosInstance.get<Result<PlayerStatistics>>(`${domain}/${id}/stats`, { signal }).then((response) => {
    return response.data;
  });
};

export const deletePlayer = (id: string): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
};
