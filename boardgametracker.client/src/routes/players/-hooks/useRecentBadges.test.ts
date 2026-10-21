import { renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { Badge } from "@/models";
import { allBadges } from "./achievementFixtures";
import { RECENT_BADGE_LIMIT, useRecentBadges } from "./useRecentBadges";

const earned = (index: number, awardedAt: string | null): Badge => ({ ...allBadges[index], awardedAt });

describe("useRecentBadges", () => {
	it("puts the most recently earned badge first", () => {
		const badges = [
			earned(4, "2026-01-01T10:00:00Z"),
			earned(8, "2026-03-01T10:00:00Z"),
			earned(1, "2026-02-01T10:00:00Z"),
		];

		const { result } = renderHook(() => useRecentBadges(badges));

		expect(result.current.map((badge) => badge.id)).toEqual([9, 2, 5]);
	});

	it("puts badges without a date last, higher levels first", () => {
		const badges = [
			earned(1, null),
			earned(9, null),
			earned(2, null),
			earned(0, null),
			earned(5, "2026-01-01T10:00:00Z"),
		];

		const { result } = renderHook(() => useRecentBadges(badges));

		expect(result.current.map((badge) => badge.id)).toEqual([6, 3, 1, 2, 10]);
	});

	it("shows a newer level of the same type as its own badge", () => {
		const badges = [earned(1, "2026-01-01T10:00:00Z"), earned(0, "2026-02-01T10:00:00Z")];

		const { result } = renderHook(() => useRecentBadges(badges));

		expect(result.current.map((badge) => badge.id)).toEqual([1, 2]);
	});

	it("keeps only the five most recent badges", () => {
		const badges = allBadges.map((badge, index) => ({
			...badge,
			awardedAt: `2026-01-${String(index + 1).padStart(2, "0")}T10:00:00Z`,
		}));

		const { result } = renderHook(() => useRecentBadges(badges));

		expect(RECENT_BADGE_LIMIT).toBe(5);
		expect(result.current.map((badge) => badge.id)).toEqual([10, 9, 8, 7, 6]);
	});

	it("returns nothing when the player has no badges", () => {
		const { result } = renderHook(() => useRecentBadges([]));

		expect(result.current).toEqual([]);
	});
});
