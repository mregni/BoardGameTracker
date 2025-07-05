import { ComponentPropsWithoutRef } from 'react';
import { cx } from 'class-variance-authority';
import { useNavigate } from '@tanstack/react-router';

import { Player } from '@/models';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';

interface Props extends ComponentPropsWithoutRef<'div'> {
  player: Player;
}

export const PlayerSpotlight = (props: Props) => {
  const { player, className } = props;
  const navigate = useNavigate();

  return (
    <div className={cx('relative mb-10', className)}>
      <div className="flex flex-col items-center gap-1">
        <BgtAvatar image={player.image} size="big" onClick={() => navigate({ to: `/players/${player.id}` })} />
        <span className="font-bold">{player.name}</span>
      </div>
      <div
        className="absolute md:top-36 top-28 left-1/2 transform -translate-x-1/2 bg-gray-500 blur-sm opacity-70 z-0 w-[100%] h-[24px] md:w-[130%] md:h-[24px]"
        style={{
          borderRadius: '80%',
        }}
      ></div>
    </div>
  );
};
