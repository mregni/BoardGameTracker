import {App} from 'antd';
import React, {ReactElement} from 'react';

import {PlayContext, usePlayContext} from './PlayState';

interface Props {
  children: ReactElement | ReactElement[];
}

export function PlayContextProvider(props: Props): ReactElement {
  const {children} = props;
  const { notification } = App.useApp();
  const playContext = usePlayContext();

  return (
    <PlayContext.Provider value={playContext}>
      {children}
    </PlayContext.Provider>
  );
}
