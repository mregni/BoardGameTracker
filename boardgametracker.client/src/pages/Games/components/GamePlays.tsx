import { useMediaQuery } from 'react-responsive';

import { MobileDetails } from './GamePlaysMobile';
import { DesktopDetails } from './GamePlaysDesktop';

export const GamePlays = () => {
  const isDekstop = useMediaQuery({ query: '(min-width: 768px)' });

  return (
    <>
      {isDekstop && (
        <div className="md:flex md:flex-1 md:col-span-2 xl:col-span-3 2xl:col-span-3">
          <DesktopDetails />
        </div>
      )}
      {!isDekstop && (
        <div>
          <MobileDetails />
        </div>
      )}
    </>
  );
};
