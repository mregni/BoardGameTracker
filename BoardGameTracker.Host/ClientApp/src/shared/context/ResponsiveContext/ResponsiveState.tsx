import {Breakpoint} from 'antd';
import {createContext} from 'react';

export interface ScreenInfo {
  isMobile: boolean;
  isDesktop: boolean;
  screenMap: Record<Breakpoint, boolean>;
}

export const ResponsiveContext = createContext<ScreenInfo | undefined>(
  undefined
);

