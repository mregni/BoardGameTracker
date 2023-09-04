import {Form} from 'antd';
import React, {useContext} from 'react';
import {useTranslation} from 'react-i18next';

import {GcDrawer} from '../../../components/GcDrawer';
import {FormLocation} from '../../../models';
import {LocationContext} from '../context/LocationState';
import {LocationForm} from './LocationForm';

interface Props {
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  open: boolean;
}

export const NewLocationDrawer = (props: Props) => {
  const { setOpen, open } = props;
  const { addLocation } = useContext(LocationContext);
  const [form] = Form.useForm();
  const { t } = useTranslation();

  const onClose = () => {
    form.resetFields();
    setOpen(false);
  };

  const saveForm = async (location: FormLocation) => {
    await addLocation(location);
    onClose();
  }

  const initialValues: FormLocation = {
    id: 0,
    name: ''
  }
  
  return (
    <GcDrawer
      title={t('location.new.title')}
      open={open}
      onClose={onClose}
    >
      <LocationForm form={form} submitAction={saveForm} initialValues={initialValues} />
    </GcDrawer>
  )
}
