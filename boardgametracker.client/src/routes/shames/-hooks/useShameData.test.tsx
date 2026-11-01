import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Shame, ShameGameNight } from "@/models";

const mocks = vi.hoisted(() => ({
	getShamesCall: vi.fn(),
	getShameStatisticsCall: vi.fn(),
	getSettingsCall: vi.fn(),
	addSuggestedGameCall: vi.fn(),
	removeSuggestedGameCall: vi.fn(),
	successToast: vi.fn(),
	errorToast: vi.fn(),
	invalidateShames: vi.fn(() => Promise.resolve()),
	invalidateGameNights: vi.fn(() => Promise.resolve()),
}));

vi.mock("@/services/gameService", async (importOriginal) => ({
	...(await importOriginal<typeof import("@/services/gameService")>()),
	getShamesCall: mocks.getShamesCall,
	getShameStatisticsCall: mocks.getShameStatisticsCall,
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({ queryKey: ["settings"], queryFn: mocks.getSettingsCall }),
}));

vi.mock("@/services/gameNightService", () => ({
	addSuggestedGameCall: mocks.addSuggestedGameCall,
	removeSuggestedGameCall: mocks.removeSuggestedGameCall,
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: mocks.successToast, errorToast: mocks.errorToast }),
}));

vi.mock("@/hooks/useQueryInvalidator", () => ({
	useQueryInvalidator: () => ({
		invalidateShames: mocks.invalidateShames,
		invalidateGameNights: mocks.invalidateGameNights,
	}),
}));

import { useShameData } from "./useShameData";

const shame = { id: 7, title: "Spirit Island" } as Shame;
const night: ShameGameNight = { id: 4, title: "Friday-ish Strategy Night", startDate: new Date(2026, 9, 23, 19) };

const wrapper = ({ children }: { children: ReactNode }) => {
	const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
	return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
};

describe("useShameData", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.getShamesCall.mockResolvedValue([shame]);
		mocks.getShameStatisticsCall.mockResolvedValue({ count: 1, nextGameNight: night });
		mocks.getSettingsCall.mockResolvedValue({ currency: "€", dateFormat: "dd-MM-yyyy" });
	});

	it("loads the shelf for the requested months", async () => {
		const { result } = renderHook(() => useShameData(12), { wrapper });

		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.shames).toEqual([shame]);
		expect(result.current.statistics).toEqual({ count: 1, nextGameNight: night });
		expect(mocks.getShamesCall).toHaveBeenCalledWith(12);
		expect(mocks.getShameStatisticsCall).toHaveBeenCalledWith(12);
	});

	it("plans the game, offers an undo and refreshes the shelf and the nights", async () => {
		mocks.addSuggestedGameCall.mockResolvedValue({});
		mocks.removeSuggestedGameCall.mockResolvedValue(undefined);
		const { result } = renderHook(() => useShameData(), { wrapper });
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		act(() => result.current.plan(shame, night));

		await waitFor(() => expect(mocks.successToast).toHaveBeenCalled());
		expect(mocks.addSuggestedGameCall).toHaveBeenCalledWith(4, 7);
		const [message, action, values] = mocks.successToast.mock.calls[0];
		expect(message).toBe("shames:plan.added");
		expect(action.label).toBe("shames:plan.undo");
		expect(values).toEqual({ game: "Spirit Island", night: "Friday-ish Strategy Night", date: "23-10-2026" });
		expect(mocks.invalidateShames).toHaveBeenCalled();
		expect(mocks.invalidateGameNights).toHaveBeenCalled();

		act(() => action.onClick());

		await waitFor(() => expect(mocks.removeSuggestedGameCall).toHaveBeenCalledWith(4, 7));
		await waitFor(() =>
			expect(mocks.successToast).toHaveBeenCalledWith("shames:plan.removed", undefined, { game: "Spirit Island" }),
		);
	});

	it("takes a planned game off its game night", async () => {
		mocks.removeSuggestedGameCall.mockResolvedValue(undefined);
		const { result } = renderHook(() => useShameData(), { wrapper });
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		act(() => result.current.unplan({ ...shame, plannedGameNightId: 9 }));

		await waitFor(() => expect(mocks.removeSuggestedGameCall).toHaveBeenCalledWith(9, 7));
		await waitFor(() =>
			expect(mocks.successToast).toHaveBeenCalledWith("shames:plan.removed", undefined, { game: "Spirit Island" }),
		);
		expect(mocks.invalidateShames).toHaveBeenCalled();
	});

	it("tells the user when planning fails", async () => {
		mocks.addSuggestedGameCall.mockRejectedValue(new Error("nope"));
		const { result } = renderHook(() => useShameData(), { wrapper });
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		act(() => result.current.plan(shame, night));

		await waitFor(() => expect(mocks.errorToast).toHaveBeenCalledWith("shames:plan.failed"));
		expect(mocks.successToast).not.toHaveBeenCalled();
	});

	it("tells the user when the undo fails", async () => {
		mocks.addSuggestedGameCall.mockResolvedValue({});
		mocks.removeSuggestedGameCall.mockRejectedValue(new Error("nope"));
		const { result } = renderHook(() => useShameData(), { wrapper });
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		act(() => result.current.plan(shame, night));
		await waitFor(() => expect(mocks.successToast).toHaveBeenCalled());
		act(() => mocks.successToast.mock.calls[0][1].onClick());

		await waitFor(() => expect(mocks.errorToast).toHaveBeenCalledWith("shames:plan.undo-failed"));
	});

	it("reports which game is being planned", async () => {
		let resolve: (value: unknown) => void = () => undefined;
		mocks.addSuggestedGameCall.mockReturnValue(
			new Promise((done) => {
				resolve = done;
			}),
		);
		const { result } = renderHook(() => useShameData(), { wrapper });
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		act(() => result.current.plan(shame, night));

		await waitFor(() => expect(result.current.planningId).toBe(7));
		await act(async () => resolve({}));
		await waitFor(() => expect(result.current.planningId).toBeUndefined());
	});
});
