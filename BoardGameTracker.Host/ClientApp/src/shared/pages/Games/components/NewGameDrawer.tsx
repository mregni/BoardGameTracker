import {Form} from 'antd';
import React from 'react';
import {useTranslation} from 'react-i18next';

import {GcDrawer} from '../../../components/GcDrawer';
import {Game} from '../../../models';
import {GameForm} from './GameForm';

interface Props {
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  open: boolean;
}

export const NewGameDrawer = (props: Props) => {
  const { setOpen, open } = props;
  const { t } = useTranslation();
  const [form] = Form.useForm();

  const onClose = () => {
    form.resetFields();
    setOpen(false);
  };

  const finish = async (game: Game): Promise<void> => {
    // const result = await createPlayer(player);
    // if (result === CreationResultType.Success) {
    //   loadPlayers();
    //   onClose();
    // }
    console.log(game);
  }

  return (
    <GcDrawer
      title={t('games.new.title')}
      open={open}
      onClose={onClose}
    >
      <GameForm
        form={form}
        submitAction={finish}
        initialValues={undefined}
        loading={false}
      />
    </GcDrawer>
  )
}
