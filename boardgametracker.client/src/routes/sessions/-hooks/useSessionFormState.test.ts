import { act, renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { CreateSessionPlayer, Expansion, Game } from "@/models";
import { useSessionFormState } from "./useSessionFormState";

const expansion = (id: number, gameId: number) => ({ id, gameId, title: `Expansion ${id}` }) as unknown as Expansion;

const game = (id: number, hasScoring: boolean, expansions: Expansion[]) =>
	({ id, title: `Game ${id}`, hasScoring, maxPlayTime: 60, expansions }) as unknown as Game;

const scored = game(1, true, [expansion(11, 1), expansion(12, 1)]);
const cooperative = game(2, false, [expansion(21, 2)]);

const setup = (
	options: { selectedGameId?: number; games?: Game[]; initialGame?: Game; initialPlayers?: CreateSessionPlayer[] } = {},
) => {
	const store = { players: options.initialPlayers ?? [] };
	const hook = renderHook(
		({ selectedGameId, games }: { selectedGameId: number; games: Game[] }) =>
			useSessionFormState({
				selectedGameId,
				games,
				initialGame: options.initialGame,
				initialExpansions: [expansion(11, 1)],
				initialPlayers: options.initialPlayers ?? [],
				setPlayers: (updater) => {
					store.players = updater(store.players);
				},
			}),
		{ initialProps: { selectedGameId: options.selectedGameId ?? scored.id, games: options.games ?? [scored] } },
	);
	return { ...hook, store };
};

describe("useSessionFormState", () => {
	it("drops the selected expansions of the previous game when the game changes", () => {
		const { result, rerender } = setup({ games: [scored, cooperative], initialGame: scored });

		expect(result.current.selectedExpansionIds).toEqual([11]);

		rerender({ selectedGameId: cooperative.id, games: [scored, cooperative] });

		expect(result.current.selectedExpansionIds).toEqual([]);
		expect(result.current.expansionList.map((x) => x.id)).toEqual([21]);
		expect(result.current.hasScoring).toBe(false);
	});

	it("uses the page's game until the games list is loaded", () => {
		const { result } = setup({ selectedGameId: cooperative.id, games: [], initialGame: cooperative });

		expect(result.current.hasScoring).toBe(false);
		expect(result.current.expansionList.map((x) => x.id)).toEqual([21]);
	});

	it("adds a player once and removes it again", () => {
		const { result, store } = setup();

		act(() => {
			result.current.addPlayer(5);
			result.current.addPlayer(5);
			result.current.addPlayer(6);
		});

		expect(store.players).toEqual([
			{ playerId: 5, won: false, firstPlay: false, score: null },
			{ playerId: 6, won: false, firstPlay: false, score: null },
		]);

		act(() => result.current.removePlayer(5));

		expect(store.players.map((x) => x.playerId)).toEqual([6]);
	});

	it("pre-selects the winners from the scores, including ties", () => {
		const { result, store } = setup();

		act(() => {
			result.current.addPlayer(1);
			result.current.addPlayer(2);
			result.current.addPlayer(3);
			result.current.setScore(1, 70);
			result.current.setScore(2, 120);
		});

		expect(store.players.map((x) => x.won)).toEqual([false, true, false]);

		act(() => result.current.setScore(3, 120));

		expect(store.players.map((x) => x.won)).toEqual([false, true, true]);
	});

	it("stops following the scores once Won is toggled by hand", () => {
		const { result, store } = setup();

		act(() => {
			result.current.addPlayer(1);
			result.current.addPlayer(2);
			result.current.setScore(1, 10);
			result.current.setScore(2, 20);
			result.current.toggleWon(1);
		});

		expect(store.players.map((x) => x.won)).toEqual([true, true]);

		act(() => result.current.setScore(1, 5));

		expect(store.players.map((x) => x.won)).toEqual([true, true]);
	});

	it("keeps hand-picked winners of an existing session", () => {
		const { result, store } = setup({
			initialPlayers: [
				{ playerId: 1, score: 10, won: false, firstPlay: false },
				{ playerId: 2, score: 5, won: true, firstPlay: true },
			],
		});

		act(() => result.current.setScore(2, 1));

		expect(store.players.map((x) => x.won)).toEqual([false, true]);
	});

	it("toggles first play per player", () => {
		const { result, store } = setup();

		act(() => result.current.addPlayer(1));
		act(() => result.current.toggleFirstPlay(1));

		expect(store.players[0].firstPlay).toBe(true);
	});
});
