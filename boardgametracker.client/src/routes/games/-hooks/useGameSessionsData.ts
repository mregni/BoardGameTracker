import { useQueries, useQueryClient } from '@tanstack/react-query';

import { deleteSessionCall } from '@/services/sessionService';
import { getSettings } from '@/services/queries/settings';
import { getPlayers } from '@/services/queries/players';
import { getGame, getGameSessions } from '@/services/queries/games';
import { QUERY_KEYS } from '@/models';

interface UseGameSessionsDataProps {
  gameId: number;
  onDeleteError?: () => void;
  onDeleteSuccess?: () => void;
}

export const useGameSessionsData = ({ gameId, onDeleteError, onDeleteSuccess }: UseGameSessionsDataProps) => {
  const queryClient = useQueryClient();

  const [gameQuery, settingsQuery, sessionsQuery, playersQuery] = useQueries({
    queries: [getGame(gameId), getSettings(), getGameSessions(gameId), getPlayers()],
  });

  const game = gameQuery.data;
  const sessions = sessionsQuery.data ?? [];
  const settings = settingsQuery.data;
  const players = playersQuery.data ?? [];
  const isLoading = gameQuery.isLoading || settingsQuery.isLoading;

  const deleteSession = async (id: string) => {
    try {
      await deleteSessionCall(id);
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.game, gameId, QUERY_KEYS.sessions] });
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      onDeleteSuccess?.();
    } catch {
      onDeleteError?.();
    }
  };

  return {
    isLoading,
    game,
    settings,
    sessions,
    players,
    deleteSession,
  };
};
