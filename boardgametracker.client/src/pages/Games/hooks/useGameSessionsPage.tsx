import { AxiosError } from 'axios';
import { useQuery } from '@tanstack/react-query';

import { FailResult, QUERY_KEYS, Session } from '@/models';
import { deleteSessionCall } from '@/hooks/services/sessionService';
import { getGameSessions } from '@/hooks/services/gameService';

interface Props {
  id: string | undefined;
  onDeleteSuccess?: () => void;
}

export const useGameSessionsPage = (props: Props) => {
  const { id, onDeleteSuccess } = props;

  const sessions = useQuery<Session[], AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gameSessions],
    queryFn: ({ signal }) => getGameSessions(id!, signal),
    enabled: id !== undefined,
  });

  const deleteSession = (id: string) => {
    void deleteSessionCall(id)
      .then(() => {
        sessions.refetch();
        onDeleteSuccess?.();
      })
      .finally(() => {});
  };

  return {
    sessions,
    deleteSession,
  };
};
