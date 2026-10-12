import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { QUERY_KEYS } from "@/models";
import { act, renderHook, waitFor } from "@/test/test-utils";
import { useLocationData } from "./useLocationData";

const mocks = vi.hoisted(() => ({
	navigate: vi.fn(),
	deleteLocationCall: vi.fn(),
	successToast: vi.fn(),
	errorToast: vi.fn(),
}));

vi.mock("@tanstack/react-router", () => ({
	useNavigate: () => mocks.navigate,
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: mocks.successToast, errorToast: mocks.errorToast }),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

vi.mock("@/services/locationService", () => ({
	deleteLocationCall: (id: number) => mocks.deleteLocationCall(id),
}));

vi.mock("@/services/queries/locations", () => ({
	getLocation: (id: number) => ({
		queryKey: ["locations", id],
		queryFn: () => Promise.resolve({ id, name: "Thuis", playCount: 2 }),
	}),
	getLocationStatistics: (id: number) => ({
		queryKey: ["locations", id, "statistics"],
		queryFn: () => Promise.resolve({ distinctGameCount: 1, distinctPlayerCount: 2, totalPlayedTime: 60 }),
	}),
	getLocationSessions: (id: number) => ({
		queryKey: ["locations", id, "sessions"],
		queryFn: () => Promise.resolve([]),
	}),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ dateFormat: "dd-MM-yyyy", uiLanguage: "en-US" }),
	}),
}));

const setup = () => {
	const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
	const invalidate = vi.spyOn(queryClient, "invalidateQueries");
	const remove = vi.spyOn(queryClient, "removeQueries");
	const wrapper = ({ children }: { children: ReactNode }) => (
		<QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
	);
	const hook = renderHook(() => useLocationData({ locationId: 3 }), { wrapper });
	return { hook, invalidate, remove };
};

describe("useLocationData", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.navigate.mockResolvedValue(undefined);
	});

	it("loads the location, its statistics, sessions and settings", async () => {
		const { hook } = setup();

		expect(hook.result.current.isLoading).toBe(true);
		await waitFor(() => expect(hook.result.current.isLoading).toBe(false));

		expect(hook.result.current.location).toEqual({ id: 3, name: "Thuis", playCount: 2 });
		expect(hook.result.current.statistics?.distinctPlayerCount).toBe(2);
		expect(hook.result.current.sessions).toEqual([]);
		expect(hook.result.current.settings?.dateFormat).toBe("dd-MM-yyyy");
	});

	it("goes back to the overview and refreshes the lists after a delete", async () => {
		mocks.deleteLocationCall.mockResolvedValue(undefined);
		const { hook, invalidate, remove } = setup();

		await act(() => hook.result.current.deleteLocation());

		expect(mocks.deleteLocationCall).toHaveBeenCalledWith(3);
		expect(mocks.successToast).toHaveBeenCalledWith("location:notifications.deleted");
		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/locations" });
		expect(remove).toHaveBeenCalledWith({ queryKey: [QUERY_KEYS.locations, 3] });
		expect(invalidate).toHaveBeenCalledWith({ queryKey: [QUERY_KEYS.locations] });
		expect(invalidate).toHaveBeenCalledWith({ queryKey: [QUERY_KEYS.counts] });
	});

	it("stays on the page and shows an error when the delete fails", async () => {
		mocks.deleteLocationCall.mockRejectedValue(new Error("in use"));
		const { hook, remove } = setup();

		await act(() => hook.result.current.deleteLocation());

		expect(mocks.errorToast).toHaveBeenCalledWith("location:notifications.delete-failed");
		expect(mocks.successToast).not.toHaveBeenCalled();
		expect(mocks.navigate).not.toHaveBeenCalled();
		expect(remove).not.toHaveBeenCalled();
	});
});
