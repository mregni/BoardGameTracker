import {Button} from 'antd';
import React, {useContext} from 'react';
import {useTranslation} from 'react-i18next';

import {
  GcPageContainer, GcPageContainerContent, GcPageContainerHeader,
} from '../../components/GcPageContainer';
import {LocationContext} from './context/LocationState';

export const Location = () => {
  const { locations, loading } = useContext(LocationContext);
  const { t } = useTranslation();

  console.log(locations);
  return (
    <GcPageContainer>
      <GcPageContainerHeader
        title={t('common.locations')}
        isLoading={loading || locations.length === 0}
      >
        <Button
          type="primary"
        >
          {t('common.add-new')}
        </Button>
      </GcPageContainerHeader>
      <GcPageContainerContent isLoading={loading || locations.length === 0}>
        <>
          {locations.map(location => <span>{location.name}</span>)}
        </>
      </GcPageContainerContent>
    </GcPageContainer>
  )
}