import { Route, Routes } from 'react-router-dom';

import { PlayerDetailpage } from './PlayerDetailPage';
import { PlayersPage } from './PlayersPage';

export const PlayerRoutes = () => {
  return (
    <Routes>
      <Route element={<PlayersPage />} path="" />
      <Route element={<PlayerDetailpage />} path="/:id" />
    </Routes>
  );
};
