import './MainApp.css';

import {App, ConfigProvider, Layout, theme} from 'antd';
import React, {useContext} from 'react';
import {BrowserRouter} from 'react-router-dom';

import {purple} from '@ant-design/colors';

import {GcContent} from './shared/components/GcContent';
import {GcHeader} from './shared/components/GcHeader';
import {GcLoader} from './shared/components/GcLoader';
import {GcMenu} from './shared/components/GcMenu';
import {SettingsContext} from './shared/context/settingsContext';
import {useScreenInfo} from './shared/hooks/useScreenInfo';
import {useAntdLanguage} from './shared/utils';

function MainApp() {
  const { token: { colorBgContainer } } = theme.useToken();
  const { settings, loading, failed } = useContext(SettingsContext);
  const { getLocale } = useAntdLanguage();
  const { screenMap } = useScreenInfo();

  return (
    <GcLoader loading={loading} failed={failed} failedMessage="Failed loading settings, is the backend running?">
      <ConfigProvider
        locale={getLocale(settings?.uiLanguage)}
        theme={{
          algorithm: theme.darkAlgorithm,
          token: {
            colorPrimary: purple[6]
          },
          components: {
            Layout: {
              colorBgHeader: purple[6],
              colorBgTrigger: purple[6]
            }
          }
        }}
      >
        <App>
          <BrowserRouter future={{ v7_startTransition: true }}>
            <Layout style={{ minHeight: '100vh', background: colorBgContainer }}>
              <GcHeader />
              <Layout hasSider={screenMap.lg} style={{ height: '100%' }}>
                {screenMap.lg && (<GcMenu />)}
                <GcContent />
              </Layout>
            </Layout>
          </BrowserRouter>
        </App>
      </ConfigProvider>
    </GcLoader>
  )
}

export default MainApp
