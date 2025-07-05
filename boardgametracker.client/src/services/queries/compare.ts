import { queryOptions } from '@tanstack/react-query';

import { getCompareCall } from '../compareService';

import { QUERY_KEYS } from '@/models';

export const getCompare = (playerOne: number, playerTwo: number) =>
  queryOptions({
    queryKey: [QUERY_KEYS.compare, playerOne, playerTwo],
    queryFn: () => getCompareCall(playerOne, playerTwo),
  });
