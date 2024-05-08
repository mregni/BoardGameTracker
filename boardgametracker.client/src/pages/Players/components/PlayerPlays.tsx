import { PlayerPlaysDesktop } from './PlayerPlaysDesktop';
import { PlayerPlaysMobile } from './PlayerPlaysMobile';

export const PlayerPlays = () => {
  return (
    <>
      <div className="hidden md:flex md:flex-1">
        <PlayerPlaysDesktop />
      </div>
      <div className="md:hidden">
        <PlayerPlaysMobile />
      </div>
    </>
  );
};
