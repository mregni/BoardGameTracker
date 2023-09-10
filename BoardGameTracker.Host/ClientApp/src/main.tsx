import './index.css';
import './i18n';

import React, {Suspense} from 'react';
import ReactDOM from 'react-dom/client';

import MainApp from './MainApp';
import {GcLoader} from './shared/components/GcLoader';
import {SettingsContextProvider} from './shared/context/settingsContext';
import {Config} from './shared/utils/config';

declare global {
  // eslint-disable-next-line no-unused-vars
  interface Window {
    runConfig: Config,
  }
}

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <Suspense fallback={<GcLoader loading={true} />}>
      <SettingsContextProvider>
        <MainApp />
      </SettingsContextProvider>
    </Suspense>
  </React.StrictMode >,
)
