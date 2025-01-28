import { Route, Routes } from 'react-router-dom';

import { NewGamePage } from './NewGamePage';
import { GamesPage } from './GamesPage';
import { GameSessionsPage } from './GameSessionsPage';
import { GameDetailPage } from './GameDetailPage';

export const GameRoutes = () => {
  return (
    <Routes>
      <Route element={<GamesPage />} path="" />
      <Route element={<NewGamePage />} path="/new" />
      <Route element={<GameDetailPage />} path="/:id" />
      <Route element={<GameSessionsPage />} path="/:id/sessions" />
    </Routes>
  );
};
