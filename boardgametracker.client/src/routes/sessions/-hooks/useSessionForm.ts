import { useQueries } from '@tanstack/react-query';

import { getSettings } from '@/services/queries/settings';
import { getPlayers } from '@/services/queries/players';
import { getLocations } from '@/services/queries/locations';
import { getGames } from '@/services/queries/games';

export const useSessionForm = () => {
  const [settingsQuery, locationQuery, gamesQuery, playersQuery] = useQueries({
    queries: [getSettings(), getLocations(), getGames(), getPlayers()],
  });

  const settings = settingsQuery.data;
  const locations = locationQuery.data ?? [];
  const games = gamesQuery.data ?? [];
  const players = playersQuery.data ?? [];

  const isLoading = settingsQuery.isLoading || locationQuery.isLoading || gamesQuery.isLoading;

  return {
    isLoading,
    settings,
    locations,
    games,
    players,
  };
};
