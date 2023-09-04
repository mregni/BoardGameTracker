import {Form} from 'antd';
import React from 'react';
import {useTranslation} from 'react-i18next';

import {GcDrawer} from '../../../components/GcDrawer';
import {FormLocation, Location} from '../../../models';
import {LocationForm} from './LocationForm';

interface Props {
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  open: boolean;
  location: Location;
  edit: (location: FormLocation) => Promise<void>;
}

export const EditLocationDrawer = (props: Props) => {
  const { setOpen, open, location, edit } = props;
  const [form] = Form.useForm();
  const { t } = useTranslation();

  const onClose = () => {
    form.resetFields();
    setOpen(false);
  };

  const finish = async (location: FormLocation): Promise<void> => {
    await edit(location);
    onClose();
  }
  
  return (
    <GcDrawer
      title={t('location.new.title')}
      open={open}
      onClose={onClose}
    >
      <LocationForm form={form} submitAction={finish} initialValues={{...location}} />
    </GcDrawer>
  )
}
