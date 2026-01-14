import { getLoansCall } from '../loanService';

import { createListQuery } from './queryFactory';

import { QUERY_KEYS } from '@/models';

export const getLoans = createListQuery(QUERY_KEYS.loans, getLoansCall);
