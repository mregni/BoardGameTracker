import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { type BggSearch, type Game, GameState } from "@/models";

const mocks = vi.hoisted(() => ({
	addGameWithBggCall: vi.fn(),
	invalidateGameCreated: vi.fn(() => Promise.resolve()),
	successToast: vi.fn(),
	errorToast: vi.fn(),
}));

vi.mock("@/services/gameService", () => ({ addGameWithBggCall: mocks.addGameWithBggCall }));
vi.mock("@/utils/errorUtils", () => ({ apiErrorMessage: (_error: unknown, fallback: string) => fallback }));
vi.mock("@/hooks/useQueryInvalidator", () => ({
	useQueryInvalidator: () => ({ invalidateGameCreated: mocks.invalidateGameCreated }),
}));
vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: mocks.successToast, errorToast: mocks.errorToast }),
}));
vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({ queryKey: ["settings"], queryFn: () => Promise.resolve({ currency: "€" }) }),
}));

import { useBggGameModal } from "./useBggGameModal";

const wrapper = ({ children }: { children: ReactNode }) => (
	<QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
		{children}
	</QueryClientProvider>
);

const search: BggSearch = {
	bggId: " 325 ",
	price: null,
	additionDate: new Date("2026-11-15"),
	state: GameState.Owned,
	hasScoring: false,
};

describe("useBggGameModal", () => {
	beforeEach(() => vi.clearAllMocks());

	it("says the game was added when BGG returned the game itself", async () => {
		const game = { id: 4, title: "Catan: Seafarers", bggId: 325 } as Game;
		mocks.addGameWithBggCall.mockResolvedValue(game);
		const onSuccess = vi.fn();
		const { result } = renderHook(() => useBggGameModal({ onSuccess }), { wrapper });

		await act(() => result.current.save(search));

		expect(mocks.successToast).toHaveBeenCalledWith("game:notifications.created");
		expect(onSuccess).toHaveBeenCalledWith(game);
	});

	it("says which base game an expansion was added to", async () => {
		const catan = { id: 1, title: "Catan", bggId: 13 } as Game;
		mocks.addGameWithBggCall.mockResolvedValue(catan);
		const onSuccess = vi.fn();
		const { result } = renderHook(() => useBggGameModal({ onSuccess }), { wrapper });

		await act(() => result.current.save(search));

		expect(mocks.successToast).toHaveBeenCalledWith("game:notifications.expansion-created", undefined, {
			title: "Catan",
		});
		expect(onSuccess).toHaveBeenCalledWith(catan);
	});
});
