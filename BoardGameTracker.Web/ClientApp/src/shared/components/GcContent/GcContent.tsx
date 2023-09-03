import {Layout} from 'antd';
import React from 'react';
import {Route, Routes} from 'react-router-dom';

import {GameContainer} from '../../pages/Games';
import {LocationContainer} from '../../pages/Location';
import {PlayerContainer} from '../../pages/Players';
import {PlayerContextProvider} from '../../pages/Players/context';

const { Content } = Layout;

export const GcContent = () => {
  return (
    <Content style={{ margin: 0 }}>
      <PlayerContextProvider>
        <Routes>
          <Route
            element={<GameContainer />}
            path="*">
          </Route>
          <Route
            element={<GameContainer />}
            path="/games/*">
          </Route>
          <Route
            element={<PlayerContainer />}
            path="/players/*">
          </Route>
          <Route
            element={<LocationContainer />}
            path="/locations/*">
          </Route>
        </Routes>
      </PlayerContextProvider>
    </Content>
  )
}
