import React, {useContext, useEffect} from 'react';
import {useParams} from 'react-router-dom';

import {GcNoDataLoader} from '../../components/GcNoDataLoader';
import GameDetailOverview from './components/GameDetailOverview';
import {GameDetailContextProvider} from './context/GameDetailProvider';
import {GameDetailContext} from './context/GameDetailState';

const GameDetailContainer = () => {
  const { id } = useParams();
  const { loading, loadGame, game } = useContext(GameDetailContext);

  useEffect(() => {
    loadGame(id ?? '');
  }, [id, loadGame]);

  return (
    <GcNoDataLoader isLoading={loading || game === null}>
      {game !== null && <GameDetailOverview />}
    </GcNoDataLoader>
  )
}

const GameDetail = () => {
  return (
    <GameDetailContextProvider>
      <GameDetailContainer />
    </GameDetailContextProvider>
  )
}

export default GameDetail