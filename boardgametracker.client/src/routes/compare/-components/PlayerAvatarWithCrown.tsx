import { memo } from 'react';

import { BgtPoster } from '../../-components/BgtPoster';

import { Player } from '@/models';
import Crown from '@/assets/icons/crown.svg?react';

interface PlayerAvatarWithCrownProps {
  player: Player;
  isWinner: boolean;
}

const PlayerAvatarWithCrownComponent = ({ player, isWinner }: PlayerAvatarWithCrownProps) => {
  return (
    <div className="w-24 h-24 md:w-32 md:h-32 rounded-full flex items-center justify-center text-5xl md:text-6xl mb-4 relative">
      <BgtPoster title={player.name} image={player.image} />
      {isWinner && (
        <div className="absolute -top-2 -right-2 bg-linear-to-br from-yellow-400 to-yellow-600 rounded-full p-2 shadow-lg">
          <Crown className="text-yellow-900 fill-yellow-900" />
        </div>
      )}
    </div>
  );
};

PlayerAvatarWithCrownComponent.displayName = 'PlayerAvatarWithCrown';

export const PlayerAvatarWithCrown = memo(PlayerAvatarWithCrownComponent);
