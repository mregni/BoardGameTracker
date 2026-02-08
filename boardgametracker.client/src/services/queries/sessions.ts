import { getSessionCall } from '../sessionService';

import { createEntityQuery } from './queryFactory';

import { QUERY_KEYS } from '@/models';

export const getSession = createEntityQuery(QUERY_KEYS.sessions, getSessionCall);
