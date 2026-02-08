import { getGameNightsCall, getGameNightStatisticsCall } from '../gameNightService';

import { createListQuery, createSingletonQuery } from './queryFactory';

import { QUERY_KEYS } from '@/models';

export const getGameNights = createListQuery(QUERY_KEYS.gameNights, getGameNightsCall);

export const getGameNightStatistics = createSingletonQuery(QUERY_KEYS.gameNightStatistics, getGameNightStatisticsCall);
