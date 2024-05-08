import { AxiosError } from 'axios';

import { keepPreviousData, QueryFunction, QueryKey, useQuery } from '@tanstack/react-query';

import { FailResult, ListResult, Play, QUERY_KEYS } from '../models';
import { getGamePlays } from './services/gameService';
import { getPlays } from './services/playerService';

interface ReturnProps {
  plays: Play[];
  totalCount: number;
  isFetching: boolean;
}

export const usePlayerPlays = (playerId: string | undefined, page: number, pageCount: number): ReturnProps => {
  return usePlays({
    enabled: playerId !== undefined,
    queryFn: ({ signal }) => getPlays(playerId!, page, pageCount, signal),
    queryKey: [QUERY_KEYS.player, playerId, QUERY_KEYS.playerPlays, page],
  });
};

export const useGamePlays = (gameId: string | undefined, page: number, pageCount: number): ReturnProps => {
  return usePlays({
    enabled: gameId !== undefined,
    queryFn: ({ signal }) => getGamePlays(gameId!, page, pageCount, signal),
    queryKey: [QUERY_KEYS.game, gameId, QUERY_KEYS.gamePlays, page],
  });
};

interface Props<T> {
  queryKey: QueryKey;
  queryFn: QueryFunction<ListResult<T>, QueryKey, never>;
  enabled: boolean;
}

const usePlays = <T,>(props: Props<T>) => {
  const { queryKey, queryFn, enabled } = props;

  const { data, isFetching } = useQuery<ListResult<T>, AxiosError<FailResult>>({
    queryKey: queryKey,
    queryFn: queryFn,
    enabled: enabled,
    placeholderData: keepPreviousData,
  });

  return {
    plays: data !== undefined ? data?.list : ([] as Play[]),
    totalCount: data !== undefined ? data?.count : 0,
    isFetching,
  };
};
