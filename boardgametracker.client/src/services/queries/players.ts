import { queryOptions } from '@tanstack/react-query';

import { getPlayerCall, getPlayersCall, getPlayerSessionsCall, getPlayerStatisticsCall } from '../playerService';

import { QUERY_KEYS } from '@/models';

export const getPlayers = () =>
  queryOptions({
    queryKey: [QUERY_KEYS.players],
    queryFn: () => getPlayersCall(),
  });

export const getPlayer = (id: string) =>
  queryOptions({
    queryKey: [QUERY_KEYS.player, id],
    queryFn: () => getPlayerCall(id),
  });

export const getPlayerStatistics = (id: string) =>
  queryOptions({
    queryKey: [QUERY_KEYS.player, id, QUERY_KEYS.statistics],
    queryFn: () => getPlayerStatisticsCall(id),
  });

export const getPlayerSessions = (id: string) =>
  queryOptions({
    queryKey: [QUERY_KEYS.player, id, QUERY_KEYS.sessions],
    queryFn: () => getPlayerSessionsCall(id),
  });
