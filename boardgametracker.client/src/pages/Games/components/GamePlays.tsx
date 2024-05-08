import { DesktopDetails } from './GamePlaysDesktop';
import { MobileDetails } from './GamePlaysMobile';

export const GamePlays = () => {
  return (
    <>
      <div className="hidden md:flex md:flex-1 md:col-span-2 xl:col-span-3 2xl:col-span-3">
        <DesktopDetails />
      </div>
      <div className="md:hidden">
        <MobileDetails />
      </div>
    </>
  );
};
