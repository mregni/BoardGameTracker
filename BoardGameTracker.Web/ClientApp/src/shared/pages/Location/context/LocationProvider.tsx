import React, {ReactElement} from 'react';

import {LocationContext, useLocationContext} from './LocationState';

interface Props {
  children: ReactElement | ReactElement[];
}

export function LocationContextProvider(props: Props): ReactElement {
  const {children} = props;
  const locationContext = useLocationContext();

  return (
    <LocationContext.Provider value={locationContext}>
      {children}
    </LocationContext.Provider>
  );
}
