import {createContext, useCallback, useEffect, useState} from 'react';

import {Game} from '../../../models';
import {getAllGames} from '../../../services/GameService';

export interface GamesContextProps {
  loading: boolean;
  games: Game[];
  loadGames: () => Promise<void>;
}

export const GamesContext = createContext<GamesContextProps>(null!);

export const useGamesContext = (): GamesContextProps => {
  const [loading, setLoading] = useState(false);
  const [games, setGames] = useState<Game[]>([]);

  const loadGames = useCallback(async (): Promise<void> => {
    setLoading(true);
    const result = await getAllGames();
    setGames(result.list);
    setLoading(false);
  }, []);

  useEffect(() => {
    loadGames();
  }, [loadGames]);

  return {
    loading, games, loadGames
  };
};
