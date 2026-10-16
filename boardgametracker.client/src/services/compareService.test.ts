import { beforeEach, describe, expect, it, vi } from "vitest";
import { getCompareCall, getDefaultPairCall } from "./compareService";

const get = vi.hoisted(() => vi.fn());

vi.mock("@/utils/axiosInstance", () => ({
	axiosInstance: { get },
}));

describe("compareService", () => {
	beforeEach(() => {
		get.mockReset();
	});

	it("asks for the comparison of two players", async () => {
		get.mockResolvedValue({ data: { totalSessionsTogether: 4 } });

		await expect(getCompareCall(3, 7)).resolves.toEqual({ totalSessionsTogether: 4 });
		expect(get).toHaveBeenCalledWith("compare/3/7");
	});

	it("returns the default pair", async () => {
		get.mockResolvedValue({ data: { playerOneId: 1, playerTwoId: 2, sessionCount: 5 } });

		await expect(getDefaultPairCall()).resolves.toEqual({ playerOneId: 1, playerTwoId: 2, sessionCount: 5 });
		expect(get).toHaveBeenCalledWith("compare/default-pair");
	});

	it("returns no pair when nobody played together", async () => {
		get.mockResolvedValue({ data: "" });

		await expect(getDefaultPairCall()).resolves.toBeNull();
	});
});
