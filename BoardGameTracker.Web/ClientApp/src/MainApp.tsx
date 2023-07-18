import './MainApp.css';

import {Layout, theme} from 'antd';
import React from 'react';
import {BrowserRouter} from 'react-router-dom';

import {GcContent} from './shared/components/GcContent';
import {GcHeader} from './shared/components/GcHeader';
import {GcMenu} from './shared/components/GcMenu';

function MainApp() {
  const {token: { colorBgContainer }} = theme.useToken();
  return (
    <BrowserRouter future={{ v7_startTransition: true }}>
      <Layout style={{ minHeight: '100vh', background: colorBgContainer }}>
        <GcHeader />
        <Layout hasSider>
          <GcMenu />
          <Layout>
            <GcContent />
          </Layout>
        </Layout>
      </Layout>
    </BrowserRouter>
  )
}

export default MainApp
