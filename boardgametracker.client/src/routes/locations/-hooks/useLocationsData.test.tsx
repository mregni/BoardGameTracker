import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { renderHook, waitFor } from "@/test/test-utils";
import { useLocationsData } from "./useLocationsData";

vi.mock("@/services/queries/locations", () => ({
	getLocations: () => ({
		queryKey: ["locations"],
		queryFn: () =>
			Promise.resolve([
				{ id: 1, name: "Bij Kathleen", playCount: 0 },
				{ id: 2, name: "QA Location", playCount: 1 },
				{ id: 3, name: "Thuis", playCount: 9 },
				{ id: 4, name: "Café", playCount: 1 },
			]),
	}),
}));

const wrapper = ({ children }: { children: ReactNode }) => (
	<QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
		{children}
	</QueryClientProvider>
);

describe("useLocationsData", () => {
	it("starts empty while loading", () => {
		const { result } = renderHook(() => useLocationsData(), { wrapper });

		expect(result.current.isLoading).toBe(true);
		expect(result.current.locations).toEqual([]);
	});

	it("puts the most used location first and sorts ties by name", async () => {
		const { result } = renderHook(() => useLocationsData(), { wrapper });

		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.locations.map((x) => x.name)).toEqual(["Thuis", "Café", "QA Location", "Bij Kathleen"]);
	});
});
