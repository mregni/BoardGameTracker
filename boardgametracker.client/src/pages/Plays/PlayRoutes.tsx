import { Route, Routes } from 'react-router-dom';

import { CreatePlayPage } from './CreatePlayPage';

export const PlayRoutes = () => {
  return (
    <Routes>
      <Route element={<CreatePlayPage />} path="create" />
      <Route element={<CreatePlayPage />} path="create/:gameId" />
    </Routes>
  );
};
