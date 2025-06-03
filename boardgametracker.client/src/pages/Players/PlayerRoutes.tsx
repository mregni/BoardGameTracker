import { Route, Routes } from 'react-router-dom';

import { PlayersPage } from './PlayersPage';
import { PlayerSessionsPage } from './PlayerSessionsPage';
import { PlayerDetailpage } from './PlayerDetailPage';

export const PlayerRoutes = () => {
  return (
    <Routes>
      <Route element={<PlayersPage />} path="" />
      <Route element={<PlayerDetailpage />} path="/:id" />
      <Route element={<PlayerSessionsPage />} path="/:id/sessions" />
    </Routes>
  );
};
