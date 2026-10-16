import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IntegrationStatus } from "@/models/Settings/IntegrationStatus";

const axiosMock = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn() }));

vi.mock("../utils/axiosInstance", () => ({ axiosInstance: axiosMock }));

import { getIntegrationStatusesCall, testIntegrationCall } from "./integrationService";

const status: IntegrationStatus = {
	integration: "bgg",
	status: "ok",
	checkedAt: "2026-10-16T09:30:00Z",
	messageKey: "ok",
};

describe("integrationService", () => {
	beforeEach(() => {
		axiosMock.get.mockReset();
		axiosMock.post.mockReset();
	});

	it("reads the status of every integration", async () => {
		axiosMock.get.mockResolvedValue({ data: [status] });

		await expect(getIntegrationStatusesCall()).resolves.toEqual([status]);
		expect(axiosMock.get).toHaveBeenCalledWith("settings/integrations/status");
	});

	it("tests one integration", async () => {
		axiosMock.post.mockResolvedValue({ data: status });

		await expect(testIntegrationCall("changeDetection")).resolves.toEqual(status);
		expect(axiosMock.post).toHaveBeenCalledWith("settings/integrations/changeDetection/test");
	});
});
