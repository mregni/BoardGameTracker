import { AxiosError } from 'axios';
import { useQuery } from '@tanstack/react-query';

import { FailResult, QUERY_KEYS, Session } from '@/models';
import { deleteSessionCall } from '@/hooks/services/sessionService';
import { getPlayerSessions } from '@/hooks/services/playerService';

interface Props {
  id: string | undefined;
  onDeleteSuccess?: () => void;
}

export const usePlayerSessionsPage = (props: Props) => {
  const { id, onDeleteSuccess } = props;

  const sessions = useQuery<Session[], AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.players, id, QUERY_KEYS.playerSessions],
    queryFn: ({ signal }) => getPlayerSessions(id!, signal),
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
