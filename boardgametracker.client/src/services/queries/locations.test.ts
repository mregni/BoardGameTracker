import { beforeEach, describe, expect, it, vi } from "vitest";
import { QUERY_KEYS } from "@/models";

const serviceMock = vi.hoisted(() => ({
	getLocationsCall: vi.fn(),
	getLocationCall: vi.fn(),
	getLocationStatisticsCall: vi.fn(),
	getLocationSessionsCall: vi.fn(),
	getPlayersOverviewCall: vi.fn(),
}));

vi.mock("../locationService", () => ({
	getLocationsCall: serviceMock.getLocationsCall,
	getLocationCall: serviceMock.getLocationCall,
	getLocationStatisticsCall: serviceMock.getLocationStatisticsCall,
	getLocationSessionsCall: serviceMock.getLocationSessionsCall,
}));

vi.mock("../playerService", () => ({
	getPlayersCall: vi.fn(),
	getPlayerCall: vi.fn(),
	getPlayerStatisticsCall: vi.fn(),
	getPlayerSessionsCall: vi.fn(),
	getPlayersOverviewCall: serviceMock.getPlayersOverviewCall,
}));

import { getLocation, getLocationSessions, getLocationStatistics, getLocations } from "./locations";
import { getPlayersOverview } from "./players";

const run = (options: { queryFn?: unknown }) => (options.queryFn as () => unknown)();

describe("location queries", () => {
	beforeEach(() => {
		for (const fn of Object.values(serviceMock)) {
			fn.mockReset().mockResolvedValue("data");
		}
	});

	it("lists the locations under the locations key", async () => {
		const options = getLocations();
		expect(options.queryKey).toEqual([QUERY_KEYS.locations]);
		await run(options);
		expect(serviceMock.getLocationsCall).toHaveBeenCalled();
	});

	it("keys one location under the locations key so list invalidations refresh it", async () => {
		const options = getLocation(3);
		expect(options.queryKey).toEqual([QUERY_KEYS.locations, 3]);
		await run(options);
		expect(serviceMock.getLocationCall).toHaveBeenCalledWith(3);
	});

	it("keys the statistics of a location below the location", async () => {
		const options = getLocationStatistics(3);
		expect(options.queryKey).toEqual([QUERY_KEYS.locations, 3, QUERY_KEYS.statistics]);
		await run(options);
		expect(serviceMock.getLocationStatisticsCall).toHaveBeenCalledWith(3, undefined);
	});

	it("keys the sessions of a location below the location", async () => {
		const options = getLocationSessions(3);
		expect(options.queryKey).toEqual([QUERY_KEYS.locations, 3, QUERY_KEYS.sessions]);
		await run(options);
		expect(serviceMock.getLocationSessionsCall).toHaveBeenCalledWith(3, undefined);
	});

	it("keys the players overview under the players key", async () => {
		const options = getPlayersOverview();
		expect(options.queryKey).toEqual([QUERY_KEYS.players, QUERY_KEYS.overview]);
		await run(options);
		expect(serviceMock.getPlayersOverviewCall).toHaveBeenCalled();
	});
});
