import { AxiosError } from 'axios';
import {
  keepPreviousData,
  QueryFunction,
  QueryKey,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query';

import { useToast } from '../providers/BgtToastProvider';
import { CreateSession } from '../models/Session/CreateSession';
import { FailResult, ListResult, QUERY_KEYS, Result, Session } from '../models';

import { addSession, deleteSession as deleteSessionCall } from './services/sessionService';
import { getSessions as getPlayerSessions } from './services/playerService';
import { getSessions as getGameSessions } from './services/gameService';

interface ReturnProps {
  sessions: Session[];
  totalCount: number;
  isFetching: boolean;
  deletePlay: (id: number) => Promise<void>;
}

export const usePlayerSessions = (playerId: string | undefined, page: number, pageCount: number): ReturnProps => {
  return useSessions({
    enabled: playerId !== undefined,
    queryFn: ({ signal }) => getPlayerSessions(playerId!, page, pageCount, signal),
    queryKey: [QUERY_KEYS.player, playerId, QUERY_KEYS.playerSessions, page],
  });
};

export const useGameSessions = (gameId: string | undefined, page: number, pageCount: number): ReturnProps => {
  return useSessions({
    enabled: gameId !== undefined,
    queryFn: ({ signal }) => getGameSessions(gameId!, page, pageCount, signal),
    queryKey: [QUERY_KEYS.game, gameId, QUERY_KEYS.gameSessions, page],
  });
};

interface Props<T> {
  queryKey: QueryKey;
  queryFn: QueryFunction<ListResult<T>, QueryKey, never>;
  enabled: boolean;
}

const useSessions = <T,>(props: Props<T>) => {
  const { queryKey, queryFn, enabled } = props;
  const { showInfoToast, showErrorToast } = useToast();

  const { data, isFetching, refetch } = useQuery<ListResult<T>, AxiosError<FailResult>>({
    queryKey: queryKey,
    queryFn: queryFn,
    enabled: enabled,
    placeholderData: keepPreviousData,
  });

  const deletePlay = async (id: number) => {
    try {
      await deleteSessionCall(id);
      await refetch();
      showInfoToast('player-session.delete.successfull');
    } catch {
      showErrorToast('player-session.delete.failed');
    }
  };

  return {
    sessions: data !== undefined ? data?.list : ([] as Session[]),
    totalCount: data !== undefined ? data?.count : 0,
    isFetching,
    deletePlay,
  };
};
