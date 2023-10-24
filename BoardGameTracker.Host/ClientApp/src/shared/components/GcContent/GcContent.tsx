import {Layout} from 'antd';
import React from 'react';
import {Route, Routes} from 'react-router-dom';

import {DashboardContainer} from '../../pages/Dashboard';
import {GameContainer} from '../../pages/Games';
import {LocationContainer} from '../../pages/Location';
import {PlayerContainer} from '../../pages/Players';
import {PlayerContextProvider} from '../../pages/Players/context';
import {SettingsContainer} from '../../pages/Settings';

const { Content } = Layout;

export const GcContent = () => {
  return (
    <Content style={{ margin: 0 }}>
      <PlayerContextProvider>
        <Routes>
          <Route element={<DashboardContainer />} path="/*" />
          <Route element={<DashboardContainer />} path="/home/*" />
          <Route element={<GameContainer />} path="/games/*" />
          <Route element={<PlayerContainer />} path="/players/*" />
          <Route element={<LocationContainer />} path="/locations/*"/>
          <Route element={<SettingsContainer />} path="/settings/*" />
        </Routes>
      </PlayerContextProvider>
    </Content>
  )
}
