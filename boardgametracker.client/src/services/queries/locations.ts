import { getLocationsCall } from '../locationService';

import { createListQuery } from './queryFactory';

import { QUERY_KEYS } from '@/models';

export const getLocations = createListQuery(QUERY_KEYS.locations, getLocationsCall);
