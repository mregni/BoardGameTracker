import {App} from 'antd';
import {createContext, useCallback, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {usePlays} from '../../../hooks';
import {Game, GameStatistics, Play, SearchResultType} from '../../../models';
import {deleteGameCall, getGame, getGameStatistics} from '../../../services/GameService';
import {createInfoNotification} from '../../../utils';

export interface GameDetailContextProps {
  loading: boolean;
  game: Game | null;
  plays: Play[];
  statistics: GameStatistics | null;
  loadGame: (id: string) => Promise<void>;
  deleteGame: () => Promise<void>;
  deleteGamePlay: (id: number) => Promise<void>;
  addGamePlay(play: Play): Promise<void>;
  updateGamePlay: (play: Play) => Promise<void>;
}

export const GameDetailContext = createContext<GameDetailContextProps>(null!);

export const useGameDetailContext = (): GameDetailContextProps => {
  const [loading, setLoading] = useState(false);
  const [game, setGame] = useState<Game | null>(null);
  const [plays, setPlays] = useState<Play[]>([]);
  const [statistics, setStatistics] = useState<GameStatistics | null>(null)
  const { notification } = App.useApp();
  const {deletePlay, addPlay, updatePlay, getPlays } = usePlays();
  const { t } = useTranslation();

  const loadPlays = async (id: number): Promise<void> => {
    const playsResult = await await getPlays(id, 'game');
    if (playsResult.result === SearchResultType.Found) {
      setPlays(playsResult.model !== null ? playsResult.model : []);
    }
  }

  const refreshData = async (id: number) => {
    await loadPlays(id);
    const statistics = await getGameStatistics(id);
    statistics && setStatistics(statistics?.model);
  }

  const loadGame = useCallback(async (id: string): Promise<void> => {
    setLoading(true);
    const result = await getGame(id);

    if (result.result === SearchResultType.Found && result.model !== null) {
      setGame(result.model);
      await refreshData(result.model.id);
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
    if (game !== null) {
      await deleteGameCall(game.id);
      createInfoNotification(
        notification,
        t('game.deleted.title'),
        t('game.deleted.description', { title: game.title })
      );
    }
  }

  const addGamePlay = async (play: Play): Promise<void> => {
    await addPlay(play);
    game != null && await refreshData(game.id);
  }

  const deleteGamePlay = async (id: number): Promise<void> => {
    await deletePlay(id);
    game != null && await refreshData(game.id);
  }

  const updateGamePlay = async (play: Play): Promise<void> => {
    await updatePlay(play);
    game != null && await refreshData(game.id);
  }

  return {
    loading, game, loadGame, deleteGame, plays,
    deleteGamePlay, addGamePlay, updateGamePlay, statistics
  };
};
