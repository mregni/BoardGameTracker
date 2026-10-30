import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { renderHook, waitFor } from "@/test/test-utils";
import { useGameSessionsData } from "./useGameSessionsData";

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
	getGame: (id: number) => ({ queryKey: ["game", id], queryFn: () => Promise.resolve({ id, title: "Catan" }) }),
	getGameSessions: (id: number) => ({
		queryKey: ["game", id, "sessions"],
		queryFn: () =>
			new Promise((resolve) => {
				mocks.resolveSessions = resolve;
			}),
	}),
}));

vi.mock("@/services/queries/players", () => ({
	getPlayers: () => ({ queryKey: ["players"], queryFn: () => Promise.resolve([]) }),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({ queryKey: ["settings"], queryFn: () => Promise.resolve({}) }),
}));

describe("useGameSessionsData", () => {
	it("keeps loading until the sessions have arrived", async () => {
		const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
		const wrapper = ({ children }: { children: ReactNode }) => (
			<QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
		);
		const { result } = renderHook(() => useGameSessionsData({ gameId: 1 }), { wrapper });

		await waitFor(() => expect(result.current.game).toBeDefined());
		expect(result.current.isLoading).toBe(true);

		mocks.resolveSessions([]);
		await waitFor(() => expect(result.current.isLoading).toBe(false));
	});
});
