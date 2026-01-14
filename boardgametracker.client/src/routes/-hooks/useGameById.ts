import { useQueries } from '@tanstack/react-query';

import { getGames } from '@/services/queries/games';
import { Game } from '@/models';

export const useGameById = () => {
  const [gameQuery] = useQueries({
    queries: [getGames()],
  });

  const games = gameQuery.data?.items ?? [];

  const gameById = (id: string | number | undefined): Game | null => {
    if (id === undefined) return null;

    const index = games.findIndex((x) => x.id === id);
    if (index !== -1) {
      return games[index];
    }

    return null;
  };

  return {
    gameById,
  };
};
