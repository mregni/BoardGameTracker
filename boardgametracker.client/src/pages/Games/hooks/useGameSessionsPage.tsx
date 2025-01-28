import { AxiosError } from 'axios';
import { useQuery } from '@tanstack/react-query';

import { FailResult, QUERY_KEYS, Session } from '@/models';
import { getGameSessions } from '@/hooks/services/gameService';

interface Props {
  id: string | undefined;
}

export const useGameSessionsPage = (props: Props) => {
  const { id } = props;

  const sessions = useQuery<Session[], AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gameSessions],
    queryFn: ({ signal }) => getGameSessions(id!, signal),
    enabled: id !== undefined,
  });

  return {
    sessions,
  };
};
