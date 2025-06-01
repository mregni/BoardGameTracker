import { Route, Routes } from 'react-router-dom';

import { UpdateGamePage } from './UpdateGamePage';
import { GamesPage } from './GamesPage';
import { GameSessionsPage } from './GameSessionsPage';
import { GameDetailPage } from './GameDetailPage';
import { CreateGamePage } from './CreateGamePage';

export const GameRoutes = () => {
  return (
    <Routes>
      <Route element={<GamesPage />} path="" />
      <Route element={<CreateGamePage />} path="/new" />
      <Route element={<UpdateGamePage />} path="/:id/update" />
      <Route element={<GameDetailPage />} path="/:id" />
      <Route element={<GameSessionsPage />} path="/:id/sessions" />
    </Routes>
  );
};
