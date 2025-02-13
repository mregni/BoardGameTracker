import { Route, Routes } from 'react-router-dom';

import { PlayersPage } from './PlayersPage';
import { PlayerDetailpage } from './PlayerDetailPage';

export const PlayerRoutes = () => {
  return (
    <Routes>
      <Route element={<PlayersPage />} path="" />
      <Route element={<PlayerDetailpage />} path="/:id" />
    </Routes>
  );
};
