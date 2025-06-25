import { useMemo } from 'react';
import { useQueries, useQueryClient } from '@tanstack/react-query';

import { getSettings } from '@/services/queries/settings';
import { getGame, getGameStatistics } from '@/services/queries/games';
import { deleteGameCall } from '@/services/gameService';
import { QUERY_KEYS } from '@/models';

interface UseGameDataProps {
  gameId: string;
  onDeleteError?: () => void;
  onDeleteSuccess?: () => void;
}

export const useGameData = ({ gameId, onDeleteError, onDeleteSuccess }: UseGameDataProps) => {
  const queryClient = useQueryClient();

  const [gameQuery, settingsQuery, statisticsQuery] = useQueries({
    queries: [getGame(gameId), getSettings(), getGameStatistics(gameId)],
  });

  const game = useMemo(() => gameQuery.data, [gameQuery.data]);
  const settings = useMemo(() => settingsQuery.data, [settingsQuery.data]);
  const statistics = useMemo(() => statisticsQuery.data, [statisticsQuery.data]);
  const isLoading = gameQuery.isLoading || settingsQuery.isLoading;

  const deleteGame = async () => {
    if (gameId !== undefined) {
      try {
        await deleteGameCall(gameId);
        queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
        queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
        onDeleteSuccess?.();
      } catch {
        onDeleteError?.();
      }
    }
  };

  return {
    isLoading,
    game,
    deleteGame,
    settings,
    statistics,
  };
};
