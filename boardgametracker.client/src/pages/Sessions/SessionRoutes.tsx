import { Route, Routes } from 'react-router-dom';

import { CreateSessionPage } from './CreateSessionPage';

export const SessionRoutes = () => {
  return (
    <Routes>
      <Route element={<CreateSessionPage />} path="create" />
      <Route element={<CreateSessionPage />} path="create/:gameId" />
    </Routes>
  );
};
