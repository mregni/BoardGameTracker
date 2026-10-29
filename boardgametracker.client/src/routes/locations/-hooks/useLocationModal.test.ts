import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook } from "@testing-library/react";
import React from "react";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: vi.fn(), errorToast: vi.fn() }),
}));

vi.mock("@/services/locationService", () => ({
	addLocationCall: vi.fn(),
	updateLocationCall: vi.fn().mockResolvedValue({ id: 2, name: "Home" }),
}));

import { useLocationModal } from "./useLocationModal";

describe("useLocationModal", () => {
	it("refreshes locations and game nights after an update", async () => {
		const queryClient = new QueryClient({ defaultOptions: { mutations: { retry: false } } });
		const invalidate = vi.spyOn(queryClient, "invalidateQueries");
		const wrapper = ({ children }: { children: React.ReactNode }) =>
			React.createElement(QueryClientProvider, { client: queryClient }, children);
		const { result } = renderHook(() => useLocationModal({}), { wrapper });

		await act(() => result.current.updateLocation({ id: 2, name: "Home" } as never));

		expect(invalidate).toHaveBeenCalledWith({ queryKey: ["locations"] });
		expect(invalidate).toHaveBeenCalledWith({ queryKey: ["gameNights"] });
	});
});
