import { useMemo } from "react";

import { type Badge, BadgeLevel } from "@/models";

export interface BadgeWithEarnedStatus extends Badge {
	earned: boolean;
}

const levelOrder: BadgeLevel[] = [BadgeLevel.green, BadgeLevel.blue, BadgeLevel.red, BadgeLevel.gold];

export const useBadgeEarnedStatus = (allBadges: Badge[], playerBadges: Badge[]): BadgeWithEarnedStatus[] => {
	return useMemo(() => {
		const earnedBadgeIds = new Set(playerBadges.map((badge) => badge.id));

		const badgesWithStatus = allBadges.map((badge) => ({
			...badge,
			earned: earnedBadgeIds.has(badge.id),
		}));

		return badgesWithStatus.sort((a, b) => {
			if (a.type !== b.type) {
				return a.type.localeCompare(b.type);
			}

			if (a.level === null && b.level === null) return 0;
			if (a.level === null) return 1;
			if (b.level === null) return -1;

			return levelOrder.indexOf(a.level) - levelOrder.indexOf(b.level);
		});
	}, [allBadges, playerBadges]);
};
