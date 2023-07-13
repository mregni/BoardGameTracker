import {App} from 'antd';
import {createContext, useCallback, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {Game, SearchResultType} from '../../../models';
import {deleteGameCall, getGame} from '../../../services/GameService';
import {createInfoNotification} from '../../../utils';

export interface GameDetailContextProps {
  loading: boolean;
  game: Game | null;
  loadGame: (id: string) => Promise<void>;
  deleteGame: () => Promise<void>;
}

export const GameDetailContext = createContext<GameDetailContextProps>(null!);

export const useGameDetailContext = (): GameDetailContextProps => {
  const [loading, setLoading] = useState(false);
  const [game, setGame] = useState<Game | null>(null);
  const { notification } = App.useApp();
  const { t } = useTranslation();

  const loadGame = useCallback(async (id: string): Promise<void> => {
    setLoading(true);
    const result = await getGame(id);

    if (result.result === SearchResultType.Found) {
      setGame(result.model);
    }

    if (result.result === SearchResultType.NotFound) {
      createInfoNotification(
        notification,
        t('game.notfound.title'),
        t('game.notfound.description', { id })
      );
    }

    setLoading(false);
  }, []);

  const deleteGame = async (): Promise<void> => {
    if(game !== null) {
      await deleteGameCall(game.id);
      createInfoNotification(
        notification,
        t('game.deleted.title'),
        t('game.deleted.description', {title: game.title})
      );
    }
  }

  return {
    loading, game, loadGame, deleteGame
  };
};
