import { AxiosError } from 'axios';
import { useQueryClient, useQuery, UseQueryResult } from '@tanstack/react-query';

import { useToast } from '@/providers/BgtToastProvider';
import { Result, Player, FailResult, QUERY_KEYS, PlayerStatistics } from '@/models';
import { getPlayer, getPlayerStatistics, deletePlayer as deletePlayerCall } from '@/hooks/services/playerService';

interface Props {
  player: UseQueryResult<Result<Player>, AxiosError<FailResult>>;
  statistics: UseQueryResult<Result<PlayerStatistics>, AxiosError<FailResult>>;
  deletePlayer: () => Promise<void>;
}

export const usePlayerDetailPage = (id: string | undefined): Props => {
  const queryClient = useQueryClient();
  const { showInfoToast, showErrorToast } = useToast();

  const player = useQuery<Result<Player>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.player, id],
    queryFn: ({ signal }) => getPlayer(id!, signal),
    enabled: id !== undefined,
  });

  const statistics = useQuery<Result<PlayerStatistics>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.player, id, QUERY_KEYS.playerStatistics],
    queryFn: ({ signal }) => getPlayerStatistics(id!, signal),
    enabled: id !== undefined,
  });

  const deletePlayer = async () => {
    if (id !== undefined) {
      try {
        await deletePlayerCall(id);
        showInfoToast('player.delete.successfull');
        await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      } catch {
        showErrorToast('player.delete.failed');
      }
    }
  };

  return {
    player,
    statistics,
    deletePlayer,
  };
};
