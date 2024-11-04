import { AxiosError } from 'axios';
import { useQueryClient, useQuery } from '@tanstack/react-query';

import { Player, FailResult, QUERY_KEYS, PlayerStatistics } from '@/models';
import { getPlayer, getPlayerStatistics, deletePlayer as deletePlayerCall } from '@/hooks/services/playerService';

interface Props {
  id: string | undefined;
  onDeleteSuccess: () => void;
  onDeleteError: () => void;
}

export const usePlayer = ({ id, onDeleteError, onDeleteSuccess }: Props) => {
  const queryClient = useQueryClient();

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

  const deletePlayer = async () => {
    if (id !== undefined) {
      try {
        await deletePlayerCall(id);

        await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
        onDeleteSuccess();
      } catch {
        onDeleteError();
      }
    }
  };

  return {
    player,
    statistics,
    deletePlayer,
  };
};
