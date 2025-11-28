import { useMemo } from 'react';
import { useQueries } from '@tanstack/react-query';

import { getLoans } from '@/services/queries/loans';

export const useNewLoan = () => {
  const [loansQuery] = useQueries({
    queries: [getLoans()],
  });

  const loans = useMemo(() => loansQuery.data ?? [], [loansQuery.data]);
  const isLoading = loansQuery.isLoading;

  return {
    isLoading,
    loans,
  };
};
