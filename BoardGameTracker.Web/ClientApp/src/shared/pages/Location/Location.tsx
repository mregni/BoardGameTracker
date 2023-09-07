import {Button} from 'antd';
import React, {useContext, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {
  GcPageContainer, GcPageContainerContent, GcPageContainerHeader,
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
  return (
    <GcPageContainer>
      <GcPageContainerHeader
        title={t('common.locations')}
        isLoading={loading || locations.length === 0}
      >
        <Button
          type="primary"
          onClick={() => setOpenNewLocation(true)}
        >
          {t('common.new-location')}
        </Button>
      </GcPageContainerHeader>
      <GcPageContainerContent isLoading={loading || locations.length === 0}>
        <LocationTable />
        <NewLocationDrawer open={openNewLocation} onClose={closeDrawer} />
      </GcPageContainerContent>
    </GcPageContainer>
  )
}