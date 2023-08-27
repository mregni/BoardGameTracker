import {Layout, theme} from 'antd';
import React from 'react';
import {Route, Routes} from 'react-router-dom';

import AddPlay from '../../pages/AddPlay/AddPlay';
import {GameContainer} from '../../pages/Games';
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
            element={<AddPlay />}
            path="/plays/:id">
          </Route>
          <Route
            element={<PlayerContainer />}
            path="/players/*">
          </Route>
        </Routes>
      </PlayerContextProvider>
    </Content>
  )
}
