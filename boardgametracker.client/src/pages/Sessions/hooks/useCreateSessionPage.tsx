import { AxiosError } from 'axios';
import { useMutation, useQueryClient } from '@tanstack/react-query';

import { CreateSession } from '@/models/Session/CreateSession';
import { FailResult, QUERY_KEYS, Session } from '@/models';
import { addSession } from '@/hooks/services/sessionService';

interface Props {
  onSessionSaveSuccess: () => void;
}

export const useCreateSessionPage = ({ onSessionSaveSuccess }: Props) => {
  const queryClient = useQueryClient();

  const saveSession = useMutation<Session, AxiosError<FailResult>, CreateSession>({
    mutationFn: addSession,
    async onSuccess(sessionResult) {
      onSessionSaveSuccess();

      const maps = sessionResult.playerSessions.map(async (x) => {
        return await queryClient.invalidateQueries({
          queryKey: [QUERY_KEYS.players, x.playerId, QUERY_KEYS.playerSessions],
        });
      });

      await queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.game, sessionResult.gameId],
      });

      await Promise.all(maps);
    },
  });

  return {
    saveSession,
  };
};
