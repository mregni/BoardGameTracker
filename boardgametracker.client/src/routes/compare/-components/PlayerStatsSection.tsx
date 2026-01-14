import { useTranslation } from 'react-i18next';
import { memo, useCallback } from 'react';
import { useNavigate } from '@tanstack/react-router';

import { CompareData, getStatConfigs, isWinningValue } from '../-utils/compareUtils';

import { CompareCard } from './CompareCard';

import { StringToHsl } from '@/utils/stringUtils';
import { Player } from '@/models';
import { BgtHeading } from '@/components/BgtHeading/BgtHeading';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';

interface PlayerStatsSectionProps {
  player: Player;
  color: 'red' | 'blue';
  playerKey: 'playerOne' | 'playerTwo';
  opponentKey: 'playerOne' | 'playerTwo';
  compare: CompareData;
  uiLanguage: string;
}

const PlayerStatsSectionComponent = ({
  player,
  color,
  playerKey,
  opponentKey,
  compare,
  uiLanguage,
}: PlayerStatsSectionProps) => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const statConfigs = getStatConfigs(uiLanguage);

  const handleAvatarClick = useCallback(() => {
    navigate({ to: `/players/${player.id}` });
  }, [navigate, player.id]);

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-3 mb-4">
        <BgtAvatar
          onClick={handleAvatarClick}
          image={player.image}
          title={player.name}
          color={StringToHsl(player.name)}
          size="large"
        />
        <BgtHeading size="3" className="text-xl text-white">
          {t('compare.stats.player-stats', { player: player.name })}
        </BgtHeading>
      </div>

      {statConfigs.map((stat) => {
        const playerValue = stat.getRawValue(compare, playerKey);
        const opponentValue = stat.getRawValue(compare, opponentKey);
        const isWinner = isWinningValue(playerValue, opponentValue);

        return (
          <CompareCard
            key={stat.key}
            color={color}
            isWinner={isWinner}
            value={stat.getValue(compare, playerKey)}
            label={t(stat.translationKey)}
            icon={stat.icon}
          />
        );
      })}
    </div>
  );
};

PlayerStatsSectionComponent.displayName = 'PlayerStatsSection';

export const PlayerStatsSection = memo(PlayerStatsSectionComponent);
