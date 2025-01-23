import { useNavigate } from 'react-router-dom';
import { cx } from 'class-variance-authority';

import { StringToHsl } from '@/utils/stringUtils';
import { RoundDecimal } from '@/utils/numberUtils';
import { TopPlayer, Trend } from '@/models/Games/TopPlayer';
import { usePlayerById } from '@/hooks/usePlayerById';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';
import TrendUpIcon from '@/assets/icons/trend-up.svg?react';
import TrendDownIcon from '@/assets/icons/trend-down.svg?react';
import StarIcon from '@/assets/icons/star.svg';

interface Props {
  player: TopPlayer;
  index: number;
}

export const TopPlayerCard = (props: Props) => {
  const { player, index } = props;
  const { playerById } = usePlayerById();
  const navigate = useNavigate();

  return (
    <BgtCard
      className={cx(
        'w-full',
        index === 0 && 'block',
        index === 1 && 'block',
        index === 2 && 'hidden md:block',
        index === 3 && 'hidden lg:block',
        index === 4 && 'hidden xl:block'
      )}
    >
      <div className="flex flex-col gap-5 p-3">
        <div className="flex flex-col gap-2 items-center">
          <BgtAvatar
            onClick={() => navigate(`/players/${player.playerId}`)}
            image={playerById(player.playerId)?.image}
            title={playerById(player.playerId)?.name}
            color={StringToHsl(playerById(player.playerId)?.name)}
            size="big"
          />
          <BgtText size="4" className="uppercase">
            {playerById(player.playerId)?.name}
          </BgtText>
          <div
            className={cx(
              'flex flex-row gap-2',
              player.trend === Trend.Up && 'text-green-400',
              player.trend === Trend.Down && 'text-red-500',
              player.trend === Trend.Equal && 'text-orange-400'
            )}
          >
            {player.trend === Trend.Up && <TrendUpIcon className="size-5 mt-0.5" />}
            {player.trend === Trend.Down && <TrendDownIcon className="size-5 mt-0.5" />}
            {RoundDecimal(player.winPercentage * 100, 0.1)}%
          </div>
        </div>
        <BgtCard className="bg-card-light">
          <div className="flex flex-row gap-2 justify-center items-center">
            <img src={StarIcon} className="h-5" />
            <BgtText size="5">
              {player.wins} / {player.playCount}
            </BgtText>
          </div>
        </BgtCard>
      </div>
    </BgtCard>
  );
};
