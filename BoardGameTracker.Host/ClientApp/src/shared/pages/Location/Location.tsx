import React, {useContext, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {PlusOutlined} from '@ant-design/icons';

import {
  GcMenuItem, GcPageContainer, GcPageContent, GcPageDrawer, GcPageHeader,
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
      <GcPageHeader
        title={t('common.locations')}
        isLoading={loading}
        items={items}
      />
      <GcPageContent isLoading={loading} hasData={locations.length !== 0}>
        <LocationTable />
      </GcPageContent>
      <GcPageDrawer>
        <NewLocationDrawer open={openNewLocation} onClose={closeDrawer} />
      </GcPageDrawer>
    </GcPageContainer>
  )
}