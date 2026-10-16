import { beforeEach, describe, expect, it, vi } from "vitest";

const axiosMock = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() }));

vi.mock("@/utils/axiosInstance", () => ({ axiosInstance: axiosMock }));

import { addSuggestedGameCall, removeSuggestedGameCall } from "./gameNightService";
import { getShameStatisticsCall, getShamesCall } from "./gameService";
import { getShameStatistics, getShames } from "./queries/games";

describe("shelf of shame calls", () => {
	beforeEach(() => {
		axiosMock.get.mockReset().mockResolvedValue({ data: "result" });
		axiosMock.put.mockReset().mockResolvedValue({ data: "night" });
		axiosMock.delete.mockReset().mockResolvedValue({ data: "ignored" });
	});

	it("fetches the shelf for the setting or for a longer period", async () => {
		await expect(getShamesCall()).resolves.toBe("result");
		expect(axiosMock.get).toHaveBeenLastCalledWith("game/shames", { params: undefined });

		await getShamesCall(12);
		expect(axiosMock.get).toHaveBeenLastCalledWith("game/shames", { params: { months: 12 } });
	});

	it("fetches the statistics for the same period", async () => {
		await expect(getShameStatisticsCall(24)).resolves.toBe("result");
		expect(axiosMock.get).toHaveBeenLastCalledWith("game/shames/statistics", { params: { months: 24 } });
	});

	it("adds one suggested game to a night", async () => {
		await expect(addSuggestedGameCall(4, 9)).resolves.toBe("night");
		expect(axiosMock.put).toHaveBeenCalledWith("gamenight/4/suggestions/9");
	});

	it("removes one suggested game from a night", async () => {
		await expect(removeSuggestedGameCall(4, 9)).resolves.toBeUndefined();
		expect(axiosMock.delete).toHaveBeenCalledWith("gamenight/4/suggestions/9");
	});

	it("keys the queries by period under the shames key", async () => {
		expect(getShames().queryKey).toEqual(["shames"]);
		expect(getShames(12).queryKey).toEqual(["shames", 12]);
		expect(getShameStatistics().queryKey).toEqual(["shames", "statistics"]);
		expect(getShameStatistics(12).queryKey).toEqual(["shames", "statistics", 12]);

		await getShames(12).queryFn?.({} as never);
		await getShameStatistics(12).queryFn?.({} as never);
		expect(axiosMock.get).toHaveBeenCalledWith("game/shames", { params: { months: 12 } });
		expect(axiosMock.get).toHaveBeenCalledWith("game/shames/statistics", { params: { months: 12 } });
	});
});
