import {ListResult, Player, Result} from '../../models';
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
