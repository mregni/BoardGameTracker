import React, {ReactElement} from 'react';

import {MenuContext, useMenuContext} from './';

interface Props {
  children: ReactElement | ReactElement[];
}

export function MenuContextProvider(props: Props): ReactElement {
  const {children} = props;
  const menuContext = useMenuContext();

  return (
    <MenuContext.Provider value={menuContext}>
      {children}
    </MenuContext.Provider>
  );
}
