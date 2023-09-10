import React, {ReactElement, useEffect} from 'react';

import {SettingsContext, useSettingsContext} from './';

interface Props {
  children: ReactElement | ReactElement[];
}

export function SettingsContextProvider(props: Props): ReactElement {
  const {children} = props;
  const settingsContext = useSettingsContext();

  return (
    <SettingsContext.Provider value={settingsContext}>
      {children}
    </SettingsContext.Provider>
  );
}
