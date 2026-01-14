import { queryOptions } from '@tanstack/react-query';

import {
  getBggCollectionCall,
  getGameCall,
  getGameExpansionsCall,
  getGamesCall,
  getGameSessionsCall,
  getGameStatisticsCall,
} from '../gameService';

import { createEntityQuery, createListQuery, createNestedQuery, createNestedQueryWithKeys } from './queryFactory';

import { QUERY_KEYS } from '@/models';

export const getGames = createListQuery(QUERY_KEYS.games, getGamesCall);

export const getGame = createEntityQuery(QUERY_KEYS.game, getGameCall);

export const getGameExpansions = createNestedQuery(QUERY_KEYS.game, QUERY_KEYS.expansions, getGameExpansionsCall);

export const getGameStatistics = createNestedQuery(QUERY_KEYS.game, QUERY_KEYS.statistics, getGameStatisticsCall);

export const getGameSessions = createNestedQuery(QUERY_KEYS.game, QUERY_KEYS.sessions, getGameSessionsCall);

export const getGameSessionsShortList = createNestedQueryWithKeys(
  QUERY_KEYS.game,
  QUERY_KEYS.sessions,
  [QUERY_KEYS.shortlist],
  getGameSessionsCall
);

export const getBggCollection = (username: string) =>
  queryOptions({
    queryKey: [QUERY_KEYS.game, QUERY_KEYS.bgg, username],
    queryFn: () => getBggCollectionCall(username),
    refetchInterval: (data) => {
      return data?.state.data?.statusCode === 200 ? false : 1000;
    },
    refetchIntervalInBackground: false,
  });
