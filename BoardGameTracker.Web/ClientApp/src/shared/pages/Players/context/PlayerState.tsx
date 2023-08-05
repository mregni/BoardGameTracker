import {App} from 'antd';
import {createContext, useCallback, useEffect, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {CreationResult, CreationResultType, Player, PlayerCreation} from '../../../models';
import {uploadImage} from '../../../services/ImageService';
import {addPlayer, getAllPlayer} from '../../../services/PlayerService';
import {createErrorNotification, createSuccessNotification} from '../../../utils';

export interface PlayerContextProps {
  loading: boolean;
  players: Player[];
  loadPlayers: () => Promise<void>;
  addingPlayer: boolean;
  addNewPlayer: (newPlayer: PlayerCreation, profileImage: File | null) => Promise<CreationResultType>;
}

export const PlayerContext = createContext<PlayerContextProps>(null!);

export const usePlayerContext = (): PlayerContextProps => {
  const [loading, setLoading] = useState(false);
  const [players, setPlayer] = useState<Player[]>([]);
  const [addingPlayer, setAddingPlayer] = useState(false);
  const { notification } = App.useApp();
  const { t } = useTranslation();

  const loadPlayers = useCallback(async (): Promise<void> => {
    setLoading(true);
    const result = await getAllPlayer();
    setPlayer(result.list);
    setLoading(false);
  }, []);

  useEffect(() => {
    loadPlayers();
  }, [loadPlayers]);

  const addNewPlayer = async (newPlayer: PlayerCreation, profileImage: File | null): Promise<CreationResultType> => {
    setAddingPlayer(true);

    const imageResult = await uploadImage(profileImage, 0);
    if (imageResult.type === CreationResultType.Failed || imageResult.data === null) {
      createErrorNotification(
        notification,
        t('player.new.failed.message'),
        t('player.new.failed.description', { name })
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
            t('player.new.failed.description', { name })
          )
        } else if (result.type === CreationResultType.Success) {
          createSuccessNotification(
            notification,
            t('player.new.success.message'),
            t('player.new.success.description', { name })
          )
        }

        return result.type;
      }).catch(() => {
        createErrorNotification(
          notification,
          t('player.new.failed.message'),
          t('player.new.failed.description', { name })
        )
        return CreationResultType.Failed;
      }).finally(() => {
        setAddingPlayer(false);
      })
  }

  return {
    loading, players, loadPlayers, addNewPlayer, addingPlayer
  };
};
