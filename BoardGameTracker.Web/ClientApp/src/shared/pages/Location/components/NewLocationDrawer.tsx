import {Form} from 'antd';
import React, {useContext} from 'react';
import {useTranslation} from 'react-i18next';

import {GcDrawer} from '../../../components/GcDrawer';
import {FormLocation} from '../../../models';
import {LocationContext} from '../context/LocationState';
import {LocationForm} from './LocationForm';

interface Props {
  onClose: (id: number | null) => void;
  open: boolean;
}

export const NewLocationDrawer = (props: Props) => {
  const { onClose, open } = props;
  const { addLocation } = useContext(LocationContext);
  const [form] = Form.useForm();
  const { t } = useTranslation();

  const closeDrawer = () => {
    form.resetFields();
    onClose(null);
  };

  const saveForm = async (location: FormLocation) => {
    const result = await addLocation(location);
    console.log(result);
    form.resetFields();
    onClose(result?.id ?? null);
  }

  const initialValues: FormLocation = {
    id: 0,
    name: ''
  }
  
  return (
    <GcDrawer
      title={t('location.new.title')}
      open={open}
      onClose={closeDrawer}
    >
      <LocationForm form={form} submitAction={saveForm} initialValues={initialValues} />
    </GcDrawer>
  )
}
