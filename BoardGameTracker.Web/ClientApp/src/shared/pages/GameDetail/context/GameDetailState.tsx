import {App} from 'antd';
import {createContext, useCallback, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {CreationResultType, Game, GameStatistics, Play, SearchResultType} from '../../../models';
import {
  deleteGameCall, getGame, getGamePlays, getGameStatistics,
} from '../../../services/GameService';
import {
  AddPlay as AddPlayCall, deletePlay as deletePlayCall, UpdatePlay as UpdatePlayCall,
} from '../../../services/PlayService';
import {createErrorNotification, createInfoNotification} from '../../../utils';

export interface GameDetailContextProps {
  loading: boolean;
  game: Game | null;
  plays: Play[];
  statistics: GameStatistics | null;
  loadGame: (id: string) => Promise<void>;
  deleteGame: () => Promise<void>;
  deletePlay: (id: number) => Promise<void>;
  addPlay(play: Play): Promise<void>;
  updatePlay: (play: Play) => Promise<void>;
}

export const GameDetailContext = createContext<GameDetailContextProps>(null!);

export const useGameDetailContext = (): GameDetailContextProps => {
  const [loading, setLoading] = useState(false);
  const [game, setGame] = useState<Game | null>(null);
  const [plays, setPlays] = useState<Play[]>([]);
  const [statistics, setStatistics] = useState<GameStatistics | null>(null)
  const { notification } = App.useApp();
  const { t } = useTranslation();

  const loadPlays = async (id: number): Promise<void> => {
    const playsResult = await getGamePlays(id, 0, 10);
    if (playsResult.result === SearchResultType.Found) {
      setPlays(playsResult.model !== null ? playsResult.model : []);
    }
  }

  const refreshData = async (id: number) => {
    await loadPlays(id);
    const statistics = await getGameStatistics(id);
      setStatistics(statistics.model);
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

  const deletePlay = async (id: number): Promise<void> => {
    await deletePlayCall(id);
    game != null && await refreshData(game.id);
    createInfoNotification(
      notification,
      t('play.deleted.title'),
      t('play.deleted.description')
    );
  }

  const addPlay = async (play: Play): Promise<void> => {
    const result = await AddPlayCall(play);
    if (result.type === CreationResultType.Success) {
      createInfoNotification(
        notification,
        t('play.new.notification.title'),
        t('play.new.notification.description')
      );
    } else {
      createErrorNotification(
        notification,
        t('play.new.notification.title-failed'),
        t('play.new.notification.description-failed')
      );
    }

    game != null && await refreshData(game.id);
  }

  const updatePlay = async (play: Play): Promise<void> => {
    const result = await UpdatePlayCall(play);
    if (result.type === CreationResultType.Success) {
      createInfoNotification(
        notification,
        t('play.update.notification.title'),
        t('play.update.notification.description')
      );
    } else {
      createErrorNotification(
        notification,
        t('play.update.notification.title-failed'),
        t('play.update.notification.description-failed')
      );
    }

    game != null && await refreshData(game.id);
  }

  return {
    loading, game, loadGame, deleteGame, plays, 
    deletePlay, addPlay, updatePlay, statistics
  };
};
