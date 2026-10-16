import { beforeEach, describe, expect, it, vi } from "vitest";

const axiosMock = vi.hoisted(() => ({ get: vi.fn() }));

vi.mock("../utils/axiosInstance", () => ({ axiosInstance: axiosMock }));

import { getLeaderboardCall } from "./leaderboardService";
import { getLeaderboard } from "./queries/leaderboard";

describe("leaderboardService", () => {
	beforeEach(() => {
		axiosMock.get.mockReset();
	});

	it("sends the filters as query parameters", async () => {
		const leaderboard = { players: [] };
		const params = { from: "2026-01-01", gameId: 3, minSessions: 3, rankBy: "winRate" as const };
		axiosMock.get.mockResolvedValue({ data: leaderboard });

		await expect(getLeaderboardCall(params)).resolves.toBe(leaderboard);
		expect(axiosMock.get).toHaveBeenCalledWith("leaderboard", { params });
	});

	it("keys the query by its filters", () => {
		const params = { minSessions: 5, rankBy: "wins" as const };

		expect(getLeaderboard(params).queryKey).toEqual(["leaderboard", params]);
	});
});
