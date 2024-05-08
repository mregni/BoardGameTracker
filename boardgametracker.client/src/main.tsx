import './index.css';
import './utils/i18n';
import '@radix-ui/themes/styles.css';
import 'react-perfect-scrollbar/dist/css/styles.css';

import React, { Suspense } from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';

import { Theme } from '@radix-ui/themes';

import AppContainer from './App.tsx';
import { BgtToastProvider } from './providers/BgtToastProvider.tsx';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <Suspense>
      <BrowserRouter future={{ v7_startTransition: true }}>
        <Theme appearance="dark" accentColor="orange" grayColor="gray" panelBackground="solid" scaling="95%">
          <BgtToastProvider>
            <AppContainer />
          </BgtToastProvider>
        </Theme>
      </BrowserRouter>
    </Suspense>
  </React.StrictMode>
);
