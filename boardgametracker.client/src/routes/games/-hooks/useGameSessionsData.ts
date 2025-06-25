import { useMemo } from 'react';
import { useQueries, useQueryClient } from '@tanstack/react-query';

import { deleteSessionCall } from '@/services/sessionService';
import { getSettings } from '@/services/queries/settings';
import { getPlayers } from '@/services/queries/players';
import { getGame, getGameSessions } from '@/services/queries/games';
import { QUERY_KEYS } from '@/models';

interface UseGameDataProps {
  gameId: string;
  onDeleteError?: () => void;
  onDeleteSuccess?: () => void;
}

export const useGameSessionsData = ({ gameId, onDeleteError, onDeleteSuccess }: UseGameDataProps) => {
  const queryClient = useQueryClient();

  const [gameQuery, settingsQuery, sessionsQuery, playersQuery] = useQueries({
    queries: [getGame(gameId), getSettings(), getGameSessions(gameId), getPlayers()],
  });

  const game = useMemo(() => gameQuery.data, [gameQuery.data]);
  const sessions = useMemo(() => sessionsQuery.data ?? [], [sessionsQuery.data]);
  const settings = useMemo(() => settingsQuery.data, [settingsQuery.data]);
  const players = useMemo(() => playersQuery.data ?? [], [playersQuery.data]);
  const isLoading = gameQuery.isLoading || settingsQuery.isLoading;

  const deleteSession = (id: string) => {
    void deleteSessionCall(id)
      .then(() => {
        queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.game, gameId, QUERY_KEYS.sessions] });
        onDeleteSuccess?.();
      })
      .catch(() => {
        onDeleteError?.();
      });
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
