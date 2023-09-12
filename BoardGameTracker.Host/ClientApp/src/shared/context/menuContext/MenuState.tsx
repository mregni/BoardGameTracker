import useBreakpoint from 'antd/lib/grid/hooks/useBreakpoint';
import {createContext, useEffect, useState} from 'react';

export interface MenuContextProps {
  collapse: (state: boolean) => void;
  collapsed: boolean;
  collapsedWidth: number;
}

export const MenuContext = createContext<MenuContextProps>(null!);

export const useMenuContext = (): MenuContextProps => {
  const [collapsed, setCollapsed] = useState(true);
  const [collapsedWidth, setCollapsedWidth] = useState(0);
  const screens = useBreakpoint();

  useEffect(() => {
    setCollapsedWidth(screens.lg ? 75 : 0);
  }, [screens.lg]);

  const collapse = (state: boolean): void => {
    setCollapsed(state);
  }

  return { collapsed, collapsedWidth, collapse }
};
