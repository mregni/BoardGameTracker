import {createContext, useEffect, useState} from 'react';

import {useScreenInfo} from '../../hooks/useScreenInfo';

export interface MenuContextProps {
  collapse: (state: boolean) => void;
  collapsed: boolean;
  collapsedWidth: number;
}

export const MenuContext = createContext<MenuContextProps>(null!);

export const useMenuContext = (): MenuContextProps => {
  const [collapsed, setCollapsed] = useState(true);
  const [collapsedWidth, setCollapsedWidth] = useState(0);
  const { screenMap } = useScreenInfo();

  useEffect(() => {
    setCollapsedWidth(screenMap.lg ? 75 : 0);
  }, [screenMap.lg]);

  const collapse = (state: boolean): void => {
    setCollapsed(state);
  }

  return { collapsed, collapsedWidth, collapse }
};
