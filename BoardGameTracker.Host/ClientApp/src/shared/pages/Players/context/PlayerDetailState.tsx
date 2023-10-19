import {App} from 'antd';
import {RcFile} from 'antd/es/upload';
import {createContext, useCallback, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {usePlays} from '../../../hooks';
import {
  CreationResult, CreationResultType, Play, Player, PlayerStatistics, SearchResultType,
} from '../../../models';
import {uploadImage} from '../../../services/ImageService';
import {
  deletePlayer as deletePlayerCall, getPlayer, getPlayerStatistics,
  updatePlayer as updatePlayerCall,
} from '../../../services/PlayerService';
import {
  createErrorNotification, createInfoNotification, createSuccessNotification,
} from '../../../utils';
import {FormPlayer} from '../components/PlayerForm';

export interface PlayerDetailContextProps {
  loading: boolean;
  player: Player | null;
  statistics: PlayerStatistics | null;
  plays: Play[];
  loadPlayer: (id: number) => Promise<void>;
  deletePlayer: (id: number, name: string) => Promise<void>;
  updatePlayer: (player: FormPlayer) => Promise<CreationResultType>;
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

  const loadPlayer = useCallback(async (id: number): Promise<void> => {
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

  const deletePlayer = async (id: number, name: string): Promise<void> => {
    await deletePlayerCall(id)
    createInfoNotification(
      notification,
      t('player.deleted.title'),
      t('player.deleted.description', { name: name })
    );
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

  const updatePlayer = async (editPlayer: FormPlayer): Promise<CreationResultType> => {
    setLoading(true);

    if (player === null) {
      return CreationResultType.Failed;
    }

    const tempPlayer: Player = {
      ...editPlayer,
      image: player.image
    };

    console.log(editPlayer);

    let image = null;
    if (editPlayer.fileList.length > 0) {
      image = editPlayer.fileList[0]?.originFileObj === undefined
        ? null : editPlayer.fileList[0].originFileObj;
    }

    const imageResult = await uploadImage(image, 0);
    if (imageResult.type === CreationResultType.Failed || imageResult.data === null) {
      createErrorNotification(
        notification,
        t('player.update.failed.message'),
        t('player.update.failed.description', { name: player.name })
      )
      return Promise.reject("Failed to upload image");
    }

    tempPlayer.image = imageResult.data;
    return await updatePlayerCall(tempPlayer)
      .then((result: CreationResult<Player>) => {
        if (result.type === CreationResultType.Failed) {
          createErrorNotification(
            notification,
            t('player.update.failed.message'),
            t('player.update.failed.description', { name: player.name })
          )
        } else if (result.type === CreationResultType.Success) {
          loadPlayer(player.id);
          createSuccessNotification(
            notification,
            t('player.update.success.message'),
            t('player.update.success.description', { name: player.name })
          )
        }

        return result.type;
      }).catch(() => {
        createErrorNotification(
          notification,
          t('player.update.failed.message'),
          t('player.update.failed.description', { name: player.name })
        )
        return CreationResultType.Failed;
      }).finally(() => {
        setLoading(false);
      });

    
    
  }

  const updatePlayerPlay = async (play: Play): Promise<void> => {
    await updatePlay(play);
    player != null && await refreshData(player.id);
  }


  return {
    loading, player, loadPlayer, deletePlayer, statistics, plays,
    addPlayerPlay, deletePlayerPlay, updatePlayerPlay, updatePlayer
  };
};
