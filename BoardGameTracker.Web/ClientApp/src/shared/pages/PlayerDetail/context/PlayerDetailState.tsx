import {App} from 'antd';
import {createContext, useCallback, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {Player, SearchResultType} from '../../../models';
import {deletePlayerCall, getPlayer} from '../../../services/PlayerService';
import {createInfoNotification} from '../../../utils';

export interface PlayerDetailContextProps {
  loading: boolean;
  player: Player | null;
  loadPlayer: (id: string) => Promise<void>;
  deletePlayer: () => Promise<void>;
}

export const PlayerDetailContext = createContext<PlayerDetailContextProps>(null!);

export const usePlayerDetailContext = (): PlayerDetailContextProps => {
  const [loading, setLoading] = useState(false);
  const [player, setPlayer] = useState<Player | null>(null);
  const { notification } = App.useApp();
  const { t } = useTranslation();

  const loadPlayer = useCallback(async (id: string): Promise<void> => {
    setLoading(true);
    const result = await getPlayer(id);

    if (result.result === SearchResultType.Found) {
      setPlayer(result.model);
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
    if(player !== null) {
      await deletePlayerCall(player.id)
      createInfoNotification(
        notification,
        t('player.deleted.title'),
        t('player.deleted.description', {name: player.name})
      );
    }
  }

  return {
    loading, player, loadPlayer, deletePlayer
  };
};
