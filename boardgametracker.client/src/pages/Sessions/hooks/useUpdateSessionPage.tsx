import { AxiosError } from 'axios';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { FailResult, QUERY_KEYS, Session } from '@/models';
import { getSession, updateSession } from '@/hooks/services/sessionService';

interface Props {
  id: string | undefined;
  onSessionSaveSuccess: () => void;
}

export const useUpdateSessionPage = (props: Props) => {
  const { id, onSessionSaveSuccess } = props;
  const queryClient = useQueryClient();

  const { data: session } = useQuery<Session, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.gameSessions, id],
    queryFn: ({ signal }) => getSession(id!, signal),
    enabled: id !== undefined,
  });

  const saveSessionMutation = useMutation<Session, AxiosError<FailResult>, Session>({
    mutationFn: updateSession,
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
    session,
    updateSession: saveSessionMutation,
  };
};
