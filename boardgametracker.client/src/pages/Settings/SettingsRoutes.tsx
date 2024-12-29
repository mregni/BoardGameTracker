import { Route, Routes } from 'react-router-dom';

import { SettingsPage } from './SettingsPage';

export const SettingsRoutes = () => {
  return (
    <Routes>
      <Route element={<SettingsPage />} path="/" />
    </Routes>
  );
};
