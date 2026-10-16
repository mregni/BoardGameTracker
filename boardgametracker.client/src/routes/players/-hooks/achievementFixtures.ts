import { type Badge, BadgeLevel, BadgeType } from "@/models";

const tiered = (id: number, type: BadgeType, level: BadgeLevel): Badge => ({
	id,
	titleKey: `${type}.${level}.title`,
	descriptionKey: `${type}.${level}.description`,
	type,
	level,
	image: `${type}-${level}.png`,
});

const feat = (id: number, type: BadgeType): Badge => ({
	id,
	titleKey: `${type}.title`,
	descriptionKey: `${type}.description`,
	type,
	level: null,
	image: `${type}.png`,
});

export const allBadges: Badge[] = [
	tiered(1, BadgeType.sessions, BadgeLevel.blue),
	tiered(2, BadgeType.sessions, BadgeLevel.green),
	tiered(3, BadgeType.sessions, BadgeLevel.gold),
	tiered(4, BadgeType.sessions, BadgeLevel.red),
	tiered(5, BadgeType.wins, BadgeLevel.green),
	tiered(6, BadgeType.wins, BadgeLevel.blue),
	tiered(7, BadgeType.wins, BadgeLevel.red),
	tiered(8, BadgeType.wins, BadgeLevel.gold),
	feat(9, BadgeType.firstTry),
	feat(10, BadgeType.marathonRunner),
];

export const earnedBadges: Badge[] = [allBadges[0], allBadges[1], allBadges[8]];
