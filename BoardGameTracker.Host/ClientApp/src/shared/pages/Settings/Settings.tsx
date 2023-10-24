import {Divider, Space} from 'antd';
import React, {useContext} from 'react';
import {useTranslation} from 'react-i18next';

import {GcPageContainer, GcPageContent, GcPageHeader} from '../../components/GcPageContainer';
import {SettingsContext} from '../../context/settingsContext';
import {LocalisationForm} from './components/LocalisationForm';

export const Settings = () => {
  const { t } = useTranslation();
  const { settings, loading } = useContext(SettingsContext);

  return (
    <GcPageContainer>
      <GcPageHeader
        title={t('common.settings')}
        isLoading={loading}
      />
      <GcPageContent isLoading={loading}>
        <Space direction="vertical">
          <LocalisationForm />
          <h3>Dates</h3>
          <h3>Environment variables</h3>
        </Space>
      </GcPageContent>
    </GcPageContainer>
  )
}
