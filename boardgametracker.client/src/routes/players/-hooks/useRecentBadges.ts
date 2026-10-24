import { useMemo } from "react";
import { type Badge, BadgeLevel } from "@/models";

export const RECENT_BADGE_LIMIT = 5;

const levelRank: Record<BadgeLevel, number> = {
	[BadgeLevel.green]: 1,
	[BadgeLevel.blue]: 2,
	[BadgeLevel.red]: 3,
	[BadgeLevel.gold]: 4,
};

const awardedTime = (badge: Badge) => (badge.awardedAt ? Date.parse(badge.awardedAt) : Number.NEGATIVE_INFINITY);

const rank = (badge: Badge) => (badge.level ? levelRank[badge.level] : 0);

export const sortByRecency = (badges: Badge[]): Badge[] =>
	[...badges].sort((a, b) => awardedTime(b) - awardedTime(a) || rank(b) - rank(a));

export const highestPerType = (badges: Badge[]): Badge[] => {
	const best = new Map<Badge["type"], Badge>();
	for (const badge of badges) {
		const current = best.get(badge.type);
		if (!current || rank(badge) > rank(current)) {
			best.set(badge.type, badge);
		}
	}
	return [...best.values()];
};

export const useRecentBadges = (playerBadges: Badge[], limit = RECENT_BADGE_LIMIT): Badge[] =>
	useMemo(() => sortByRecency(highestPerType(playerBadges)).slice(0, limit), [playerBadges, limit]);
