import { QUERY_KEYS } from '@/models';
import { queryOptions } from '@tanstack/react-query';
import { getLoansCall } from '../loanService';

export const getLoans = () =>
  queryOptions({
    queryKey: [QUERY_KEYS.games],
    queryFn: getLoansCall,
  });
