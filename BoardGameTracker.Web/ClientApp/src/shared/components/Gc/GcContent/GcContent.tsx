import {Layout, theme, Typography} from 'antd';
import React from 'react';
import {useTranslation} from 'react-i18next';
import {Route, Routes} from 'react-router-dom';

import {Games} from '../../Games';
import {Users} from '../../Users';

const { Content } = Layout;

export const GcContent = () => {
  const {
    token: { colorBgContainer },
  } = theme.useToken();

  return (
    <Content
      style={{
        padding: 24,
        margin: 0,
        background: colorBgContainer
      }}
    >
      <Routes>
        <Route
          element={<Games />}
          path="">
        </Route>
        <Route
          element={<Games />}
          path="/games">
        </Route>
        <Route
          element={<Users />}
          path="/users">
        </Route>
      </Routes>
    </Content>
  )
}
