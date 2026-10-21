import { beforeEach, describe, expect, it, vi } from "vitest";

const axiosMock = vi.hoisted(() => ({ get: vi.fn(), put: vi.fn() }));

vi.mock("../utils/axiosInstance", () => ({ axiosInstance: axiosMock }));

import { getPlayersOverviewCall, linkPlayerAccountCall } from "./playerService";

describe("playerService", () => {
	beforeEach(() => {
		axiosMock.get.mockReset();
		axiosMock.put.mockReset();
	});

	it("fetches the players overview", async () => {
		const overview = [{ id: 1, name: "Mikhael", image: null, playCount: 8, winCount: 5 }];
		axiosMock.get.mockResolvedValue({ data: overview });

		await expect(getPlayersOverviewCall()).resolves.toBe(overview);
		expect(axiosMock.get).toHaveBeenCalledWith("player/overview");
	});

	it("links an account to a player through the admin endpoint", async () => {
		const player = { id: 4, name: "Jane", image: null, email: null, badges: [] };
		axiosMock.put.mockResolvedValue({ data: player });

		await expect(linkPlayerAccountCall(4, "u1")).resolves.toBe(player);
		expect(axiosMock.put).toHaveBeenCalledWith("admin/players/4/account", { userId: "u1" });
	});

	it("unlinks the account with an empty user", async () => {
		axiosMock.put.mockResolvedValue({ data: {} });

		await linkPlayerAccountCall(4, null);

		expect(axiosMock.put).toHaveBeenCalledWith("admin/players/4/account", { userId: null });
	});
});
