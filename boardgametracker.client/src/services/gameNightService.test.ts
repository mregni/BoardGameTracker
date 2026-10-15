import { beforeEach, describe, expect, it, vi } from "vitest";

const axiosMock = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn(), delete: vi.fn() }));

vi.mock("../utils/axiosInstance", () => ({ axiosInstance: axiosMock }));

import {
	discardRunningGameCall,
	endGameNightCall,
	finishRunningGameCall,
	getGameNightCall,
	reopenGameNightCall,
	startRunningGameCall,
} from "./gameNightService";

describe("gameNightService", () => {
	beforeEach(() => {
		axiosMock.get.mockReset().mockResolvedValue({ data: "night" });
		axiosMock.post.mockReset().mockResolvedValue({ data: "created" });
		axiosMock.delete.mockReset().mockResolvedValue({});
	});

	it("fetches the details of a night", async () => {
		await expect(getGameNightCall(4)).resolves.toBe("night");
		expect(axiosMock.get).toHaveBeenCalledWith("gamenight/4");
	});

	it("starts a game on a night", async () => {
		const command = { gameId: 9, playerIds: [1, 2] };

		await expect(startRunningGameCall(4, command)).resolves.toBe("created");
		expect(axiosMock.post).toHaveBeenCalledWith("gamenight/4/running-games", command);
	});

	it("finishes a running game", async () => {
		const command = { playerSessions: [], start: new Date(2026, 9, 16, 21, 0), minutes: 40 };

		await expect(finishRunningGameCall(12, command)).resolves.toBe("created");
		expect(axiosMock.post).toHaveBeenCalledWith("gamenight/running-games/12/finish", command);
	});

	it("discards a running game", async () => {
		await discardRunningGameCall(12);
		expect(axiosMock.delete).toHaveBeenCalledWith("gamenight/running-games/12");
	});

	it("ends and reopens a night", async () => {
		await endGameNightCall(4);
		await reopenGameNightCall(4);

		expect(axiosMock.post).toHaveBeenCalledWith("gamenight/4/end");
		expect(axiosMock.post).toHaveBeenCalledWith("gamenight/4/reopen");
	});
});
