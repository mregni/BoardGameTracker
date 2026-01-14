import { getSessionCall } from '../sessionService';

import { createEntityQueryWithStringId } from './queryFactory';

import { QUERY_KEYS } from '@/models';

export const getSession = createEntityQueryWithStringId(QUERY_KEYS.sessions, getSessionCall);
