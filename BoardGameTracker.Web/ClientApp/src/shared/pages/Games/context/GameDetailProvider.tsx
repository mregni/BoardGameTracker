import React, {ReactElement} from 'react';

import {GameDetailContext, useGameDetailContext} from './GameDetailState';

interface Props {
  children: ReactElement | ReactElement[];
}

export function GameDetailContextProvider(props: Props): ReactElement {
  const {children} = props;
  const gameDetailContext = useGameDetailContext();

  return (
    <GameDetailContext.Provider value={gameDetailContext}>
      {children}
    </GameDetailContext.Provider>
  );
}
