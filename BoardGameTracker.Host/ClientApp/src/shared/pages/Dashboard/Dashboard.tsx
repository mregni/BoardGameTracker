import React from 'react';
import {useTranslation} from 'react-i18next';

import {GcPageContainer, GcPageHeader} from '../../components/GcPageContainer';

export const Dashboard = () => {
  const { t } = useTranslation();

  return (
    <GcPageContainer>
      <GcPageHeader
        title={t('common.dashboard')}
        isLoading={false}
      />
    </GcPageContainer>
  )
}
