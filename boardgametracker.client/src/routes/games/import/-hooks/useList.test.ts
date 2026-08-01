import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import React from "react";
import { describe, expect, it, vi } from "vitest";

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
    invalidateGames: vi.fn(),
    invalidateCounts: vi.fn(),
    invalidateDashboard: vi.fn(),
  }),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
  useToasts: () => ({ successToast: vi.fn(), errorToast: vi.fn() }),
}));

vi.mock("@/services/gameService", () => ({
  importGamesCall: vi.fn(() => Promise.resolve()),
}));

import { useList } from "./useList";

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
  await waitFor(() =>
    expect(rendered.result.current.processingGames).toBe(false),
  );
  return rendered;
};

describe("useList", () => {
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

    expect(result.current.games.find((g) => g.bggId === 1)?.checked).toBe(
      false,
    );
    expect(result.current.games.find((g) => g.bggId === 2)?.checked).toBe(true);
  });
});
