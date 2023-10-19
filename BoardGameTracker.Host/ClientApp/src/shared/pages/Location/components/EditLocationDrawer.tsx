import {Button, Form} from 'antd';
import React, {useContext} from 'react';
import {Trans, useTranslation} from 'react-i18next';

import {DeleteOutlined} from '@ant-design/icons';

import {GcDrawer} from '../../../components/GcDrawer';
import {FormLocation, Location} from '../../../models';
import {useModals} from '../../../utils';
import {LocationContext} from '../context/LocationState';
import {LocationForm} from './LocationForm';

interface Props {
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  open: boolean;
  location: Location;
  edit: (location: FormLocation) => Promise<void>;
}

export const EditLocationDrawer = (props: Props) => {
  const { setOpen, open, location, edit } = props;
  const { deleteLocation } = useContext(LocationContext);
  const [form] = Form.useForm();
  const { t } = useTranslation();
  const { deleteModal } = useModals();

  const onClose = () => {
    form.resetFields();
    setOpen(false);
  };

  const finish = async (location: FormLocation): Promise<void> => {
    await edit(location);
    onClose();
  }

  const localDeleteLocation = async () => {
    await deleteLocation(location.id);
    onClose();
  }

  const showDeleteModal = () => {
    deleteModal(
      t('game.delete.title', { title: location.name }),
      <Trans
        i18nKey="location.delete.description"
        components={{ strong: <strong />, newline: <br /> }} />,
      localDeleteLocation
    );
  }

  return (
    <GcDrawer
      title={t('location.new.title')}
      open={open}
      onClose={onClose}
      extraButtons={<Button icon={<DeleteOutlined />} danger onClick={() => showDeleteModal()}>
        {t('common.delete')}
      </Button>}
    >
      <LocationForm form={form} submitAction={finish} initialValues={{ ...location }} />
    </GcDrawer>
  )
}
