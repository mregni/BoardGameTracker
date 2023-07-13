import './MainApp.css';

import {Layout} from 'antd';
import React from 'react';
import {BrowserRouter} from 'react-router-dom';

import {GcContent} from './shared/components/GcContent';
import {GcHeader} from './shared/components/GcHeader';
import {GcMenu} from './shared/components/GcMenu';

function MainApp() {
  return (
    <BrowserRouter future={{ v7_startTransition: true }}>
      <Layout style={{ minHeight: '100vh' }}>
        <GcHeader />
        <Layout>
          <GcMenu />
          <Layout style={{ padding: '24px 24px 24px' }}>
            <GcContent />
          </Layout>
        </Layout>
      </Layout>
    </BrowserRouter>
  )
}

export default MainApp
