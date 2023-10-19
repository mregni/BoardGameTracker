import {App} from 'antd';
import {createContext, Dispatch, SetStateAction, useCallback, useEffect, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {CreationResult, CreationResultType, Player, PlayerCreation} from '../../../models';
import {uploadImage} from '../../../services/ImageService';
import {
  addPlayer, deletePlayer as deletePlayerCall, getAllPlayer, updatePlayer as updatePlayerCall,
} from '../../../services/PlayerService';
import {
  createErrorNotification, createInfoNotification, createSuccessNotification,
} from '../../../utils';
import {FormPlayer} from '../components/PlayerForm';

export interface PlayerContextProps {
  loading: boolean;
  loadPlayers: () => Promise<void>;
  players: Player[];
  addingPlayer: boolean;
  createPlayer: (newPlayer: FormPlayer) => Promise<CreationResultType>;
}

export const PlayerContext = createContext<PlayerContextProps>(null!);

export const usePlayerContext = (): PlayerContextProps => {
  const [loading, setLoading] = useState(false);
  const [players, setPlayers] = useState<Player[]>([]);
  const [addingPlayer, setAddingPlayer] = useState(false);
  const { notification } = App.useApp();
  const { t } = useTranslation();

  const loadPlayers = useCallback(async (): Promise<void> => {
    setLoading(true);
    const result = await getAllPlayer();
    setPlayers(result.list);
    setLoading(false);
  }, []);

  useEffect(() => {
    loadPlayers();
  }, [loadPlayers]);

  const createPlayer = async (newPlayer: FormPlayer): Promise<CreationResultType> => {
    setAddingPlayer(true);

    let image = null;
    if (newPlayer.fileList.length > 0) {
      image = newPlayer.fileList[0]?.originFileObj === undefined
        ? null : newPlayer.fileList[0].originFileObj;
    }

    const imageResult = await uploadImage(image, 0);
    if (imageResult.type === CreationResultType.Failed || imageResult.data === null) {
      createErrorNotification(
        notification,
        t('player.new.failed.message'),
        t('player.new.failed.description', { name: newPlayer.name })
      )
      return Promise.reject("Failed to upload image");
    }

    const player: Player = {
      id: 0,
      name: newPlayer.name,
      image: imageResult.data
    };

    return await addPlayer(player)
      .then((result: CreationResult<Player>) => {
        if (result.type === CreationResultType.Failed) {
          createErrorNotification(
            notification,
            t('player.new.failed.message'),
            t('player.new.failed.description', { name: player.name })
          )
        } else if (result.type === CreationResultType.Success) {
          loadPlayers();
          createSuccessNotification(
            notification,
            t('player.new.success.message'),
            t('player.new.success.description', { name: player.name })
          )
        }

        return result.type;
      }).catch(() => {
        createErrorNotification(
          notification,
          t('player.new.failed.message'),
          t('player.new.failed.description', { name: player.name })
        )
        return CreationResultType.Failed;
      }).finally(() => {
        setAddingPlayer(false);
      });
  }

  return {
    loading, players, loadPlayers, createPlayer,
    addingPlayer
  };
};
