import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import React from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { GameManual } from "@/models";

const { successToast, errorToast, invalidateGame } = vi.hoisted(() => ({
	successToast: vi.fn(),
	errorToast: vi.fn(),
	invalidateGame: vi.fn(() => Promise.resolve()),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast, errorToast }),
}));

vi.mock("@/hooks/useQueryInvalidator", () => ({
	useQueryInvalidator: () => ({ invalidateGame }),
}));

vi.mock("@/services/manualService", () => ({
	getGameManualsCall: vi.fn(),
	getGameNightManualsCall: vi.fn(),
	uploadManualsCall: vi.fn(),
	deleteManualCall: vi.fn(),
	reindexManualCall: vi.fn(),
	downloadManualCall: vi.fn(),
}));

import {
	deleteManualCall,
	downloadManualCall,
	getGameManualsCall,
	reindexManualCall,
	uploadManualsCall,
} from "@/services/manualService";
import { useGameManuals } from "./useGameManuals";

const getGameManualsCallMock = vi.mocked(getGameManualsCall);
const uploadManualsCallMock = vi.mocked(uploadManualsCall);
const deleteManualCallMock = vi.mocked(deleteManualCall);
const reindexManualCallMock = vi.mocked(reindexManualCall);
const downloadManualCallMock = vi.mocked(downloadManualCall);

const manual = (overrides: Partial<GameManual> = {}): GameManual => ({
	id: 1,
	gameId: 10,
	title: "Rulebook",
	fileSizeBytes: 1024,
	uploadDate: new Date("2024-01-01T00:00:00Z"),
	contentType: "application/pdf",
	indexStatus: "indexed",
	indexedChunkCount: 3,
	indexError: null,
	indexedDate: null,
	...overrides,
});

const createWrapper = () => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false, gcTime: 0 }, mutations: { retry: false } },
	});
	return ({ children }: { children: React.ReactNode }) =>
		React.createElement(QueryClientProvider, { client: queryClient }, children);
};

const renderUseGameManuals = async (gameId = 10, pollWhileIndexing = false) => {
	const rendered = renderHook(() => useGameManuals(gameId, pollWhileIndexing), {
		wrapper: createWrapper(),
	});
	await waitFor(() => expect(rendered.result.current.isLoading).toBe(false));
	return rendered;
};

const advanceTimers = async (ms: number) => {
	await act(async () => {
		await vi.advanceTimersByTimeAsync(ms);
	});
};

afterEach(() => {
	vi.useRealTimers();
	vi.clearAllMocks();
});

