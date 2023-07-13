import React from 'react';
import {Route, Routes} from 'react-router-dom';

import GameDetail from '../GameDetail/GameDetail';
import {GamesContextProvider} from './context';
import {Games} from './Games';

export const GameContainer = () => {
  return (
    <GamesContextProvider>
      <Routes>
        <Route
          element={<Games />}
          path="">
        </Route>
        <Route
          element={<GameDetail />}
          path="/:id">
        </Route>
      </Routes>
    </GamesContextProvider>
  )
}
