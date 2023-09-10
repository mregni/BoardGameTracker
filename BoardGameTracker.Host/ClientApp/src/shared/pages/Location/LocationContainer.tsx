import React from 'react';
import {Route, Routes} from 'react-router-dom';

import {LocationContextProvider} from './context/LocationProvider';
import {Location} from './Location';

export const LocationContainer = () => {
  return (
    <LocationContextProvider>
      <Routes>
        <Route
          element={<Location />}
          path="">
        </Route>
      </Routes>
    </LocationContextProvider>
  )
}