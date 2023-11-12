import {Space} from 'antd';
import React, {useContext} from 'react';
import {useTranslation} from 'react-i18next';

import {GcPageContainer, GcPageContent, GcPageHeader} from '../../components/GcPageContainer';
import {SettingsContext} from '../../context/settingsContext';
import {EnvironmentTable} from './components/EnvironmentTable';
import {LocalisationForm} from './components/LocalisationForm';

export const Settings = () => {
  const { t } = useTranslation();
  const { loading } = useContext(SettingsContext);

  return (
    <GcPageContainer>
      <GcPageHeader
        title={t('common.settings')}
        isLoading={loading}
      />
      <GcPageContent isLoading={loading}>
        <Space direction="vertical">
          <LocalisationForm />
          <EnvironmentTable />
        </Space>
      </GcPageContent>
    </GcPageContainer>
  )
}
