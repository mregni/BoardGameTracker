import { useQueries, useQueryClient } from '@tanstack/react-query';

import { getSettings } from '@/services/queries/settings';
import { getGame, getGameSessionsShortList, getGameStatistics } from '@/services/queries/games';
import { deleteExpansionCall, deleteGameCall } from '@/services/gameService';
import { QUERY_KEYS } from '@/models';

interface UseGameDataProps {
  gameId: number;
  onDeleteError?: () => void;
  onDeleteSuccess?: () => void;
  onDeleteExpansionSuccess?: () => void;
  onDeleteExpansionError?: () => void;
}

export const useGameData = (props: UseGameDataProps) => {
  const { gameId, onDeleteError, onDeleteSuccess, onDeleteExpansionSuccess, onDeleteExpansionError } = props;
  const queryClient = useQueryClient();

  const [gameQuery, settingsQuery, statisticsQuery, sessionsQuery] = useQueries({
    queries: [getGame(gameId), getSettings(), getGameStatistics(gameId), getGameSessionsShortList(gameId, 5)],
  });

  const game = gameQuery.data;
  const settings = settingsQuery.data;
  const statistics = statisticsQuery.data;
  const sessions = sessionsQuery.data;
  const isLoading =
    gameQuery.isLoading || settingsQuery.isLoading || sessionsQuery.isLoading || statisticsQuery.isLoading;

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

  const deleteExpansion = async (id: number, gameIdParam: number) => {
    try {
      await deleteExpansionCall(id, gameIdParam);
      queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.game, gameIdParam] });
      onDeleteExpansionSuccess?.();
    } catch {
      onDeleteExpansionError?.();
    }
  };

  return {
    isLoading,
    game,
    deleteGame,
    settings,
    statistics,
    sessions,
    deleteExpansion,
  };
};
