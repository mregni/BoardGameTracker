import {Route, Routes} from 'react-router-dom';

import {PlayersPage} from './PlayersPage';

export const PlayerRoutes = () => {
  return (
    <Routes>
      <Route
        element={<PlayersPage />}
        path="">
      </Route>
    </Routes>
  )
}