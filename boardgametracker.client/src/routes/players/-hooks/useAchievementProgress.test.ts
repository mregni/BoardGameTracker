import { renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { BadgeLevel, BadgeType } from "@/models";
import { allBadges, earnedBadges } from "./achievementFixtures";
import { useAchievementProgress } from "./useAchievementProgress";

describe("useAchievementProgress", () => {
	it("counts earned badges against every badge", () => {
		const { result } = renderHook(() => useAchievementProgress(allBadges, earnedBadges));

		expect(result.current.earnedCount).toBe(3);
		expect(result.current.total).toBe(10);
	});

	it("orders each milestone's levels from green to gold and points at the next one", () => {
		const { result } = renderHook(() => useAchievementProgress(allBadges, earnedBadges));
		const sessions = result.current.milestones.find((milestone) => milestone.type === BadgeType.sessions);

		expect(sessions?.levels.map((badge) => badge.level)).toEqual([
			BadgeLevel.green,
			BadgeLevel.blue,
			BadgeLevel.red,
			BadgeLevel.gold,
		]);
		expect(sessions?.earnedLevels).toEqual([BadgeLevel.green, BadgeLevel.blue]);
		expect(sessions?.current?.level).toBe(BadgeLevel.blue);
		expect(sessions?.next?.level).toBe(BadgeLevel.red);
	});

	it("has no current level for an untouched milestone", () => {
		const { result } = renderHook(() => useAchievementProgress(allBadges, earnedBadges));
		const wins = result.current.milestones.find((milestone) => milestone.type === BadgeType.wins);

		expect(wins?.current).toBeNull();
		expect(wins?.next?.level).toBe(BadgeLevel.green);
	});

	it("has no next level once gold is earned", () => {
		const sessions = allBadges.filter((badge) => badge.type === BadgeType.sessions);
		const { result } = renderHook(() => useAchievementProgress(allBadges, sessions));

		expect(result.current.milestones.find((milestone) => milestone.type === BadgeType.sessions)?.next).toBeNull();
	});

	it("lists one-off badges as feats with their earned state", () => {
		const { result } = renderHook(() => useAchievementProgress(allBadges, earnedBadges));

		expect(result.current.feats.map((feat) => [feat.badge.type, feat.earned])).toEqual([
			[BadgeType.firstTry, true],
			[BadgeType.marathonRunner, false],
		]);
	});
});
