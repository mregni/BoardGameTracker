import React from 'react';
import {Route, Routes} from 'react-router-dom';

import {Settings} from './Settings';

export const SettingsContainer = () => {
  return (
    <Routes>
      <Route
        element={<Settings />}
        path="">
      </Route>
    </Routes>
  )
}
