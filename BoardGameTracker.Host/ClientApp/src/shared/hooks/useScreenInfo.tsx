import useBreakpoint from 'antd/lib/grid/hooks/useBreakpoint';
import {useContext} from 'react';

import {ResponsiveContext, ScreenInfo} from '../context/ResponsiveContext';

export function useScreenInfo(): ScreenInfo {
  const contextData = useContext(ResponsiveContext);
  if (contextData === undefined) {
    throw new Error('cannot use outside context');
  }

  return contextData;
}

export const ResponsiveGate: React.FC<{ loader?: JSX.Element; children?: React.ReactNode; }> = (props) => {
  const breakpoint = useBreakpoint();
  const canLiftGate = breakpoint.lg !== undefined;
  const isMobile = breakpoint.xs === true && breakpoint.md === false;
  return canLiftGate ? (
    <ResponsiveContext.Provider
      value={{
        screenMap: {
          xs: breakpoint.xs || false,
          sm: breakpoint.sm || false,
          md: breakpoint.md || false,
          lg: breakpoint.lg || false,
          xl: breakpoint.xl || false,
          xxl: breakpoint.xxl || false,
        },
        isMobile,
        isDesktop: !isMobile,
      }}
    >
      <>
        {props.children}
      </>
    </ResponsiveContext.Provider>
  ) : (
    props.loader || null
  );
};