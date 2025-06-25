import { useMutation, useQueryClient } from '@tanstack/react-query';

import { addSessionCall } from '@/services/sessionService';
import { QUERY_KEYS } from '@/models';

interface Props {
  onSaveSuccess?: () => void;
  onSaveError?: () => void;
}

export const useNewSessionData = ({ onSaveSuccess, onSaveError }: Props) => {
  const queryClient = useQueryClient();

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

      await Promise.all(maps);
    },
    onError: () => {
      onSaveError?.();
    },
  });

  return {
    isPending: saveSessionMutation.isPending,
    saveSession: saveSessionMutation.mutateAsync,
  };
};
