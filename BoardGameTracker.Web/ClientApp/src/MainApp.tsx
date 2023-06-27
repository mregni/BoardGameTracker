import './MainApp.css';

import {Layout, theme, Typography} from 'antd';
import React, {useState} from 'react';
import {BrowserRouter} from 'react-router-dom';

import {GcContent} from './shared/components/Gc/GcContent';
import {GcHeader} from './shared/components/Gc/GcHeader';
import {GcMenu} from './shared/components/Gc/GcMenu';

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
