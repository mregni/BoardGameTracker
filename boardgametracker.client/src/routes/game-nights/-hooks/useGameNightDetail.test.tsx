import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { act, renderHook, waitFor } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	getGameNightCall: vi.fn(),
	startRunningGameCall: vi.fn(),
	finishRunningGameCall: vi.fn(),
	discardRunningGameCall: vi.fn(),
	invalidateGameNights: vi.fn(),
	invalidateSession: vi.fn(),
	successToast: vi.fn(),
	errorToast: vi.fn(),
}));

vi.mock("@/services/gameNightService", () => ({
	getGameNightsCall: vi.fn(),
	getGameNightByLinkCall: vi.fn(),
	getGameNightCall: (id: number) => mocks.getGameNightCall(id),
	startRunningGameCall: (id: number, command: unknown) => mocks.startRunningGameCall(id, command),
	finishRunningGameCall: (id: number, command: unknown) => mocks.finishRunningGameCall(id, command),
	discardRunningGameCall: (id: number) => mocks.discardRunningGameCall(id),
}));

vi.mock("@/hooks/useQueryInvalidator", () => ({
	useQueryInvalidator: () => ({
		invalidateGameNights: mocks.invalidateGameNights,
		invalidateSession: mocks.invalidateSession,
	}),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: mocks.successToast, errorToast: mocks.errorToast }),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

import { useGameNightDetail } from "./useGameNightDetail";

const setup = () => {
	const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
	const wrapper = ({ children }: { children: React.ReactNode }) => (
		<QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
	);
	return renderHook(() => useGameNightDetail(4), { wrapper }).result;
};

describe("useGameNightDetail", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.getGameNightCall.mockResolvedValue({ gameNight: { id: 4 }, sessions: [], runningGames: [] });
	});

	it("loads the night", async () => {
		const result = setup();

		await waitFor(() =>
			expect(result.current.detail).toEqual({ gameNight: { id: 4 }, sessions: [], runningGames: [] }),
		);
		expect(mocks.getGameNightCall).toHaveBeenCalledWith(4);
	});

	it("starts a game and refreshes the night", async () => {
		mocks.startRunningGameCall.mockResolvedValue({ id: 12 });
		const result = setup();

		await act(() => result.current.startGame({ gameId: 9, playerIds: [1] }));

		expect(mocks.startRunningGameCall).toHaveBeenCalledWith(4, { gameId: 9, playerIds: [1] });
		expect(mocks.successToast).toHaveBeenCalledWith("game-nights:play.notifications.started");
		expect(mocks.invalidateGameNights).toHaveBeenCalled();
	});

	it("tells when starting a game fails", async () => {
		mocks.startRunningGameCall.mockRejectedValue(new Error("nope"));
		const result = setup();

		await act(() => result.current.startGame({ gameId: 9, playerIds: [1] }).catch(() => undefined));

		expect(mocks.errorToast).toHaveBeenCalledWith("game-nights:play.notifications.start-failed");
	});

	it("finishes a game and refreshes the night and the session", async () => {
		mocks.finishRunningGameCall.mockResolvedValue({ id: 40, gameId: 9, playerSessions: [{ playerId: 1 }] });
		const result = setup();
		const command = { playerSessions: [], start: null, minutes: null };

		await act(() => result.current.finishGame({ id: 12, command }));

		expect(mocks.finishRunningGameCall).toHaveBeenCalledWith(12, command);
		expect(mocks.successToast).toHaveBeenCalledWith("player-session:new.notifications.created");
		expect(mocks.invalidateSession).toHaveBeenCalledWith(40, 9, [1]);
		expect(mocks.invalidateGameNights).toHaveBeenCalled();
	});

	it("tells when finishing a game fails", async () => {
		mocks.finishRunningGameCall.mockRejectedValue(new Error("nope"));
		const result = setup();

		await act(() =>
			result.current
				.finishGame({ id: 12, command: { playerSessions: [], start: null, minutes: null } })
				.catch(() => undefined),
		);

		expect(mocks.errorToast).toHaveBeenCalledWith("player-session:new.notifications.create-failed");
	});

	it("discards a running game", async () => {
		mocks.discardRunningGameCall.mockResolvedValue(undefined);
		const result = setup();

		await act(() => result.current.discardGame(12));

		expect(mocks.discardRunningGameCall).toHaveBeenCalledWith(12);
		expect(mocks.successToast).toHaveBeenCalledWith("game-nights:play.notifications.discarded");
		expect(mocks.invalidateGameNights).toHaveBeenCalled();
	});

	it("tells when discarding fails", async () => {
		mocks.discardRunningGameCall.mockRejectedValue(new Error("nope"));
		const result = setup();

		await act(() => result.current.discardGame(12).catch(() => undefined));

		expect(mocks.errorToast).toHaveBeenCalledWith("game-nights:play.notifications.discard-failed");
	});
});
