import { AxiosError } from 'axios';
import { UseQueryResult, useQuery } from '@tanstack/react-query';

import { FailResult, QUERY_KEYS, ListResult, Player } from '@/models';
import { getPlayers } from '@/hooks/services/playerService';

interface Props {
  players: UseQueryResult<ListResult<Player>, AxiosError<FailResult>>;
}

export const usePlayersPage = (): Props => {
  const players = useQuery<ListResult<Player>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.players],
    queryFn: ({ signal }) => getPlayers(signal),
  });

  return {
    players,
  };
};
