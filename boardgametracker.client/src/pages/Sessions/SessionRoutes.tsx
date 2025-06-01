import { Route, Routes } from 'react-router-dom';

import { UpdateSessionPage } from './UpdateSessionPage';
import { CreateSessionPage } from './CreateSessionPage';

export const SessionRoutes = () => {
  return (
    <Routes>
      <Route element={<CreateSessionPage />} path="create" />
      <Route element={<CreateSessionPage />} path="create/:gameId" />
      <Route element={<UpdateSessionPage />} path="update/:id" />
    </Routes>
  );
};
