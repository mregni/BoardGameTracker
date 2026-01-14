import { useQueries } from '@tanstack/react-query';

import { getPlayers } from '@/services/queries/players';
import { Player } from '@/models';

export const usePlayerById = () => {
  const [playersQuery] = useQueries({
    queries: [getPlayers()],
  });

  const players = playersQuery.data ?? [];

  const playerById = (id: number | undefined): Player | null => {
    if (id === undefined) return null;

    const index = players.findIndex((x) => x.id === id);
    if (index !== -1) {
      return players[index];
    }

    return null;
  };

  return {
    playerById,
  };
};
