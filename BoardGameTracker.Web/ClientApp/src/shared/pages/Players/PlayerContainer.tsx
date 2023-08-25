import React from 'react';
import {Route, Routes} from 'react-router-dom';

import {PlayerDetail} from '../PlayerDetail';
import {Players} from './Players';

export const PlayerContainer = () => {
  return (
    <Routes>
      <Route
        element={<Players />}
        path="">
      </Route>
      <Route
        element={<PlayerDetail />}
        path="/:id">
      </Route>
    </Routes>
  )
}
