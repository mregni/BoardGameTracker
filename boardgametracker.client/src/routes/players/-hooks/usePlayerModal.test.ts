import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook } from "@testing-library/react";
import React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const { successToast, errorToast } = vi.hoisted(() => ({
	successToast: vi.fn(),
	errorToast: vi.fn(),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast, errorToast }),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallbackKey: string) => fallbackKey,
}));

vi.mock("@/services/imageService", () => ({ uploadImageCall: vi.fn() }));

vi.mock("@/services/playerService", () => ({
	addPlayerCall: vi.fn(),
	updatePlayerCall: vi.fn(),
	linkPlayerAccountCall: vi.fn(),
}));

import { linkPlayerAccountCall } from "@/services/playerService";
import { usePlayerModal } from "./usePlayerModal";

const setup = () => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false, gcTime: 0 }, mutations: { retry: false } },
	});
	const invalidate = vi.spyOn(queryClient, "invalidateQueries");
	const wrapper = ({ children }: { children: React.ReactNode }) =>
		React.createElement(QueryClientProvider, { client: queryClient }, children);
	const { result } = renderHook(() => usePlayerModal({}), { wrapper });
	return { result, invalidate };
};

describe("usePlayerModal linked account", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("refreshes players, accounts and the profile after changing the link", async () => {
		vi.mocked(linkPlayerAccountCall).mockResolvedValue({ id: 4, name: "Jane", image: null, email: null, badges: [] });
		const { result, invalidate } = setup();

		await act(() => result.current.linkAccount({ playerId: 4, userId: "u1" }));

		expect(linkPlayerAccountCall).toHaveBeenCalledWith(4, "u1");
		expect(invalidate).toHaveBeenCalledWith({ queryKey: ["players"] });
		expect(invalidate).toHaveBeenCalledWith({ queryKey: ["player"] });
		expect(invalidate).toHaveBeenCalledWith({ queryKey: ["users"] });
		expect(invalidate).toHaveBeenCalledWith({ queryKey: ["profile"] });
	});

	it("reports a failed link change", async () => {
		vi.mocked(linkPlayerAccountCall).mockRejectedValue(new Error("nope"));
		const { result } = setup();

		await act(() => result.current.linkAccount({ playerId: 4, userId: null }).catch(() => undefined));

		expect(errorToast).toHaveBeenCalledWith("player:account.link-failed");
	});
});
