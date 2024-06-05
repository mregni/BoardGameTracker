import { Route, Routes } from 'react-router-dom';

import { GamesPage } from './GamesPage';
import { GameDetailPage } from './GameDetailPage';

export const GameRoutes = () => {
  return (
    <Routes>
      <Route element={<GamesPage />} path="" />
      <Route element={<GameDetailPage />} path="/:id" />
    </Routes>
  );
};
