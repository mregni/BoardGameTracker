import React, {ReactElement} from 'react';

import {PlayerDetailContext, usePlayerDetailContext} from './PlayerDetailState';

interface Props {
  children: ReactElement | ReactElement[];
}

export function PlayerDetailContextProvider(props: Props): ReactElement {
  const {children} = props;
  const playerDetailContext = usePlayerDetailContext();

  return (
    <PlayerDetailContext.Provider value={playerDetailContext}>
      {children}
    </PlayerDetailContext.Provider>
  );
}
