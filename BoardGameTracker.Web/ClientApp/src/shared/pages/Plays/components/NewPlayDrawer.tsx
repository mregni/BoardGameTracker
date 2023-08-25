import {Form} from 'antd';
import dayjs from 'dayjs';
import React, {useContext} from 'react';
import {useTranslation} from 'react-i18next';

import {GcDrawer} from '../../../components/GcDrawer';
import {Game, Play} from '../../../models';
import {GameDetailContext} from '../../GameDetail/context/GameDetailState';
import {GamesContextProvider} from '../../Games/context';
import {PlayerContextProvider} from '../../Players/context';
import {FormPlay, PlayForm} from './PlayForm';

interface Props {
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  open: boolean;
  game?: Game;
}

const NewPlayDrawerContainer = (props: Props) => {
  const { setOpen, open, game = null } = props;
  const { addPlay } = useContext(GameDetailContext);
  const { t } = useTranslation();
  const [form] = Form.useForm();

  const onClose = () => {
    form.resetFields();
    setOpen(false);
  };

  const finish = async (play: Play): Promise<void> => {
    await addPlay(play);
    onClose();
  }

  const initialValues: FormPlay = {
    gameId: game?.id ?? 0,
    id: 0,
    ended: true,
    comment: '',
    players: [],
    minutes: 0,
    start: dayjs()
  }

  return (
    <GcDrawer
      title={t('play.new.title')}
      open={open}
      onClose={onClose}>
      <PlayForm form={form} submitAction={finish} initialValues={initialValues} />
    </GcDrawer>
  )
}

export const NewPlayDrawer = (props: Props) => {
  return (
    <GamesContextProvider>
      <PlayerContextProvider>
        <NewPlayDrawerContainer {...props} />
      </PlayerContextProvider>
    </GamesContextProvider>
  )
}