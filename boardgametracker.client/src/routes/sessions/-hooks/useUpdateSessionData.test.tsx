import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { act, renderHook, waitFor } from "@/test/test-utils";
import { useUpdateSessionData } from "./useUpdateSessionData";

const mocks = vi.hoisted(() => ({
	invalidateSession: vi.fn(),
	invalidateGame: vi.fn(),
	updateSessionCall: vi.fn(),
}));

vi.mock("@/hooks/useQueryInvalidator", () => ({
	useQueryInvalidator: () => ({
		invalidateSession: mocks.invalidateSession,
		invalidateGame: mocks.invalidateGame,
	}),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: vi.fn(), errorToast: vi.fn() }),
}));

vi.mock("@/services/sessionService", () => ({
	updateSessionCall: (session: unknown) => mocks.updateSessionCall(session),
}));

vi.mock("@/services/queries/sessions", () => ({
	getSession: (id: number) => ({
		queryKey: ["sessions", id],
		queryFn: () => Promise.resolve({ id, gameId: 1, playerSessions: [{ playerId: 7 }] }),
	}),
}));

vi.mock("@/services/queries/games", () => ({
	getGame: (id: number) => ({ queryKey: ["game", id], queryFn: () => Promise.resolve({ id }) }),
}));

const renderUpdate = async () => {
	const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
	const wrapper = ({ children }: { children: ReactNode }) => (
		<QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
	);
	const hook = renderHook(() => useUpdateSessionData({ sessionId: 5 }), { wrapper });
	await waitFor(() => expect(hook.result.current.session).toBeDefined());
	return hook.result;
};

describe("useUpdateSessionData", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("refreshes the previous game when the session moved to another game", async () => {
		mocks.updateSessionCall.mockResolvedValue({ id: 5, gameId: 2, playerSessions: [{ playerId: 8 }] });
		const result = await renderUpdate();

		await act(() => result.current.updateSession({ id: 5 } as never));

		expect(mocks.invalidateSession).toHaveBeenCalledWith(5, 2, [7, 8]);
		expect(mocks.invalidateGame).toHaveBeenCalledWith(1);
	});

	it("does not refresh another game when the game stayed the same", async () => {
		mocks.updateSessionCall.mockResolvedValue({ id: 5, gameId: 1, playerSessions: [{ playerId: 7 }] });
		const result = await renderUpdate();

		await act(() => result.current.updateSession({ id: 5 } as never));

		expect(mocks.invalidateSession).toHaveBeenCalledWith(5, 1, [7]);
		expect(mocks.invalidateGame).not.toHaveBeenCalled();
	});
});
