import {Form} from 'antd';
import dayjs from 'dayjs';
import {useContext} from 'react';
import {useTranslation} from 'react-i18next';

import {GcDrawer} from '../../../components/GcDrawer';
import {Play} from '../../../models';
import {GameDetailContext} from '../../GameDetail/context/GameDetailState';
import {PlayForm} from './PlayForm';

interface Props {
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  open: boolean;
  play: Play;
  edit: (play: Play) => Promise<void>;
}

export const EditPlayDrawer = (props: Props) => {
  const { setOpen, open, play, edit } = props;

  const { t } = useTranslation();
  const [form] = Form.useForm();

  const onClose = () => {
    form.resetFields();
    setOpen(false);
  };

  const finish = async (play: Play): Promise<void> => {
    await edit(play);
    onClose();
  }

  return (
    <GcDrawer
      title={t('play.edit.title')}
      open={open}
      onClose={onClose}>
      <PlayForm form={form} submitAction={finish} initialValues={{...play, start: dayjs(play.start)}} />
    </GcDrawer>
  )
}
