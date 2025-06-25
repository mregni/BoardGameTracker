import { queryOptions } from '@tanstack/react-query';

import {
  getGameCall,
  getGameExpansionsCall,
  getGamesCall,
  getGameSessionsCall,
  getGameStatisticsCall,
} from '../gameService';

import { QUERY_KEYS } from '@/models';

export const getGames = () =>
  queryOptions({
    queryKey: [QUERY_KEYS.games],
    queryFn: getGamesCall,
  });

export const getGame = (id: string) =>
  queryOptions({
    queryKey: [QUERY_KEYS.game, id],
    queryFn: () => getGameCall(id),
  });

export const getGameExpansions = (id: string) =>
  queryOptions({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.expansions],
    queryFn: () => getGameExpansionsCall(id),
  });

export const getGameStatistics = (id: string) =>
  queryOptions({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.statistics],
    queryFn: () => getGameStatisticsCall(id),
  });

export const getGameSessions = (id: string) =>
  queryOptions({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.sessions],
    queryFn: () => getGameSessionsCall(id),
  });
