import { useMemo } from 'react';

import { Badge, BadgeType } from '@/models';

export const useBadgeProcessing = (badges: Badge[]) => {
  return useMemo(() => {
    if (!badges?.length) {
      return { displayBadges: [], hiddenCount: 0 };
    }

    const badgesByType = badges.reduce(
      (acc, badge) => {
        if (!acc[badge.type]) {
          acc[badge.type] = [];
        }
        acc[badge.type].push(badge);
        return acc;
      },
      {} as Record<BadgeType, Badge[]>
    );

    const highestLevelBadges: Badge[] = [];
    let totalHiddenCount = 0;

    Object.values(badgesByType).forEach((typeBadges) => {
      const sortedBadges = typeBadges.sort((a, b) => {
        if (a.level === null) return 1;
        if (b.level === null) return -1;
        return b.level - a.level;
      });

      highestLevelBadges.push(sortedBadges[0]);
      totalHiddenCount += sortedBadges.length - 1;
    });

    return {
      displayBadges: highestLevelBadges,
      hiddenCount: totalHiddenCount,
    };
  }, [badges]);
};
