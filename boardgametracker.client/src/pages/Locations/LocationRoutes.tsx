import { Route, Routes } from 'react-router-dom';

import { LocationsPage } from './LocationsPage';

export const LocationRoutes = () => {
  return (
    <Routes>
      <Route element={<LocationsPage />} path="" />
    </Routes>
  );
};
