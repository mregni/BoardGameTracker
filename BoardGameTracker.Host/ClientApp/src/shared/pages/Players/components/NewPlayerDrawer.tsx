import {Form} from 'antd';
import React, {useContext} from 'react';
import {useTranslation} from 'react-i18next';

import {GcDrawer} from '../../../components/GcDrawer';
import {CreationResultType, Player} from '../../../models';
import {PlayerContext} from '../context';
import {FormPlayer, PlayerForm} from './PlayerForm';

interface Props {
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  open: boolean;
}

export const NewPlayerDrawer = (props: Props) => {
  const { createPlayer, loadPlayers, addingPlayer } = useContext(PlayerContext);
  const { setOpen, open } = props;
  const { t } = useTranslation();
  const [form] = Form.useForm();

  const onClose = () => {
    form.resetFields();
    setOpen(false);
  };

  const finish = async (player: FormPlayer): Promise<void> => {
    const result = await createPlayer(player);
    if (result === CreationResultType.Success) {
      loadPlayers();
      onClose();
    }
  }

  const initialValues: FormPlayer = {
    id: 0,
    name: '',
    fileList: []
  }

  return (
    <GcDrawer
      title={t('player.new.title')}
      open={open}
      onClose={onClose}
    >
      <PlayerForm
        form={form}
        submitAction={finish}
        initialValues={initialValues}
        loading={addingPlayer}
      />
    </GcDrawer>
  )
}
