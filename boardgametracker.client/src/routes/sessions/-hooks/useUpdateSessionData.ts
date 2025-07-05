import { useMutation, useQueryClient, useSuspenseQuery } from '@tanstack/react-query';

import { updateSessionCall } from '@/services/sessionService';
import { getSession } from '@/services/queries/sessions';
import { getGame } from '@/services/queries/games';
import { QUERY_KEYS } from '@/models';

interface Props {
  sessionId: string;
  onSaveSuccess?: () => void;
  onSaveError?: () => void;
}

export const useUpdateSessionData = ({ sessionId, onSaveSuccess, onSaveError }: Props) => {
  const queryClient = useQueryClient();

  const { data: session } = useSuspenseQuery(getSession(sessionId));
  const { data: game } = useSuspenseQuery(getGame(session.gameId));

  const updateSessionMutation = useMutation({
    mutationFn: updateSessionCall,
    async onSuccess(sessionResult) {
      onSaveSuccess?.();
      sessionResult.playerSessions.map((x) => {
        queryClient.invalidateQueries({
          queryKey: [QUERY_KEYS.player, x.playerId, QUERY_KEYS.sessions],
        });
      });

      queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.game, sessionResult.gameId] });
      queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.sessions, sessionId] });
    },
    onError: () => {
      onSaveError?.();
    },
  });

  return {
    session,
    game,
    isPending: updateSessionMutation.isPending,
    updateSession: updateSessionMutation.mutateAsync,
  };
};
