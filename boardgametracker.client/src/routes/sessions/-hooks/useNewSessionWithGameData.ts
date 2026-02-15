import { useMutation, useQueries, useQueryClient } from '@tanstack/react-query';

import { addSessionCall } from '@/services/sessionService';
import { getGame } from '@/services/queries/games';
import { useToasts } from '@/routes/-hooks/useToasts';
import { QUERY_KEYS } from '@/models';

interface Props {
  gameId: number;
  onSuccess?: () => void;
}

export const useNewSessionWithGameData = ({ gameId, onSuccess }: Props) => {
  const queryClient = useQueryClient();
  const { successToast, errorToast } = useToasts();

  const [gameQuery] = useQueries({
    queries: [getGame(gameId)],
  });

  const game = gameQuery.data;

  const saveSessionMutation = useMutation({
    mutationFn: addSessionCall,
    async onSuccess(sessionResult) {
      successToast('player-session.new.notifications.created');
      onSuccess?.();

      await Promise.all([
        ...sessionResult.playerSessions.map((x) =>
          queryClient.invalidateQueries({
            queryKey: [QUERY_KEYS.player, x.playerId, QUERY_KEYS.sessions],
          })
        ),
        queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.game, sessionResult.gameId] }),
        queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] }),
        queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.shames] }),
      ]);
    },
    onError: () => {
      errorToast('player-session.new.notifications.create-failed');
    },
  });

  return {
    game,
    isLoading: gameQuery.isLoading,
    isPending: saveSessionMutation.isPending,
    saveSession: saveSessionMutation.mutateAsync,
  };
};
