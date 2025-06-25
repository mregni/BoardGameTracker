import { useMemo } from 'react';
import { useQueries, useQueryClient } from '@tanstack/react-query';

import { deleteSessionCall } from '@/services/sessionService';
import { getSettings } from '@/services/queries/settings';
import { getPlayer, getPlayers, getPlayerSessions } from '@/services/queries/players';
import { getGames } from '@/services/queries/games';
import { QUERY_KEYS } from '@/models';

interface UsePLayerDataProps {
  playerId: string;
  onDeleteSuccess?: () => void;
  onDeleteError?: () => void;
}

export const usePlayerSessionData = ({ playerId, onDeleteSuccess, onDeleteError }: UsePLayerDataProps) => {
  const queryClient = useQueryClient();

  const [settingsQuery, playerQuery, gamesQuery, sessionsQuery, playersQuery] = useQueries({
    queries: [getSettings(), getPlayer(playerId), getGames(), getPlayerSessions(playerId), getPlayers()],
  });

  const player = useMemo(() => playerQuery.data, [playerQuery.data]);
  const settings = useMemo(() => settingsQuery.data, [settingsQuery.data]);
  const games = useMemo(() => gamesQuery.data ?? [], [gamesQuery.data]);
  const sessions = useMemo(() => sessionsQuery.data ?? [], [sessionsQuery.data]);
  const players = useMemo(() => playersQuery.data ?? [], [playersQuery.data]);

  const isLoading = settingsQuery.isLoading || playerQuery.isLoading || gamesQuery.isLoading;

  const deleteSession = (id: string) => {
    void deleteSessionCall(id)
      .then(() => {
        queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.player, playerId, QUERY_KEYS.sessions] });
        onDeleteSuccess?.();
      })
      .catch(() => {
        onDeleteError?.();
      });
  };

  return {
    player,
    sessions,
    games,
    settings,
    players,
    isLoading,
    deleteSession,
  };
};
