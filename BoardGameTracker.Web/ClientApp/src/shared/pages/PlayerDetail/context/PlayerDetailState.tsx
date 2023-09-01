import {App} from 'antd';
import {createContext, useCallback, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {usePlays} from '../../../hooks';
import {Play, Player, PlayerStatistics, SearchResultType} from '../../../models';
import {deletePlayerCall, getPlayer, getPlayerStatistics} from '../../../services/PlayerService';
import {createInfoNotification} from '../../../utils';

export interface PlayerDetailContextProps {
  loading: boolean;
  player: Player | null;
  statistics: PlayerStatistics | null;
  plays: Play[];
  loadPlayer: (id: string) => Promise<void>;
  deletePlayer: () => Promise<void>;
  deletePlayerPlay: (id: number) => Promise<void>;
  addPlayerPlay(play: Play): Promise<void>;
  updatePlayerPlay: (play: Play) => Promise<void>;
}

export const PlayerDetailContext = createContext<PlayerDetailContextProps>(null!);

export const usePlayerDetailContext = (): PlayerDetailContextProps => {
  const [loading, setLoading] = useState(false);
  const [player, setPlayer] = useState<Player | null>(null);
  const [statistics, setStatistics] = useState<PlayerStatistics | null>(null)
  const [plays, setPlays] = useState<Play[]>([]);
  const { deletePlay, addPlay, updatePlay, getPlays } = usePlays();
  const { notification } = App.useApp();
  const { t } = useTranslation();

  const loadPlayer = useCallback(async (id: string): Promise<void> => {
    setLoading(true);
    const result = await getPlayer(id);

    if (result.result === SearchResultType.Found && result.model !== null) {
      setPlayer(result.model);
      await refreshData(result.model.id);
    }

    if (result.result === SearchResultType.NotFound) {
      createInfoNotification(
        notification,
        t('player.notfound.title'),
        t('player.notfound.description', { id })
      );
    }

    setLoading(false);
  }, []);

  const deletePlayer = async (): Promise<void> => {
    if (player !== null) {
      await deletePlayerCall(player.id)
      createInfoNotification(
        notification,
        t('player.deleted.title'),
        t('player.deleted.description', { name: player.name })
      );
    }
  }

  const refreshData = async (id: number) => {
    await loadPlayerPlays(id);
    const statistics = await getPlayerStatistics(id);
    statistics && setStatistics(statistics?.model);
  }

  const loadPlayerPlays = async (id: number): Promise<void> => {
    const playsResult = await getPlays(id, 'player');
    if (playsResult.result === SearchResultType.Found) {
      setPlays(playsResult.model !== null ? playsResult.model : []);
    }
  }

  const addPlayerPlay = async (play: Play): Promise<void> => {
    await addPlay(play);
    player != null && await refreshData(player.id);
  }

  const deletePlayerPlay = async (id: number): Promise<void> => {
    await deletePlay(id);
    player != null && await refreshData(player.id);
  }

  const updatePlayerPlay = async (play: Play): Promise<void> => {
    await updatePlay(play);
    player != null && await refreshData(player.id);
  }


  return {
    loading, player, loadPlayer, deletePlayer, statistics, plays,
    addPlayerPlay, deletePlayerPlay, updatePlayerPlay
  };
};
