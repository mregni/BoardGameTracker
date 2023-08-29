import React, {useContext, useEffect} from 'react';
import {useParams} from 'react-router-dom';

import {GcNoDataLoader} from '../../components/GcNoDataLoader';
import {GamesContextProvider} from '../Games/context';
import {PlayerDetailOverview} from './components/PlayerDetailOverview';
import {PlayerDetailContextProvider} from './context/PlayerDetailProvider';
import {PlayerDetailContext} from './context/PlayerDetailState';

export const PlayerDetailContainer = () => {
  const { id } = useParams();
  const { loading, loadPlayer, player } = useContext(PlayerDetailContext);

  useEffect(() => {
    loadPlayer(id ?? '');
  }, [id, loadPlayer]);

  return (
    <GcNoDataLoader isLoading={loading || player == null}>
      {player !== null && <PlayerDetailOverview />}
    </GcNoDataLoader>
  )
}

export const PlayerDetail = () => {
  return (
    <PlayerDetailContextProvider>
      <GamesContextProvider>
        <PlayerDetailContainer />
      </GamesContextProvider>
    </PlayerDetailContextProvider>
  )
}
