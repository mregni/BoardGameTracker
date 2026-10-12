import { type InvalidateQueryFilters, QueryClient } from "@tanstack/react-query";
import { beforeEach, describe, expect, it, type MockInstance, vi } from "vitest";
import { QUERY_KEYS } from "@/models";
import { QueryInvalidator } from "./invalidations";

type InvalidateSpy = MockInstance<(filters?: InvalidateQueryFilters) => Promise<void>>;

const invalidatedKeys = (spy: InvalidateSpy): (readonly unknown[] | undefined)[] =>
	spy.mock.calls.map(([filters]) => filters?.queryKey);

describe("QueryInvalidator", () => {
	let queryClient: QueryClient;
	let spy: InvalidateSpy;
	let invalidator: QueryInvalidator;

	beforeEach(() => {
		queryClient = new QueryClient();
		spy = vi.spyOn(queryClient, "invalidateQueries").mockResolvedValue(undefined);
		invalidator = new QueryInvalidator(queryClient);
	});

	it("invalidateGame refreshes the game, its sub-queries, the list, counts, shames and the dashboard", async () => {
		await invalidator.invalidateGame(7);

		const keys = invalidatedKeys(spy);
		expect(keys).toEqual(
			expect.arrayContaining([
				[QUERY_KEYS.games],
				[QUERY_KEYS.game, 7],
				[QUERY_KEYS.game, 7, QUERY_KEYS.statistics],
				[QUERY_KEYS.game, 7, QUERY_KEYS.sessions],
				[QUERY_KEYS.counts],
				[QUERY_KEYS.shames],
				[QUERY_KEYS.dashboard, QUERY_KEYS.statistics],
			]),
		);
		expect(keys).not.toContainEqual([QUERY_KEYS.game, 8]);
	});

	it("invalidateSession refreshes the game, every player that played and the compare page", async () => {
		await invalidator.invalidateSession(42, 7, [1, 2]);

		const keys = invalidatedKeys(spy);
		expect(keys).toEqual(
			expect.arrayContaining([
				[QUERY_KEYS.sessions],
				[QUERY_KEYS.sessions, 42],
				[QUERY_KEYS.game, 7],
				[QUERY_KEYS.player, 1],
				[QUERY_KEYS.player, 1, QUERY_KEYS.badges],
				[QUERY_KEYS.player, 2],
				[QUERY_KEYS.locations],
				[QUERY_KEYS.compare],
				[QUERY_KEYS.leaderboard],
			]),
		);
	});

	it("invalidateSessionDeleted refreshes the game, every participant and the leaderboard", async () => {
		await invalidator.invalidateSessionDeleted(7, [1, 2]);

		const keys = invalidatedKeys(spy);
		expect(keys).toEqual(
			expect.arrayContaining([
				[QUERY_KEYS.game, 7, QUERY_KEYS.statistics],
				[QUERY_KEYS.player, 1, QUERY_KEYS.statistics],
				[QUERY_KEYS.player, 2, QUERY_KEYS.sessions],
				[QUERY_KEYS.leaderboard],
				[QUERY_KEYS.dashboard, QUERY_KEYS.statistics],
			]),
		);
	});

	it("invalidateSessionDeleted falls back to the lists when ids are unknown", async () => {
		await invalidator.invalidateSessionDeleted();

		const keys = invalidatedKeys(spy);
		expect(keys).toEqual(expect.arrayContaining([[QUERY_KEYS.games], [QUERY_KEYS.players], [QUERY_KEYS.sessions]]));
		expect(keys.some((key) => key?.[0] === QUERY_KEYS.game && key.length > 1)).toBe(false);
	});

	it("invalidateLoan refreshes the loans and the lent game", async () => {
		await invalidator.invalidateLoan(3, 9);

		expect(invalidatedKeys(spy)).toEqual(
			expect.arrayContaining([[QUERY_KEYS.loans], [QUERY_KEYS.loans, 3], [QUERY_KEYS.game, 9], [QUERY_KEYS.counts]]),
		);
	});

	it("invalidateLoan without ids only touches the lists", async () => {
		await invalidator.invalidateLoan();

		const keys = invalidatedKeys(spy);
		expect(keys).toContainEqual([QUERY_KEYS.loans]);
		expect(keys.some((key) => key?.[0] === QUERY_KEYS.loans && key.length > 1)).toBe(false);
	});

	it("invalidateSettings also refreshes the live prices that depend on the changedetection settings", async () => {
		await invalidator.invalidateSettings();

		const keys = invalidatedKeys(spy);
		expect(keys).toEqual(
			expect.arrayContaining([
				[QUERY_KEYS.settings],
				[QUERY_KEYS.trackedPrices],
				[QUERY_KEYS.counts],
				[QUERY_KEYS.shames],
				[QUERY_KEYS.gameNights],
			]),
		);
		const predicate = spy.mock.calls.map(([filters]) => filters?.predicate).find(Boolean);
		expect(predicate?.({ queryKey: [QUERY_KEYS.game, 3, QUERY_KEYS.price] } as never)).toBe(true);
		expect(predicate?.({ queryKey: [QUERY_KEYS.game, 3, QUERY_KEYS.sessions] } as never)).toBe(false);
	});

	it("invalidateGameNights refreshes the game nights and the menu counts", async () => {
		await invalidator.invalidateGameNights();

		expect(invalidatedKeys(spy)).toEqual(expect.arrayContaining([[QUERY_KEYS.gameNights], [QUERY_KEYS.counts]]));
	});

	it("invalidateGameCreated refreshes the list, counts, shames and dashboard", async () => {
		await invalidator.invalidateGameCreated();

		expect(invalidatedKeys(spy)).toEqual(
			expect.arrayContaining([
				[QUERY_KEYS.games],
				[QUERY_KEYS.counts],
				[QUERY_KEYS.shames],
				[QUERY_KEYS.dashboard, QUERY_KEYS.charts],
			]),
		);
	});
});
