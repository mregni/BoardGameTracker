import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { type GameNight, GameNightRsvpState, QUERY_KEYS, type UpdateGameNight } from "@/models";
import { act, renderHook, waitFor } from "@/test/test-utils";

const { updateRsvpCall, updateCall, invalidateGameNights, successToast, errorToast } = vi.hoisted(() => ({
	updateRsvpCall: vi.fn(),
	updateCall: vi.fn(),
	invalidateGameNights: vi.fn(),
	successToast: vi.fn(),
	errorToast: vi.fn(),
}));

vi.mock("@/services/gameNightService", () => ({
	getGameNightsCall: vi.fn(() => new Promise(() => {})),
	createGameNightCall: vi.fn(),
	updateGameNightCall: updateCall,
	deleteGameNightCall: vi.fn(),
	updateGameNightRsvpCall: updateRsvpCall,
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({ queryKey: ["settings"], queryFn: () => Promise.resolve({}) }),
}));

vi.mock("@/services/queries/players", () => ({
	getPlayers: () => ({ queryKey: ["players"], queryFn: () => Promise.resolve([]) }),
}));

vi.mock("@/services/queries/games", () => ({
	getGames: () => ({ queryKey: ["games"], queryFn: () => Promise.resolve([]) }),
}));

vi.mock("@/services/queries/locations", () => ({
	getLocations: () => ({ queryKey: ["locations"], queryFn: () => Promise.resolve([]) }),
}));

vi.mock("@/hooks/useQueryInvalidator", () => ({
	useQueryInvalidator: () => ({ invalidateGameNights }),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast, errorToast }),
}));

import { useGameNightData } from "./useGameNightData";

const gameNights = [
	{
		id: 4,
		invitedPlayers: [
			{ id: 11, playerId: 1, gameNightId: 4, state: GameNightRsvpState.Accepted },
			{ id: 12, playerId: 7, gameNightId: 4, state: GameNightRsvpState.Pending },
		],
	},
] as unknown as GameNight[];

const setup = () => {
	const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
	queryClient.setQueryData([QUERY_KEYS.gameNights], gameNights);
	const wrapper = ({ children }: { children: React.ReactNode }) => (
		<QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
	);
	const { result } = renderHook(() => useGameNightData(), { wrapper });
	const stateOf = (rsvpId: number) =>
		queryClient
			.getQueryData<GameNight[]>([QUERY_KEYS.gameNights])
			?.flatMap((gameNight) => gameNight.invitedPlayers)
			.find((rsvp) => rsvp.id === rsvpId)?.state;
	return { result, stateOf };
};

const update = { id: 12, gameNightId: 4, playerId: 7, state: GameNightRsvpState.Declined };

describe("useGameNightData rsvp updates", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("applies the new state before the server answers", async () => {
		let resolve: (value: unknown) => void = () => {};
		updateRsvpCall.mockReturnValue(new Promise((r) => (resolve = r)));
		const { result, stateOf } = setup();

		act(() => {
			result.current.updateRsvp(update);
		});

		await waitFor(() => expect(stateOf(12)).toBe(GameNightRsvpState.Declined));
		expect(stateOf(11)).toBe(GameNightRsvpState.Accepted);

		await act(async () => resolve({}));
		await waitFor(() => expect(invalidateGameNights).toHaveBeenCalled());
		expect(successToast).toHaveBeenCalledWith("game-nights:notifications.rsvp-updated");
	});

	it("rolls back when the update fails", async () => {
		updateRsvpCall.mockRejectedValue(new Error("400"));
		const { result, stateOf } = setup();

		act(() => {
			result.current.updateRsvp(update);
		});

		await waitFor(() => expect(errorToast).toHaveBeenCalled());
		expect(stateOf(12)).toBe(GameNightRsvpState.Pending);
		expect(errorToast).toHaveBeenCalledWith("game-nights:notifications.rsvp-update-failed");
	});
});

const gameNightUpdate = {
	id: 4,
	title: "Strategy night",
	notes: "",
	startDate: new Date(2026, 9, 11, 19, 30),
	hostId: 1,
	locationId: 3,
	suggestedGameIds: [],
	invitedPlayerIds: [7],
} as UpdateGameNight;

describe("useGameNightData game night updates", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("confirms an edit with a success toast", async () => {
		updateCall.mockResolvedValue({});
		const { result } = setup();

		await act(async () => {
			await result.current.updateGameNight(gameNightUpdate);
		});

		expect(updateCall.mock.calls[0][0]).toEqual(gameNightUpdate);
		expect(successToast).toHaveBeenCalledWith("game-nights:notifications.updated");
		expect(invalidateGameNights).toHaveBeenCalled();
	});

	it("invites or removes players from the RSVP dialog without a success toast", async () => {
		updateCall.mockResolvedValue({});
		const { result } = setup();

		act(() => {
			result.current.updateInvitedPlayers(gameNightUpdate);
		});

		await waitFor(() => expect(invalidateGameNights).toHaveBeenCalled());
		expect(updateCall.mock.calls[0][0]).toEqual(gameNightUpdate);
		expect(successToast).not.toHaveBeenCalled();
		expect(invalidateGameNights).toHaveBeenCalled();
	});

	it("still reports a failed invite or removal", async () => {
		updateCall.mockRejectedValue(new Error("400"));
		const { result } = setup();

		act(() => {
			result.current.updateInvitedPlayers(gameNightUpdate);
		});

		await waitFor(() => expect(errorToast).toHaveBeenCalled());
		expect(successToast).not.toHaveBeenCalled();
		expect(errorToast).toHaveBeenCalledWith("game-nights:notifications.update-failed");
	});
});
