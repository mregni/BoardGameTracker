import React, {useContext, useEffect} from 'react';
import {useParams} from 'react-router-dom';

import {GcNoDataLoader} from '../../components/GcNoDataLoader';
import GameDetailOverview from './components/GameDetailOverview';
import {GameDetailContext, GameDetailContextProvider} from './context';

const GameDetailContainer = () => {
  const { id } = useParams();
  const { loading, loadGame, game } = useContext(GameDetailContext);

  useEffect(() => {
    loadGame(id ?? '');
  }, [id, loadGame]);

  return (
    <GcNoDataLoader isLoading={loading} hasData={game !== null}>
      {game !== null && <GameDetailOverview key={game.id} />}
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