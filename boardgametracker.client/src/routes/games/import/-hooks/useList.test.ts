import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { GameState, type ImportGame } from "@/models";

const mocks = vi.hoisted(() => ({
	invalidateGames: vi.fn(),
	invalidateCounts: vi.fn(),
	invalidateDashboard: vi.fn(),
	successToast: vi.fn(),
	errorToast: vi.fn(),
	importGamesCall: vi.fn((_games: unknown[]) => Promise.resolve(true)),
}));

vi.mock("@/services/queries/games", () => ({
	getBggCollection: (username: string) => ({
		queryKey: ["bgg-collection-mock", username],
		queryFn: () =>
			Promise.resolve([
				{
					bggId: 1,
					title: "Already Owned",
					state: "owned",
					imageUrl: "",
					lastModified: "2024-01-01T00:00:00Z",
				},
				{
					bggId: 2,
					title: "Not Owned",
					state: "owned",
					imageUrl: "",
					lastModified: "2024-01-01T00:00:00Z",
				},
			]),
	}),
	getGames: () => ({
		queryKey: ["games-mock"],
		queryFn: () =>
			Promise.resolve([
				{
					bggId: 1,
					buyingPrice: 0,
					additionDate: "2024-01-01T00:00:00Z",
					hasScoring: true,
				},
			]),
	}),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings-mock"],
		queryFn: () => Promise.resolve({ currency: "€" }),
	}),
}));

vi.mock("@/hooks/useQueryInvalidator", () => ({
	useQueryInvalidator: () => ({
		invalidateGames: mocks.invalidateGames,
		invalidateCounts: mocks.invalidateCounts,
		invalidateDashboard: mocks.invalidateDashboard,
	}),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: mocks.successToast, errorToast: mocks.errorToast }),
}));

vi.mock("@/services/gameService", () => ({
	importGamesCall: mocks.importGamesCall,
}));

import { IMPORT_BATCH_SIZE, useList } from "./useList";

const importGame = (bggId: number): ImportGame => ({
	title: `Game ${bggId}`,
	bggId,
	state: GameState.Owned,
	imageUrl: "",
	checked: true,
	inCollection: false,
	hasScoring: true,
	price: 0,
	addedDate: new Date("2024-01-01"),
	lastModified: new Date("2024-01-01"),
});

const createWrapper = () => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false, gcTime: 0 } },
	});
	return ({ children }: { children: React.ReactNode }) =>
		React.createElement(QueryClientProvider, { client: queryClient }, children);
};

const renderUseList = async () => {
	const rendered = renderHook(() => useList({ username: "tester" }), {
		wrapper: createWrapper(),
	});
	await waitFor(() => expect(rendered.result.current.processingGames).toBe(false));
	return rendered;
};

describe("useList", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.importGamesCall.mockImplementation(() => Promise.resolve(true));
	});

	it("hides games already in the collection by default", async () => {
		const { result } = await renderUseList();

		expect(result.current.games.map((g) => g.bggId)).toEqual([2]);
		expect(result.current.inCollectionCount).toBe(1);
		expect(result.current.totalCount).toBe(2);
	});

	it("never keeps an in-collection game selected, even after it was checked", async () => {
		const { result } = await renderUseList();

		act(() => result.current.setFilterCollected(false));
		act(() => {
			result.current.updateGame(1, { checked: true });
			result.current.updateGame(2, { checked: true });
		});

		const owned = result.current.games.find((g) => g.bggId === 1);
		const notOwned = result.current.games.find((g) => g.bggId === 2);

		expect(owned?.inCollection).toBe(true);
		expect(owned?.checked).toBe(false);

		expect(notOwned?.inCollection).toBe(false);
		expect(notOwned?.checked).toBe(true);

		expect(result.current.games.filter((g) => g.checked)).toHaveLength(1);
	});

	it("bulk selection (select all) still respects the in-collection invariant", async () => {
		const { result } = await renderUseList();

		act(() => result.current.setFilterCollected(false));
		act(() =>
			result.current.setSelection([
				{ bggId: 1, checked: true },
				{ bggId: 2, checked: true },
			]),
		);

		expect(result.current.games.find((g) => g.bggId === 1)?.checked).toBe(false);
		expect(result.current.games.find((g) => g.bggId === 2)?.checked).toBe(true);
	});

	it("imports the selection in small sequential batches and reports progress", async () => {
		const { result } = await renderUseList();
		const selected = Array.from({ length: 12 }, (_, index) => importGame(100 + index));

		act(() => result.current.startImport(selected));

		await waitFor(() => expect(mocks.successToast).toHaveBeenCalledWith("games:import.success"));
		expect(mocks.importGamesCall.mock.calls.map(([batch]) => batch.length)).toEqual([
			IMPORT_BATCH_SIZE,
			IMPORT_BATCH_SIZE,
			12 - 2 * IMPORT_BATCH_SIZE,
		]);
		expect(mocks.importGamesCall.mock.calls.flatMap(([batch]) => batch)).toEqual(selected);
		expect(result.current.importProgress).toEqual({ done: 12, total: 12 });
		expect(mocks.invalidateGames).toHaveBeenCalled();
	});

	it("stops at a failed batch but still refreshes the games the earlier batches imported", async () => {
		mocks.importGamesCall
			.mockImplementationOnce(() => Promise.resolve(true))
			.mockImplementationOnce(() => Promise.reject(new Error("boom")));
		const { result } = await renderUseList();
		const selected = Array.from({ length: 12 }, (_, index) => importGame(100 + index));

		act(() => result.current.startImport(selected));

		await waitFor(() => expect(mocks.errorToast).toHaveBeenCalled());
		expect(mocks.importGamesCall).toHaveBeenCalledTimes(2);
		expect(result.current.importProgress).toEqual({ done: IMPORT_BATCH_SIZE, total: 12 });
		await waitFor(() => expect(mocks.invalidateGames).toHaveBeenCalled());
		expect(mocks.invalidateCounts).toHaveBeenCalled();
		expect(mocks.invalidateDashboard).toHaveBeenCalled();
		expect(mocks.successToast).not.toHaveBeenCalled();
	});
});
