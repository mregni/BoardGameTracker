import { useTranslation } from 'react-i18next';
import { memo } from 'react';
import { cx } from 'class-variance-authority';
import { useNavigate } from '@tanstack/react-router';

import { StringToHsl } from '@/utils/stringUtils';
import { RoundDecimal } from '@/utils/numberUtils';
import { usePlayerById } from '@/routes/-hooks/usePlayerById';
import { TopPlayer, Trend } from '@/models';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';
import TrendUpIcon from '@/assets/icons/trend-up.svg?react';
import TrendDownIcon from '@/assets/icons/trend-down.svg?react';

interface Props {
  player: TopPlayer;
}

const TopPlayerCardItemComponent = (props: Props) => {
  const { player } = props;
  const { t } = useTranslation();
  const { playerById } = usePlayerById();
  const navigate = useNavigate();

  return (
    <div className="flex items-center gap-4 bg-primary/5 rounded-lg p-4 border border-primary/10">
      <BgtAvatar
        onClick={() => navigate({ to: `/players/${player.playerId}` })}
        image={playerById(player.playerId)?.image}
        title={playerById(player.playerId)?.name}
        color={StringToHsl(playerById(player.playerId)?.name)}
        size="large"
      />
      <div className="flex-1">
        <BgtText className="text-white uppercase">{playerById(player.playerId)?.name}</BgtText>
        <BgtText className="text-primary/70">
          {t('common.win', { count: player.wins })} • {t('common.game', { count: player.playCount })}
        </BgtText>
      </div>
      <div className="text-right flex flex-col items-end">
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
        {player.averageScore && (
          <div className="text-white/50 text-sm">
            {player.averageScore} {t('statistics.average-abreviation')}
          </div>
        )}
      </div>
    </div>
  );
};

TopPlayerCardItemComponent.displayName = 'TopPlayerCardItem';

export const TopPlayerCardItem = memo(TopPlayerCardItemComponent);
