import { AxiosError } from 'axios';
import { useQuery, useQueryClient } from '@tanstack/react-query';

import { FailResult, QUERY_KEYS, Player } from '@/models';
import { getPlayers } from '@/hooks/services/playerService';
import { deletePlayer as deletePlayerCall } from '@/hooks/services/playerService';

interface Props {
  onDeleteSuccess?: () => void;
  onDeleteError?: () => void;
}

export const usePlayers = ({ onDeleteSuccess, onDeleteError }: Props) => {
  const queryClient = useQueryClient();

  const players = useQuery<Player[], AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.players],
    queryFn: ({ signal }) => getPlayers(signal),
  });

  const deletePlayer = async (id: string) => {
    try {
      await deletePlayerCall(id);
      players.refetch();
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      onDeleteSuccess?.();
    } catch {
      onDeleteError?.();
    }
  };

  return {
    players: players.data ?? [],
    deletePlayer,
  };
};
