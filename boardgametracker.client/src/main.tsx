import '@radix-ui/themes/styles.css';
import './index.css';
import './utils/i18n';
import { Toaster } from 'sonner';
import { BrowserRouter } from 'react-router-dom';
import ReactDOM from 'react-dom/client';
import React, { Suspense } from 'react';
import { Theme } from '@radix-ui/themes';

import { classConfig } from './config/sonner.ts';
import AppContainer from './App.tsx';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <Suspense>
      <BrowserRouter future={{ v7_startTransition: true }}>
        <Theme appearance="dark" accentColor="purple" grayColor="gray" panelBackground="solid" scaling="95%">
          <Toaster toastOptions={{ unstyled: true, classNames: classConfig }} />
          <AppContainer />
        </Theme>
      </BrowserRouter>
    </Suspense>
  </React.StrictMode>
);
