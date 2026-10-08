import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { Expansion, Game } from "@/models";
import { useSessionFormState } from "./useSessionFormState";

const expansion = (id: number, gameId: number) => ({ id, gameId, title: `Expansion ${id}` }) as unknown as Expansion;

const game = (id: number, hasScoring: boolean, expansions: Expansion[]) =>
	({ id, title: `Game ${id}`, hasScoring, maxPlayTime: 60, expansions }) as unknown as Game;

const createForm = (gameId: number) => {
	const listeners = new Set<() => void>();
	const state = { values: { gameId } };
	return {
		store: {
			state,
			subscribe: (listener: () => void) => {
				listeners.add(listener);
				return { unsubscribe: () => listeners.delete(listener) };
			},
		},
		setFieldValue: vi.fn(),
		selectGame: (id: number) => {
			state.values.gameId = id;
			for (const listener of listeners) listener();
		},
	};
};

const scored = game(1, true, [expansion(11, 1), expansion(12, 1)]);
const cooperative = game(2, false, [expansion(21, 2)]);

describe("useSessionFormState", () => {
	it("drops the selected expansions of the previous game when the game changes", () => {
		const form = createForm(scored.id);
		const { result } = renderHook(() =>
			useSessionFormState({
				form,
				games: [scored, cooperative],
				initialGame: scored,
				initialExpansions: [expansion(11, 1)],
			}),
		);

		expect(result.current.selectedExpansionIds).toEqual([11]);

		act(() => form.selectGame(cooperative.id));

		expect(result.current.selectedGameId).toBe(cooperative.id);
		expect(result.current.selectedExpansionIds).toEqual([]);
		expect(result.current.expansionList.map((x) => x.id)).toEqual([21]);
	});

	it("takes the scoring mode from the selected game, not from the page's game", () => {
		const form = createForm(0);
		const { result } = renderHook(() => useSessionFormState({ form, games: [scored, cooperative] }));

		expect(result.current.hasScoring).toBe(true);

		act(() => form.selectGame(cooperative.id));

		expect(result.current.hasScoring).toBe(false);
	});

	it("shows the expansions once the games list arrives after the form mounted", () => {
		const form = createForm(scored.id);
		const initialGame = game(1, true, []);
		const { result, rerender } = renderHook(
			({ games }: { games: Game[] }) =>
				useSessionFormState({ form, games, initialGame, initialExpansions: [expansion(12, 1)] }),
			{ initialProps: { games: [] as Game[] } },
		);

		expect(result.current.expansionList).toEqual([]);

		rerender({ games: [scored, cooperative] });

		expect(result.current.expansionList.map((x) => x.id)).toEqual([11, 12]);
		expect(result.current.selectedExpansionIds).toEqual([12]);
	});

	it("uses the page's game until the games list is loaded", () => {
		const form = createForm(cooperative.id);
		const { result } = renderHook(() => useSessionFormState({ form, games: [], initialGame: cooperative }));

		expect(result.current.hasScoring).toBe(false);
		expect(result.current.expansionList.map((x) => x.id)).toEqual([21]);
	});
});