describe("useGameManuals", () => {
	it("returns the manuals for the game once loaded", async () => {
		const manuals = [manual(), manual({ id: 2, title: "Expansion" })];
		getGameManualsCallMock.mockResolvedValue(manuals);

		const { result } = await renderUseGameManuals();

		expect(result.current.manuals).toEqual(manuals);
		expect(getGameManualsCallMock).toHaveBeenCalledWith(10, undefined);
	});

	it("polls while a manual is pending and polling is enabled", async () => {
		vi.useFakeTimers();
		getGameManualsCallMock.mockResolvedValue([manual({ indexStatus: "pending" })]);

		renderHook(() => useGameManuals(10, true), { wrapper: createWrapper() });
		await advanceTimers(0);
		expect(getGameManualsCallMock).toHaveBeenCalledTimes(1);

		await advanceTimers(3000);
		expect(getGameManualsCallMock).toHaveBeenCalledTimes(2);

		await advanceTimers(3000);
		expect(getGameManualsCallMock).toHaveBeenCalledTimes(3);
	});

	it("stops polling once indexing completes", async () => {
		vi.useFakeTimers();
		getGameManualsCallMock
			.mockResolvedValueOnce([manual({ indexStatus: "indexing" })])
			.mockResolvedValue([manual({ indexStatus: "indexed" })]);

		renderHook(() => useGameManuals(10, true), { wrapper: createWrapper() });
		await advanceTimers(0);
		await advanceTimers(3000);
		expect(getGameManualsCallMock).toHaveBeenCalledTimes(2);

		await advanceTimers(9000);
		expect(getGameManualsCallMock).toHaveBeenCalledTimes(2);
	});

	it("does not poll while indexing when polling is disabled", async () => {
		vi.useFakeTimers();
		getGameManualsCallMock.mockResolvedValue([manual({ indexStatus: "indexing" })]);

		renderHook(() => useGameManuals(10, false), { wrapper: createWrapper() });
		await advanceTimers(0);
		await advanceTimers(9000);

		expect(getGameManualsCallMock).toHaveBeenCalledTimes(1);
	});

	it("stops polling after the maximum number of polls even while still indexing", async () => {
		vi.useFakeTimers();
		getGameManualsCallMock.mockResolvedValue([manual({ indexStatus: "indexing" })]);

		renderHook(() => useGameManuals(10, true), { wrapper: createWrapper() });
		await advanceTimers(0);
		for (let i = 0; i < 70; i++) {
			await advanceTimers(3000);
		}
		const callsAfterCap = getGameManualsCallMock.mock.calls.length;
		expect(callsAfterCap).toBeGreaterThan(1);
		expect(callsAfterCap).toBeLessThanOrEqual(61);

		await advanceTimers(30000);
		expect(getGameManualsCallMock).toHaveBeenCalledTimes(callsAfterCap);
	});

	it("uploads files and refreshes the game with a success toast", async () => {
		getGameManualsCallMock.mockResolvedValue([]);
		uploadManualsCallMock.mockResolvedValue([manual()]);
		const files = [new File(["content"], "rules.pdf", { type: "application/pdf" })];

		const { result } = await renderUseGameManuals();
		act(() => result.current.uploadManuals(files));

		await waitFor(() => expect(successToast).toHaveBeenCalledWith("game:manuals.upload-success"));
		expect(uploadManualsCallMock).toHaveBeenCalledWith(10, files);
		expect(invalidateGame).toHaveBeenCalledWith(10);
		expect(errorToast).not.toHaveBeenCalled();
	});

	it("shows an error toast and does not refresh when upload fails", async () => {
		getGameManualsCallMock.mockResolvedValue([]);
		uploadManualsCallMock.mockRejectedValue(new Error("upload failed"));

		const { result } = await renderUseGameManuals();
		act(() => result.current.uploadManuals([new File(["content"], "rules.pdf")]));

		await waitFor(() => expect(errorToast).toHaveBeenCalledWith("game:manuals.upload-failed"));
		expect(invalidateGame).not.toHaveBeenCalled();
		expect(successToast).not.toHaveBeenCalled();
	});

	it("exposes isUploading while an upload is in flight", async () => {
		getGameManualsCallMock.mockResolvedValue([]);
		uploadManualsCallMock.mockImplementation(() => new Promise(() => {}));

		const { result } = await renderUseGameManuals();
		expect(result.current.isUploading).toBe(false);

		act(() => result.current.uploadManuals([new File(["content"], "rules.pdf")]));

		await waitFor(() => expect(result.current.isUploading).toBe(true));
	});

	it("deletes a manual and refreshes the game with a success toast", async () => {
		getGameManualsCallMock.mockResolvedValue([manual()]);
		deleteManualCallMock.mockResolvedValue(undefined);

		const { result } = await renderUseGameManuals();
		act(() => result.current.deleteManual(1));

		await waitFor(() => expect(successToast).toHaveBeenCalledWith("game:manuals.delete-success"));
		expect(deleteManualCallMock.mock.calls[0][0]).toBe(1);
		expect(invalidateGame).toHaveBeenCalledWith(10);
	});

	it("shows an error toast and does not refresh when delete fails", async () => {
		getGameManualsCallMock.mockResolvedValue([manual()]);
		deleteManualCallMock.mockRejectedValue(new Error("delete failed"));

		const { result } = await renderUseGameManuals();
		act(() => result.current.deleteManual(1));

		await waitFor(() => expect(errorToast).toHaveBeenCalledWith("game:manuals.delete-failed"));
		expect(invalidateGame).not.toHaveBeenCalled();
		expect(successToast).not.toHaveBeenCalled();
	});

	it("reindexes a manual, refetches the manuals and shows a success toast", async () => {
		getGameManualsCallMock.mockResolvedValue([manual({ indexStatus: "failed" })]);
		reindexManualCallMock.mockResolvedValue(undefined);

		const { result } = await renderUseGameManuals();
		expect(getGameManualsCallMock).toHaveBeenCalledTimes(1);

		act(() => result.current.reindexManual(1));

		await waitFor(() => expect(successToast).toHaveBeenCalledWith("game:manuals.reindex-success"));
		expect(reindexManualCallMock.mock.calls[0][0]).toBe(1);
		await waitFor(() => expect(getGameManualsCallMock).toHaveBeenCalledTimes(2));
	});

	it("shows an error toast and does not refetch when reindex fails", async () => {
		getGameManualsCallMock.mockResolvedValue([manual({ indexStatus: "failed" })]);
		reindexManualCallMock.mockRejectedValue(new Error("reindex failed"));

		const { result } = await renderUseGameManuals();
		act(() => result.current.reindexManual(1));

		await waitFor(() => expect(errorToast).toHaveBeenCalledWith("game:manuals.reindex-failed"));
		expect(getGameManualsCallMock).toHaveBeenCalledTimes(1);
		expect(successToast).not.toHaveBeenCalled();
	});

	it("downloads a manual with its title", async () => {
		getGameManualsCallMock.mockResolvedValue([manual()]);
		downloadManualCallMock.mockResolvedValue(undefined);

		const { result } = await renderUseGameManuals();
		await result.current.downloadManual(1, "Rulebook");

		expect(downloadManualCallMock).toHaveBeenCalledWith(1, "Rulebook");
		expect(errorToast).not.toHaveBeenCalled();
	});

	it("shows an error toast when the download fails", async () => {
		getGameManualsCallMock.mockResolvedValue([manual()]);
		downloadManualCallMock.mockRejectedValue(new Error("download failed"));

		const { result } = await renderUseGameManuals();
		await result.current.downloadManual(1, "Rulebook");

		expect(errorToast).toHaveBeenCalledWith("game:manuals.download-failed");
	});
});
