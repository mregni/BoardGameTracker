import { useQuery } from '@tanstack/react-query';

import { Player, QUERY_KEYS } from '../models';

import { getPlayers } from './services/playerService';

export const usePlayerById = () => {
  const { data } = useQuery<Player[]>({
    queryKey: [QUERY_KEYS.players],
    queryFn: ({ signal }) => getPlayers(signal),
  });

  const playerById = (id: number | string | undefined): Player | null => {
    if (data === undefined || id === undefined) return null;

    const index = data.findIndex((x) => x.id === id.toString());
    if (index !== -1) {
      return data[index];
    }

    return null;
  };

  return {
    playerById,
  };
};
