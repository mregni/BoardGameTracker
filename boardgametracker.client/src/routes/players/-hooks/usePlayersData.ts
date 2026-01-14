import { useQuery } from '@tanstack/react-query';

import { getPlayers } from '@/services/queries/players';

export const usePlayersData = () => {
  const { data, isLoading } = useQuery(getPlayers());

  return {
    players: data ?? [],
    isLoading,
  };
};
