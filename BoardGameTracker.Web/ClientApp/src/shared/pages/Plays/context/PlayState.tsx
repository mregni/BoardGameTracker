import {App} from 'antd';
import {createContext, useCallback, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {Game, Play, SearchResultType} from '../../../models';
import {deleteGameCall, getGame} from '../../../services/GameService';
import {AddPlay as AddPlayCall} from '../../../services/PlayService';
import {createInfoNotification} from '../../../utils';

export interface PlayContextProps {
  loading: boolean;
  addPlay(play: Play): void;
}

export const PlayContext = createContext<PlayContextProps>(null!);

export const usePlayContext = (): PlayContextProps => {
  const [loading, setLoading] = useState(false);

  const addPlay = (play: Play): void => {
    setLoading(true);
    AddPlayCall(play)
    .finally(() => {
      setLoading(false);
    });
  }

  return {
    loading, addPlay
  };
};
