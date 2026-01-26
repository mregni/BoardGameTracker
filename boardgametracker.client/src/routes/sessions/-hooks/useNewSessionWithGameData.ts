import { useMutation, useQueries, useQueryClient } from '@tanstack/react-query';

import { addSessionCall } from '@/services/sessionService';
import { getGame } from '@/services/queries/games';
import { QUERY_KEYS } from '@/models';

interface Props {
  gameId: number;
  onSaveSuccess?: () => void;
  onSaveError?: () => void;
}

export const useNewSessionWithGameData = ({ gameId, onSaveSuccess, onSaveError }: Props) => {
  const queryClient = useQueryClient();
  const [gameQuery] = useQueries({
    queries: [getGame(gameId)],
  });

  const game = gameQuery.data;

  const saveSessionMutation = useMutation({
    mutationFn: addSessionCall,
    async onSuccess(sessionResult) {
      onSaveSuccess?.();

      const maps = sessionResult.playerSessions.map(async (x) => {
        return await queryClient.invalidateQueries({
          queryKey: [QUERY_KEYS.players, x.playerId, QUERY_KEYS.sessions],
        });
      });

      await queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.game, sessionResult.gameId],
      });

      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.shames] });

      await Promise.all(maps);
    },
    onError: () => {
      onSaveError?.();
    },
  });

  return {
    game,
    isLoading: gameQuery.isLoading,
    isPending: saveSessionMutation.isPending,
    saveSession: saveSessionMutation.mutateAsync,
  };
};
