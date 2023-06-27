import {createContext, useState} from 'react';

import {Game} from '../../../models';

export interface GamesContextProps {
  
}

export const GamesContext = createContext<GamesContextProps>(null!);

export const useGamesContext = (): GamesContextProps => {
  const [loading, setLoading] = useState(false);
  
  return {
    loading
  };
};
