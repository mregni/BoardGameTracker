import { useTranslation } from 'react-i18next';

import { useBadgeEarnedStatus } from '../-hooks/useBadgeEarnedStatus';

import { Badge } from '@/models';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import { BgtAchievement } from '@/components/BgtAchievement/BgtAchievement';
import Award from '@/assets/icons/award.svg?react';

interface Props {
  playerBadges: Badge[];
  badges: Badge[];
}

export const PlayerAchievementsCard = (props: Props) => {
  const { badges, playerBadges } = props;
  const { t } = useTranslation();

  const badgesWithEarnedStatus = useBadgeEarnedStatus(badges, playerBadges);

  return (
    <BgtCard title={`${t('player.titles.achievements')} (${playerBadges.length}/${badges.length})`} icon={Award}>
      <div className="space-y-3 max-h-96 overflow-y-auto pr-2">
        {badgesWithEarnedStatus.map((badge) => (
          <BgtAchievement badge={badge} earned={badge.earned} key={badge.id} />
        ))}
      </div>
    </BgtCard>
  );
};
