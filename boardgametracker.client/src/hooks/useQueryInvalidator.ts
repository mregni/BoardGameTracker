import { useMemo } from 'react';
import { useQueryClient } from '@tanstack/react-query';

import { QueryInvalidator } from '@/services/queries/invalidations';

export const useQueryInvalidator = () => {
  const queryClient = useQueryClient();
  return useMemo(() => new QueryInvalidator(queryClient), [queryClient]);
};
