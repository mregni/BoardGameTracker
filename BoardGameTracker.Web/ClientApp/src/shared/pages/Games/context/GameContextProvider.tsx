import React, {ReactElement} from 'react';

import {GamesContext, useGamesContext} from './';

interface Props {
  children: ReactElement | ReactElement[];
}

export function GamesContextProvider(props: Props): ReactElement {
  const {children} = props;
  const gamesContext = useGamesContext();

  return (
    <GamesContext.Provider value={gamesContext}>
      {children}
    </GamesContext.Provider>
  );
}
