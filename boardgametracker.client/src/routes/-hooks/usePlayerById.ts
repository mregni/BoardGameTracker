import { useMemo } from 'react';
import { useQueries } from '@tanstack/react-query';

import { getPlayers } from '@/services/queries/players';
import { Player } from '@/models';

export const usePlayerById = () => {
  const [playersQuery] = useQueries({
    queries: [getPlayers()],
  });

  const players = useMemo(() => playersQuery.data ?? [], [playersQuery.data]);

  const playerById = (id: string | number | undefined): Player | null => {
    if (id === undefined) return null;

    const index = players.findIndex((x) => x.id === id.toString());
    if (index !== -1) {
      return players[index];
    }

    return null;
  };

  return {
    playerById,
  };
};
