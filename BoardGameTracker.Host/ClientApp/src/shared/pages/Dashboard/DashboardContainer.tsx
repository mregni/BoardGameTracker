import {Route, Routes} from 'react-router-dom';

import {Dashboard} from './Dashboard';

export const DashboardContainer = () => {
  return (
    <Routes>
      <Route
        element={<Dashboard />}
        path="">
      </Route>
    </Routes>
  )
}
