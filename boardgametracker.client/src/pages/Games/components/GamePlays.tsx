import {DesktopDetails} from './GamePlaysDesktop';
import {MobileDetails} from './GamePlaysMobile';

export const GamePlays = () => {
  return (
    <>
      <div className="hidden md:flex md:flex-1">
        <DesktopDetails />
      </div>
      <div className="md:hidden">
        <MobileDetails />
      </div>
    </>
  )
}
