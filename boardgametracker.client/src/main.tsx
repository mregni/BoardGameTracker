import './index.css';
import './utils/i18n';
import '@radix-ui/themes/styles.css';
import 'react-perfect-scrollbar/dist/css/styles.css';

import { BrowserRouter } from 'react-router-dom';
import ReactDOM from 'react-dom/client';
import React, { Suspense } from 'react';
import { Theme } from '@radix-ui/themes';

import { BgtToastProvider } from './providers/BgtToastProvider.tsx';
import AppContainer from './App.tsx';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <Suspense>
      <BrowserRouter future={{ v7_startTransition: true }}>
        <Theme appearance="dark" accentColor="purple" grayColor="gray" panelBackground="solid" scaling="95%">
          <BgtToastProvider>
            <AppContainer />
          </BgtToastProvider>
        </Theme>
      </BrowserRouter>
    </Suspense>
  </React.StrictMode>
);
