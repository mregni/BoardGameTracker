import React, {ReactElement} from 'react';

import {PlayerContext, usePlayerContext} from './';

interface Props {
  children: ReactElement | ReactElement[];
}

export function PlayerContextProvider(props: Props): ReactElement {
  const {children} = props;
  const playerContext = usePlayerContext();

  return (
    <PlayerContext.Provider value={playerContext}>
      {children}
    </PlayerContext.Provider>
  );
}
