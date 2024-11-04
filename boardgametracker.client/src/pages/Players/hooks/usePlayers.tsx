import { AxiosError } from 'axios';
import { useQuery } from '@tanstack/react-query';

import { FailResult, QUERY_KEYS, Player } from '@/models';
import { getPlayers } from '@/hooks/services/playerService';

export const usePlayers = () => {
  const players = useQuery<Player[], AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.players],
    queryFn: ({ signal }) => getPlayers(signal),
  });

  return {
    players,
  };
};
