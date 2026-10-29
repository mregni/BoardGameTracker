import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { renderHook, waitFor } from "@/test/test-utils";
import { usePlayerSessionData } from "./usePlayerSessionData";

const mocks = vi.hoisted(() => ({
	resolveSessions: (_value: unknown[]) => {},
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ infoToast: vi.fn(), errorToast: vi.fn() }),
}));

vi.mock("@/services/queries/games", () => ({
	getGames: () => ({ queryKey: ["games"], queryFn: () => Promise.resolve([]) }),
}));

vi.mock("@/services/queries/players", () => ({
	getPlayer: (id: number) => ({ queryKey: ["player", id], queryFn: () => Promise.resolve({ id, name: "Jane" }) }),
	getPlayers: () => ({ queryKey: ["players"], queryFn: () => Promise.resolve([]) }),
	getPlayerSessions: (id: number) => ({
		queryKey: ["player", id, "sessions"],
		queryFn: () =>
			new Promise((resolve) => {
				mocks.resolveSessions = resolve;
			}),
	}),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({ queryKey: ["settings"], queryFn: () => Promise.resolve({}) }),
}));

describe("usePlayerSessionData", () => {
	it("keeps loading until the sessions have arrived", async () => {
		const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
		const wrapper = ({ children }: { children: ReactNode }) => (
			<QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
		);
		const { result } = renderHook(() => usePlayerSessionData({ playerId: 1 }), { wrapper });

		await waitFor(() => expect(result.current.player).toBeDefined());
		expect(result.current.isLoading).toBe(true);

		mocks.resolveSessions([]);
		await waitFor(() => expect(result.current.isLoading).toBe(false));
	});
});
