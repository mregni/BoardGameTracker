import { useBadgeProcessing } from '../-hooks/useBadgeProcessing';

import { Badge } from '@/models';
import { BgtAchievementIcon } from '@/components/BgtAchievement/BgtAchievement';

interface Props {
  badges: Badge[];
}

export const PlayerBadgeContainer = (props: Props) => {
  const { badges } = props;
  const processedBadges = useBadgeProcessing(badges);

  return (
    <div className="flex flex-row flex-wrap gap-2">
      {processedBadges.displayBadges.map((badge) => (
        <BgtAchievementIcon key={badge.titleKey} badge={badge} />
      ))}
      {processedBadges.hiddenCount > 0 && (
        <div className="text-gray-500 text-xl flex items-center hover:cursor-pointer hover:underline">
          +{processedBadges.hiddenCount}
        </div>
      )}
    </div>
  );
};
