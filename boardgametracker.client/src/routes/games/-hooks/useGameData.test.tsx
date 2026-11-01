import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/routes/-hooks/useToasts", () => ({ useToasts: () => ({ successToast: vi.fn(), errorToast: vi.fn() }) }));
vi.mock("@/utils/errorUtils", () => ({ apiErrorMessage: (_: unknown, key: string) => key }));
vi.mock("@/hooks/useQueryInvalidator", () => ({ useQueryInvalidator: () => ({}) }));
vi.mock("@/routes/-hooks/useIntegrationSettings", () => ({ useIntegrationSettings: () => ({}) }));
vi.mock("@/services/gameService", async (importOriginal) => ({
	...(await importOriginal<object>()),
	getGameCall: vi.fn(),
	getGameStatisticsCall: vi.fn(),
	getGameSessionsCall: vi.fn(),
}));
vi.mock("@/services/settingsService", async (importOriginal) => ({
	...(await importOriginal<object>()),
	getSettingsCall: vi.fn(() => Promise.resolve({})),
}));

import { getGameCall, getGameSessionsCall, getGameStatisticsCall } from "@/services/gameService";
import { useGameData } from "./useGameData";

const wrapper = ({ children }: { children: ReactNode }) => (
	<QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
		{children}
	</QueryClientProvider>
);

const apiError = (status: number) => ({ kind: "client", status, message: "", url: undefined });

describe("useGameData load states", () => {
	beforeEach(() => {
		vi.mocked(getGameStatisticsCall).mockResolvedValue({} as never);
		vi.mocked(getGameSessionsCall).mockResolvedValue([] as never);
	});

	it("reports a missing game as not found", async () => {
		vi.mocked(getGameCall).mockRejectedValue(apiError(404));
		const { result } = renderHook(() => useGameData({ gameId: 7 }), { wrapper });

		await waitFor(() => expect(result.current.loadFailed).toBe(true));
		expect(result.current.isNotFound).toBe(true);
	});

	it("refetches only the queries that failed", async () => {
		vi.mocked(getGameCall).mockResolvedValue({ id: 7 } as never);
		vi.mocked(getGameStatisticsCall).mockRejectedValueOnce(apiError(500));
		const { result } = renderHook(() => useGameData({ gameId: 7 }), { wrapper });

		await waitFor(() => expect(result.current.detailsFailed).toBe(true));
		expect(result.current.isNotFound).toBe(false);
		const gameCalls = vi.mocked(getGameCall).mock.calls.length;

		act(() => result.current.retry());

		await waitFor(() => expect(result.current.detailsFailed).toBe(false));
		expect(vi.mocked(getGameCall).mock.calls).toHaveLength(gameCalls);
	});
});
