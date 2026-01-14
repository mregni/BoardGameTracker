import { PlayerBadgeContainer } from './PlayerBadgeContainer';

import { BgtPoster } from '@/routes/-components/BgtPoster';
import { Player } from '@/models';

interface Props {
  player: Player;
}

export const PlayerHeroSection = (props: Props) => {
  const { player } = props;

  return (
    <div className="flex flex-col lg:flex-row gap-6">
      <div className="aspect-square rounded-lg overflow-hidden bg-[#8502fb]/10 border border-[#8502fb]/20 w-48 mx-auto lg:mx-0">
        <BgtPoster title={player.name} image={player.image} />
      </div>

      <div className="flex flex-col flex-1 gap-2">
        <PlayerBadgeContainer badges={player.badges} />
      </div>
    </div>
  );
};
