import './App.css';

import BgtContainer from './components/BgtLayout/BgtContainer';
import {useRemoteSettings} from './hooks/useRemoteSettings';

function App() {
  const { settings } = useRemoteSettings();

  console.log(settings)

  if (!settings) return null;

  return (
    <BgtContainer />
  )
}

export default App
