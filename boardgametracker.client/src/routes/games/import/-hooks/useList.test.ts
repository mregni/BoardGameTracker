import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { GameState, type ImportGame } from "@/models";

const defaultCollection = [
	{ bggId: 1, title: "Already Owned", state: "owned", imageUrl: "", lastModified: "2024-01-01T00:00:00Z" },
	{ bggId: 2, title: "Not Owned", state: "owned", imageUrl: "", lastModified: "2024-01-01T00:00:00Z" },
];
const defaultGames = [
	{ bggId: 1, buyingPrice: 0, additionDate: "2024-01-01T00:00:00Z", hasScoring: true, expansions: [] },
];

const mocks = vi.hoisted(() => ({
	collection: [] as unknown[],
	games: [] as unknown[],
	invalidateGameCreated: vi.fn(),
	successToast: vi.fn(),
	errorToast: vi.fn(),
	navigate: vi.fn(),
	importGamesCall: vi.fn((_games: unknown[]) => Promise.resolve(true)),
}));

vi.mock("@/services/queries/games", () => ({
	getBggCollection: (username: string) => ({
		queryKey: ["bgg-collection-mock", username],
		queryFn: () => Promise.resolve(mocks.collection),
	}),
	getGames: () => ({
		queryKey: ["games-mock"],
		queryFn: () => Promise.resolve(mocks.games),
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
		invalidateGameCreated: mocks.invalidateGameCreated,
	}),
}));

vi.mock("@tanstack/react-router", () => ({
	useNavigate: () => mocks.navigate,
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: mocks.successToast, errorToast: mocks.errorToast }),
}));

vi.mock("@/services/gameService", () => ({
	importGamesCall: mocks.importGamesCall,
}));

import { getBggCollection } from "@/services/queries/games";
import { IMPORT_BATCH_SIZE, importBatches, useList } from "./useList";

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
	baseGameBggId: null,
	baseGameTitle: null,
});

const bggItem = (
	bggId: number,
	title: string,
	baseGameBggId: number | null = null,
	baseGameTitle: string | null = null,
) => ({
	bggId,
	title,
	state: "owned",
	imageUrl: "",
	lastModified: "2024-01-01T00:00:00Z",
	baseGameBggId,
	baseGameTitle,
});

const createWrapper = () => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false, gcTime: 0 } },
	});
	return ({ children }: { children: React.ReactNode }) =>
		React.createElement(QueryClientProvider, { client: queryClient }, children);
};

const renderUseList = async () => {
	const rendered = renderHook(() => useList({ collectionQuery: getBggCollection("tester") }), {
		wrapper: createWrapper(),
	});
	await waitFor(() => expect(rendered.result.current.processingGames).toBe(false));
	return rendered;
};

describe("useList", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.collection = defaultCollection;
		mocks.games = defaultGames;
		mocks.importGamesCall.mockImplementation(() => Promise.resolve(true));
	});

	it("lists expansions under their base game and adds a base game that is missing", async () => {
		mocks.collection = [
			bggItem(21, "7 Wonders: Leaders", 20, "7 Wonders"),
			bggItem(11, "Root: The Riverfolk Expansion", 10, "Root"),
			bggItem(10, "Root"),
		];
		mocks.games = [];
		const { result } = await renderUseList();

		expect(result.current.games.map((g) => [g.bggId, g.title])).toEqual([
			[20, "7 Wonders"],
			[21, "7 Wonders: Leaders"],
			[10, "Root"],
			[11, "Root: The Riverfolk Expansion"],
		]);
		expect(result.current.games[0]).toMatchObject({ addedForExpansion: true, checked: false, baseGameBggId: null });
	});

	it("checks and locks the base game while one of its expansions is checked", async () => {
		mocks.collection = [bggItem(10, "Root"), bggItem(11, "Root: The Riverfolk Expansion", 10, "Root")];
		mocks.games = [];
		const { result } = await renderUseList();

		act(() => result.current.updateGame(11, { checked: true }));
		expect(result.current.games.find((g) => g.bggId === 10)).toMatchObject({ checked: true, lockedByExpansion: true });

		act(() => result.current.updateGame(11, { checked: false }));
		expect(result.current.games.find((g) => g.bggId === 10)).toMatchObject({
			checked: false,
			lockedByExpansion: false,
		});
	});

	it("keeps a base game from the collection as the header of its new expansions", async () => {
		mocks.collection = [bggItem(11, "Root: The Riverfolk Expansion", 10, "Root")];
		mocks.games = [
			{ bggId: 10, title: "Root", image: "/images/cover/root.webp", state: "owned", hasScoring: true, expansions: [] },
		];
		const { result } = await renderUseList();

		expect(result.current.games.map((g) => [g.bggId, g.inCollection])).toEqual([
			[10, true],
			[11, false],
		]);
		expect(result.current.games[0]).toMatchObject({ title: "Root", imageUrl: "/images/cover/root.webp" });

		act(() => result.current.updateGame(11, { checked: true }));
		expect(result.current.games[0]).toMatchObject({ checked: false, lockedByExpansion: false });
	});

	it("treats an expansion that is already imported as part of the collection", async () => {
		mocks.collection = [bggItem(10, "Root"), bggItem(11, "Root: The Riverfolk Expansion", 10, "Root")];
		mocks.games = [
			{
				bggId: 10,
				title: "Root",
				hasScoring: true,
				expansions: [{ bggId: 11, buyingPrice: 35, additionDate: "2024-02-01T00:00:00Z" }],
			},
		];
		const { result } = await renderUseList();

		expect(result.current.games).toEqual([]);
		act(() => result.current.setFilterCollected(false));
		expect(result.current.games.find((g) => g.bggId === 11)).toMatchObject({ inCollection: true, price: 35 });
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

	it("keeps a base game and its expansions in one batch, base game first", () => {
		const expansion = (bggId: number) => ({ ...importGame(bggId), baseGameBggId: 1 });
		const selected = [importGame(7), expansion(2), expansion(3), importGame(8), importGame(9), importGame(1)];

		expect(importBatches(selected, 3).map((batch) => batch.map((game) => game.bggId))).toEqual([
			[7],
			[1, 2, 3],
			[8, 9],
		]);
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
		expect(mocks.invalidateGameCreated).toHaveBeenCalled();
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
		await waitFor(() => expect(mocks.invalidateGameCreated).toHaveBeenCalled());
		expect(mocks.successToast).not.toHaveBeenCalled();
	});

	it("offers the integration settings in the toast when BGG rejects the import", async () => {
		mocks.importGamesCall.mockImplementationOnce(() =>
			Promise.reject({ kind: "client", status: 401, message: "x", url: "game/bgg/import" }),
		);
		const { result } = await renderUseList();

		act(() => result.current.startImport([importGame(100)]));

		await waitFor(() => expect(mocks.errorToast).toHaveBeenCalled());
		const [message, action] = mocks.errorToast.mock.calls[0];
		expect(message).toBe("games:import.failed-unauthorized");
		expect(action.label).toBe("common:open-integration-settings");
		action.onClick();
		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/settings", search: { tab: "integrations" } });
	});

	it("shows a plain error toast for a generic import failure", async () => {
		mocks.importGamesCall.mockImplementationOnce(() => Promise.reject(new Error("Network Error")));
		const { result } = await renderUseList();

		act(() => result.current.startImport([importGame(100)]));

		await waitFor(() => expect(mocks.errorToast).toHaveBeenCalledWith("games:import.failed-unknown", undefined));
	});
});
