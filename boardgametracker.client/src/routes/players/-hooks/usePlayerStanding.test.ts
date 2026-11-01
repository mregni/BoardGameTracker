import { renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({ data: undefined as unknown }));

vi.mock("@tanstack/react-query", async (importOriginal) => ({
	...(await importOriginal<typeof import("@tanstack/react-query")>()),
	useQuery: () => ({ data: mocks.data }),
}));

import { usePlayerStanding } from "./usePlayerStanding";

const leaderboard = {
	rankedCount: 4,
	players: [
		{ playerId: 1, rank: 2, isRanked: true, sessionsToRank: 0, playCount: 12 },
		{ playerId: 2, rank: 5, isRanked: false, sessionsToRank: 3, playCount: 0 },
	],
};

describe("usePlayerStanding", () => {
	beforeEach(() => {
		mocks.data = leaderboard;
	});

	it("returns the rank of a player on the leaderboard", () => {
		const { result } = renderHook(() => usePlayerStanding(1));

		expect(result.current).toEqual({ rank: 2, isRanked: true, rankedCount: 4, sessionsToRank: 0 });
	});

	it("returns null for a player without sessions", () => {
		const { result } = renderHook(() => usePlayerStanding(2));

		expect(result.current).toBeNull();
	});

	it("returns null while the leaderboard is loading", () => {
		mocks.data = undefined;
		const { result } = renderHook(() => usePlayerStanding(1));

		expect(result.current).toBeNull();
	});
});
