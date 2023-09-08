import {Button, MenuProps} from 'antd';
import React, {useContext, useState} from 'react';
import {useTranslation} from 'react-i18next';
import {Link} from 'react-router-dom';

import {PlusOutlined} from '@ant-design/icons';

import {
  GcMenuItem, GcPageContainer, GcPageContainerContent, GcPageContainerHeader,
} from '../../components/GcPageContainer';
import {LocationTable} from './components/LocationTable';
import {NewLocationDrawer} from './components/NewLocationDrawer';
import {LocationContext} from './context/LocationState';

export const Location = () => {
  const { locations, loading } = useContext(LocationContext);
  const { t } = useTranslation();
  const [openNewLocation, setOpenNewLocation] = useState(false);

  const closeDrawer = () => {
    setOpenNewLocation(false);
  }


  const items: GcMenuItem[] = [
    {
      buttonType: 'primary',
      icon: <PlusOutlined />,
      onClick: () => setOpenNewLocation(true),
      content: t('location.new.button')
    }
  ];

  return (
    <GcPageContainer>
      <GcPageContainerHeader
        title={t('common.locations')}
        isLoading={loading || locations.length === 0}
        items={items}
      />
      <GcPageContainerContent isLoading={loading || locations.length === 0}>
        <LocationTable />
        <NewLocationDrawer open={openNewLocation} onClose={closeDrawer} />
      </GcPageContainerContent>
    </GcPageContainer>
  )
}