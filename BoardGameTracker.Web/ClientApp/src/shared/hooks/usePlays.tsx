import {App} from 'antd';
import {useTranslation} from 'react-i18next';

import {CreationResultType, Play, SearchResult} from '../models';
import {
  AddPlay as AddPlayCall, deletePlay as deletePlayCall, getPlays as getPlaysCall,
  UpdatePlay as UpdatePlayCall,
} from '../services/PlayService';
import {createErrorNotification, createInfoNotification} from '../utils';

interface Props {
  getPlays: (id: number, type: 'game' | 'player') => Promise<SearchResult<Play[]>>;
  deletePlay: (id: number) => Promise<void>;
  addPlay: (play: Play) => Promise<void>;
  updatePlay: (play: Play) => Promise<void>;
}

export const usePlays = (): Props => {
  const { notification } = App.useApp();
  const { t } = useTranslation();

  const getPlays = (id: number, type: 'game' | 'player'): Promise<SearchResult<Play[]>> => {
    return getPlaysCall(id, type);
  }

  const deletePlay = async (id: number): Promise<void> => {
    await deletePlayCall(id);
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
  }

  return { deletePlay, addPlay, updatePlay, getPlays }
}