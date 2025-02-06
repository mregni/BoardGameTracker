import { AxiosError } from 'axios';
import { useQuery } from '@tanstack/react-query';

import { Player, FailResult, QUERY_KEYS, PlayerStatistics } from '@/models';
import { getPlayer, getPlayerStatistics } from '@/hooks/services/playerService';

interface Props {
  id: string | undefined;
}

export const usePlayer = ({ id }: Props) => {
  const player = useQuery<Player, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.player, id],
    queryFn: ({ signal }) => getPlayer(id!, signal),
    enabled: id !== undefined,
  });

  const statistics = useQuery<PlayerStatistics, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.player, id, QUERY_KEYS.playerStatistics],
    queryFn: ({ signal }) => getPlayerStatistics(id!, signal),
    enabled: id !== undefined,
  });

  return {
    player,
    statistics,
  };
};
