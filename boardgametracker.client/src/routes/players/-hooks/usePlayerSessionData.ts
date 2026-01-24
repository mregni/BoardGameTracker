import { useQueries, useQueryClient } from '@tanstack/react-query';

import { deleteSessionCall } from '@/services/sessionService';
import { getSettings } from '@/services/queries/settings';
import { getPlayer, getPlayers, getPlayerSessions } from '@/services/queries/players';
import { getGames } from '@/services/queries/games';
import { QUERY_KEYS } from '@/models';

interface UsePlayerSessionDataProps {
  playerId: number;
  onDeleteSuccess?: () => void;
  onDeleteError?: () => void;
}

export const usePlayerSessionData = ({ playerId, onDeleteSuccess, onDeleteError }: UsePlayerSessionDataProps) => {
  const queryClient = useQueryClient();

  const [settingsQuery, playerQuery, gamesQuery, sessionsQuery, playersQuery] = useQueries({
    queries: [getSettings(), getPlayer(playerId), getGames(), getPlayerSessions(playerId), getPlayers()],
  });

  const player = playerQuery.data;
  const settings = settingsQuery.data;
  const games = gamesQuery.data ?? [];
  const sessions = sessionsQuery.data ?? [];
  const players = playersQuery.data ?? [];

  const isLoading = settingsQuery.isLoading || playerQuery.isLoading || gamesQuery.isLoading;

  const deleteSession = async (id: string) => {
    try {
      await deleteSessionCall(id);
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.player, playerId, QUERY_KEYS.sessions] });
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      onDeleteSuccess?.();
    } catch {
      onDeleteError?.();
    }
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
