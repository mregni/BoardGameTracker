import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IntegrationStatus } from "@/models/Settings/IntegrationStatus";
import { act, renderHook } from "@/test/test-utils";
import { useRefreshIntegrationStatus } from "./useIntegrationStatus";

const { toastError, getStatusesCall } = vi.hoisted(() => ({
	toastError: vi.fn(),
	getStatusesCall: vi.fn(),
}));

vi.mock("sonner", () => ({
	toast: { error: toastError },
}));

vi.mock("@/services/integrationService", () => ({
	getIntegrationStatusesCall: getStatusesCall,
	testIntegrationCall: vi.fn(),
}));

const status = (
	integration: IntegrationStatus["integration"],
	value: IntegrationStatus["status"],
	messageKey: string,
): IntegrationStatus => ({ integration, status: value, messageKey, checkedAt: "2026-10-16T09:30:00Z" });

const renderRefresh = () => {
	const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
	queryClient.setQueryData(["integrationStatus"], [status("bgg", "ok", "ok")]);
	const wrapper = ({ children }: { children: ReactNode }) => (
		<QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
	);
	const { result } = renderHook(() => useRefreshIntegrationStatus(), { wrapper });
	return { refresh: result.current, queryClient };
};

describe("useRefreshIntegrationStatus", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("re-tests after a save and stores the fresh status without a toast when everything works", async () => {
		const fresh = [status("bgg", "ok", "ok"), status("changeDetection", "notConfigured", "not-configured")];
		getStatusesCall.mockResolvedValue(fresh);
		const { refresh, queryClient } = renderRefresh();

		await act(() => refresh());

		expect(getStatusesCall).toHaveBeenCalledTimes(1);
		expect(queryClient.getQueryData(["integrationStatus"])).toEqual(fresh);
		expect(toastError).not.toHaveBeenCalled();
	});

	it("raises one toast per integration that fails after a save", async () => {
		getStatusesCall.mockResolvedValue([
			status("bgg", "unauthorized", "unauthorized"),
			status("changeDetection", "rateLimited", "rate-limited"),
		]);
		const { refresh } = renderRefresh();

		await act(() => refresh());

		expect(toastError).toHaveBeenCalledTimes(2);
		expect(toastError).toHaveBeenCalledWith("integrations.save-failed", {
			description: "integrations.reason.unauthorized",
		});
		expect(toastError).toHaveBeenCalledWith("integrations.save-failed", {
			description: "integrations.reason.rate-limited",
		});
	});

	it("reports when the status could not be fetched", async () => {
		getStatusesCall.mockRejectedValue(new Error("Network Error"));
		const { refresh } = renderRefresh();

		await act(() => refresh());

		expect(toastError).toHaveBeenCalledWith("integrations.test-failed");
	});
});
