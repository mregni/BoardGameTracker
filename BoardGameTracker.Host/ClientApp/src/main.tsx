import './index.css';
import './i18n';

import React, {Suspense} from 'react';
import ReactDOM from 'react-dom/client';

import MainApp from './MainApp';
import {GcLoader} from './shared/components/GcLoader';
import {SettingsContextProvider} from './shared/context/settingsContext';
import {ResponsiveGate} from './shared/hooks/useScreenInfo';

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <Suspense fallback={<GcLoader loading={true} />}>
      <SettingsContextProvider>
        <ResponsiveGate>
          <MainApp />
        </ResponsiveGate>
      </SettingsContextProvider>
    </Suspense>
  </React.StrictMode >,
)
