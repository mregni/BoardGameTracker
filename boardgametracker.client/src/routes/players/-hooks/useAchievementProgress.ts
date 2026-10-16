import { useMemo } from "react";
import { type Badge, BadgeLevel, type BadgeType } from "@/models";

const levelOrder: BadgeLevel[] = [BadgeLevel.green, BadgeLevel.blue, BadgeLevel.red, BadgeLevel.gold];

export interface Milestone {
	type: BadgeType;
	levels: Badge[];
	earnedLevels: BadgeLevel[];
	current: Badge | null;
	next: Badge | null;
}

export interface Feat {
	badge: Badge;
	earned: boolean;
}

export interface AchievementProgress {
	milestones: Milestone[];
	feats: Feat[];
	earnedCount: number;
	total: number;
}

export const useAchievementProgress = (allBadges: Badge[], playerBadges: Badge[]): AchievementProgress => {
	return useMemo(() => {
		const earnedIds = new Set(playerBadges.map((badge) => badge.id));
		const levelsByType = new Map<BadgeType, Badge[]>();
		const feats: Feat[] = [];

		for (const badge of allBadges) {
			if (badge.level === null) {
				feats.push({ badge, earned: earnedIds.has(badge.id) });
				continue;
			}
			levelsByType.set(badge.type, [...(levelsByType.get(badge.type) ?? []), badge]);
		}

		const milestones = [...levelsByType.entries()].map(([type, badges]) => {
			const levels = [...badges].sort(
				(a, b) => levelOrder.indexOf(a.level as BadgeLevel) - levelOrder.indexOf(b.level as BadgeLevel),
			);
			const earned = levels.filter((badge) => earnedIds.has(badge.id));
			return {
				type,
				levels,
				earnedLevels: earned.map((badge) => badge.level as BadgeLevel),
				current: earned.at(-1) ?? null,
				next: levels.find((badge) => !earnedIds.has(badge.id)) ?? null,
			};
		});

		return {
			milestones,
			feats,
			earnedCount:
				milestones.filter((milestone) => milestone.earnedLevels.length > 0).length +
				feats.filter((feat) => feat.earned).length,
			total: milestones.length + feats.length,
		};
	}, [allBadges, playerBadges]);
};
