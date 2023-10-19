import {Button, Form} from 'antd';
import {Console} from 'console';
import React, {useContext} from 'react';
import {Trans, useTranslation} from 'react-i18next';

import {DeleteOutlined} from '@ant-design/icons';

import {GcDrawer} from '../../../components/GcDrawer';
import {CreationResultType, Player} from '../../../models';
import {useModals} from '../../../utils';
import {PlayerContext, PlayerDetailContext} from '../context';
import {FormPlayer, PlayerForm} from './PlayerForm';

interface Props {
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  open: boolean;
  player: Player;
}

export const EditPlayerDrawer = (props: Props) => {
  const { setOpen, open, player } = props;
  const { updatePlayer, loading, deletePlayer } = useContext(PlayerDetailContext);
  const { loadPlayers } = useContext(PlayerContext);
  const [form] = Form.useForm();
  const { t } = useTranslation();
  const { deleteModal } = useModals();

  const onClose = async () => {
    form.resetFields();
    await loadPlayers();
    setOpen(false);
  };

  const finish = async (values: FormPlayer): Promise<void> => {
    const result = await updatePlayer({ ...values, id: player.id });
    if (result === CreationResultType.Success) {
      await onClose();
    }
  }

  const localDeletePlayer = async () => {
    await deletePlayer(player.id, player.name);
    await onClose();
  }

  const showDeleteModal = () => {
    deleteModal(
      t('player.delete.title', { title: player.name }),
      <Trans
        i18nKey="player.delete.description"
        components={{ strong: <strong />, newline: <br /> }} />,
      localDeletePlayer
    );
  }

  return (
    <GcDrawer
      title={t('player.edit.title')}
      open={open}
      onClose={onClose}
      extraButtons={<Button icon={<DeleteOutlined />} danger onClick={() => showDeleteModal()}>
        {t('common.delete')}
      </Button>}
    >
      <PlayerForm
        form={form}
        submitAction={finish}
        initialValues={{
          ...player,
          fileList: [{
            uid: '-1',
            name: player.image ?? '',
            status: 'done',
            url: `/${player.image}`
          }]
        }}
        loading={loading}
      />
    </GcDrawer>
  )
}
