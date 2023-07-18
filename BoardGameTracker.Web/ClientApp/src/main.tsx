import './index.css';
import './i18n';

import {App, ConfigProvider, theme} from 'antd';
import React, {Suspense} from 'react';
import ReactDOM from 'react-dom/client';

import {purple} from '@ant-design/colors';

import MainApp from './MainApp';

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <Suspense fallback="... is loading">
      <ConfigProvider
        theme={{
          algorithm: theme.darkAlgorithm,
          token: {
            colorPrimary: purple[6]
          },
          components: {
            Layout: {
              colorBgHeader: purple[6],
              colorBgTrigger: purple[6]
            },
          }
        }}
      >
        <App>
          <MainApp />
        </App>
      </ConfigProvider>
    </Suspense>
  </React.StrictMode >,
)
