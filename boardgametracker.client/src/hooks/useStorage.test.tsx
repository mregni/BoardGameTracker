import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { BoxOrientation, StorageItemType, StorageLayout } from "@/models";

const mocks = vi.hoisted(() => ({
	successToast: vi.fn(),
	errorToast: vi.fn(),
	invalidateStorage: vi.fn(() => Promise.resolve()),
	add: vi.fn(),
	update: vi.fn(),
	move: vi.fn(),
	remove: vi.fn(),
	assign: vi.fn(),
	arrange: vi.fn(),
	orientation: vi.fn(),
}));

vi.mock("@/services/storageService", () => ({
	addStorageLocationCall: mocks.add,
	updateStorageLocationCall: mocks.update,
	moveStorageLocationCall: mocks.move,
	deleteStorageLocationCall: mocks.remove,
	assignStorageCall: mocks.assign,
	arrangeStorageCall: mocks.arrange,
	updateBoxOrientationCall: mocks.orientation,
}));
vi.mock("@/services/queries/storage", () => ({
	getStorageLocations: () => ({ queryKey: ["storage"], queryFn: () => Promise.resolve([{ id: 5 }]) }),
}));
vi.mock("@/services/queries/locations", () => ({
	getLocations: () => ({ queryKey: ["locations"], queryFn: () => Promise.resolve([{ id: 1 }]) }),
}));
vi.mock("@/utils/errorUtils", () => ({ apiErrorMessage: (_error: unknown, fallback: string) => fallback }));
vi.mock("./useQueryInvalidator", () => ({
	useQueryInvalidator: () => ({ invalidateStorage: mocks.invalidateStorage }),
}));
vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: mocks.successToast, errorToast: mocks.errorToast }),
}));

import { useStorageActions, useStorageData } from "./useStorage";

const wrapper = ({ children }: { children: ReactNode }) => (
	<QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
		{children}
	</QueryClientProvider>
);

describe("useStorageData", () => {
	it("starts empty and loading, then returns the storage and the rooms", async () => {
		const { result } = renderHook(() => useStorageData(), { wrapper });

		expect(result.current).toEqual({ nodes: [], locations: [], isLoading: true });
		await waitFor(() => expect(result.current.isLoading).toBe(false));
		expect(result.current.nodes).toEqual([{ id: 5 }]);
		expect(result.current.locations).toEqual([{ id: 1 }]);
	});
});

describe("useStorageActions", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		for (const call of [
			mocks.add,
			mocks.update,
			mocks.move,
			mocks.remove,
			mocks.assign,
			mocks.arrange,
			mocks.orientation,
		]) {
			call.mockResolvedValue({ id: 9 });
		}
	});

	it("says what changed and refreshes the storage after each change", async () => {
		const { result } = renderHook(() => useStorageActions(), { wrapper });

		await act(() =>
			result.current.createStorageLocation({
				name: "Kallax",
				locationId: 1,
				parentId: null,
				layout: StorageLayout.Grid,
				rows: 2,
				columns: 2,
			}),
		);
		await act(() => result.current.updateStorageLocation({ id: 5, name: "Billy", rows: 3, columns: 2 }));
		await act(() => result.current.moveStorageLocation({ id: 5, locationId: 2, parentId: null }));
		await act(() => result.current.deleteStorageLocation(5));

		expect(mocks.update).toHaveBeenCalledWith(5, { name: "Billy", rows: 3, columns: 2 });
		expect(mocks.move).toHaveBeenCalledWith(5, { locationId: 2, parentId: null });
		expect(mocks.successToast.mock.calls.map(([key]) => key)).toEqual([
			"storage:notifications.created",
			"storage:notifications.saved",
			"storage:notifications.moved",
			"storage:notifications.deleted",
		]);
		expect(mocks.invalidateStorage).toHaveBeenCalledTimes(4);
	});

	it("puts boxes away, reorders and turns them without a toast", async () => {
		const { result } = renderHook(() => useStorageActions(), { wrapper });
		const items = [{ type: StorageItemType.Game, id: 3 }];

		await act(() => result.current.assignStorage({ items, locationId: 1, storageLocationId: 5 }));
		await act(() => result.current.arrangeStorage({ storageLocationId: 5, items }));
		await act(() => result.current.updateBoxOrientation({ item: items[0], orientation: BoxOrientation.Flat }));

		expect(mocks.assign).toHaveBeenCalled();
		expect(mocks.arrange).toHaveBeenCalled();
		expect(mocks.orientation).toHaveBeenCalled();
		expect(mocks.successToast).not.toHaveBeenCalled();
		expect(mocks.invalidateStorage).toHaveBeenCalledTimes(3);
	});

	it("shows an error and still refreshes when a change fails", async () => {
		mocks.remove.mockRejectedValue(new Error("nope"));
		const { result } = renderHook(() => useStorageActions(), { wrapper });

		await act(() => result.current.deleteStorageLocation(5).catch(() => undefined));

		expect(mocks.errorToast).toHaveBeenCalledWith("storage:notifications.failed");
		expect(mocks.successToast).not.toHaveBeenCalled();
		expect(mocks.invalidateStorage).toHaveBeenCalled();
	});

	it("reports pending while a change is on its way", async () => {
		let finish: (value: unknown) => void = () => undefined;
		mocks.assign.mockReturnValue(new Promise((resolve) => (finish = resolve)));
		const { result } = renderHook(() => useStorageActions(), { wrapper });

		expect(result.current.isPending).toBe(false);
		let saving: Promise<unknown> = Promise.resolve();
		act(() => {
			saving = result.current.assignStorage({ items: [], locationId: null, storageLocationId: null });
		});
		await waitFor(() => expect(result.current.isPending).toBe(true));
		await act(async () => {
			finish(undefined);
			await saving;
		});
		await waitFor(() => expect(result.current.isPending).toBe(false));
	});
});
