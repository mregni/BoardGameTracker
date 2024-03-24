import {Route, Routes} from 'react-router-dom';

import {GameDetailPage} from './GameDetailPage';
import {GamesPage} from './GamesPage';

export const GameRoutes = () => {
  return (
    <Routes>
      <Route
        element={<GamesPage />}
        path="">
      </Route>
      <Route
        element={<GameDetailPage />}
        path="/:id">
      </Route>
    </Routes>
  )
}