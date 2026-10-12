import { beforeEach, describe, expect, it, vi } from "vitest";

const axiosMock = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() }));

vi.mock("../utils/axiosInstance", () => ({ axiosInstance: axiosMock }));

import {
	addLocationCall,
	deleteLocationCall,
	getLocationCall,
	getLocationSessionsCall,
	getLocationStatisticsCall,
	getLocationsCall,
	updateLocationCall,
} from "./locationService";

describe("locationService", () => {
	beforeEach(() => {
		axiosMock.get.mockReset().mockResolvedValue({ data: "result" });
		axiosMock.post.mockReset().mockResolvedValue({ data: "created" });
		axiosMock.put.mockReset().mockResolvedValue({ data: "updated" });
		axiosMock.delete.mockReset().mockResolvedValue({});
	});

	it("fetches every location", async () => {
		await expect(getLocationsCall()).resolves.toBe("result");
		expect(axiosMock.get).toHaveBeenCalledWith("location");
	});

	it("fetches one location", async () => {
		await expect(getLocationCall(3)).resolves.toBe("result");
		expect(axiosMock.get).toHaveBeenCalledWith("location/3");
	});

	it("fetches the statistics of a location", async () => {
		await expect(getLocationStatisticsCall(3)).resolves.toBe("result");
		expect(axiosMock.get).toHaveBeenCalledWith("location/3/statistics");
	});

	it("fetches the sessions of a location", async () => {
		await expect(getLocationSessionsCall(3)).resolves.toBe("result");
		expect(axiosMock.get).toHaveBeenCalledWith("location/3/sessions");
	});

	it("creates a location", async () => {
		await expect(addLocationCall({ name: "Thuis" })).resolves.toBe("created");
		expect(axiosMock.post).toHaveBeenCalledWith("location", { name: "Thuis" });
	});

	it("updates a location", async () => {
		const location = { id: 3, name: "Thuis", playCount: 2 };
		await expect(updateLocationCall(location)).resolves.toBe("updated");
		expect(axiosMock.put).toHaveBeenCalledWith("location", location);
	});

	it("deletes a location", async () => {
		await deleteLocationCall(3);
		expect(axiosMock.delete).toHaveBeenCalledWith("location/3");
	});
});
