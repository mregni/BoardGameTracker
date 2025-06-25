import { useMemo } from 'react';
import { useQueries, useQueryClient } from '@tanstack/react-query';

import { getPlayer, getPlayerStatistics } from '@/services/queries/players';
import { deletePlayerCall } from '@/services/playerService';
import { QUERY_KEYS } from '@/models';

interface UsePLayerDataProps {
  playerId: string;
  onDeleteSuccess?: () => void;
  onDeleteError?: () => void;
}

export const usePlayerData = ({ playerId, onDeleteSuccess, onDeleteError }: UsePLayerDataProps) => {
  const queryClient = useQueryClient();

  const [playerQuery, statisticsQuery] = useQueries({
    queries: [getPlayer(playerId), getPlayerStatistics(playerId)],
  });

  const player = useMemo(() => playerQuery.data, [playerQuery.data]);
  const statistics = useMemo(() => statisticsQuery.data, [statisticsQuery.data]);
  const isLoading = playerQuery.isLoading;

  const deletePlayer = async (id: string) => {
    try {
      await deletePlayerCall(id);
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      onDeleteSuccess?.();
    } catch {
      onDeleteError?.();
    }
  };

  return {
    player,
    statistics,
    isLoading,
    deletePlayer,
  };
};
