import { beforeEach, describe, expect, it, vi } from "vitest";

const axiosMock = vi.hoisted(() => ({ get: vi.fn() }));

vi.mock("../utils/axiosInstance", () => ({ axiosInstance: axiosMock }));

import { getPlayersOverviewCall } from "./playerService";

describe("playerService", () => {
	beforeEach(() => {
		axiosMock.get.mockReset();
	});

	it("fetches the players overview", async () => {
		const overview = [{ id: 1, name: "Mikhael", image: null, playCount: 8, winCount: 5 }];
		axiosMock.get.mockResolvedValue({ data: overview });

		await expect(getPlayersOverviewCall()).resolves.toBe(overview);
		expect(axiosMock.get).toHaveBeenCalledWith("player/overview");
	});
});
