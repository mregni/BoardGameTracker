import { noop } from "@tanstack/react-query";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { getBadges } from "@/services/queries/basdges";
import { getDashboardStatistics } from "@/services/queries/dashboard";
import { getGameNights } from "@/services/queries/gameNights";
import {
	getBggCollection,
	getGame,
	getGameSessions,
	getGameSessionsShortList,
	getGameStatistics,
	getGames,
	getShameStatistics,
	getShames,
} from "@/services/queries/games";
import { getLeaderboard } from "@/services/queries/leaderboard";
import { getLocations } from "@/services/queries/locations";
import { getPlayer, getPlayerSessions, getPlayerStatistics, getPlayers } from "@/services/queries/players";
import { getSession } from "@/services/queries/sessions";
import { getLanguages, getSettings } from "@/services/queries/settings";

type QueryOptions = { queryKey: readonly unknown[]; staleTime?: unknown };

interface FakeQueryClient {
	query: ReturnType<typeof vi.fn<(options: QueryOptions) => Promise<unknown>>>;
}

interface CapturedRoute {
	beforeLoad?: (args: { context: { queryClient: FakeQueryClient } }) => Promise<void>;
	loader?: (args: {
		params: Record<string, number | string>;
		deps: Record<string, unknown>;
		context: { queryClient: FakeQueryClient };
	}) => unknown;
}

const mocks = vi.hoisted(() => ({
	routes: {} as Record<string, CapturedRoute>,
}));

vi.mock("@tanstack/react-router", async (importOriginal) => ({
	...(await importOriginal<typeof import("@tanstack/react-router")>()),
	createFileRoute: (path: string) => (config: CapturedRoute) => {
		mocks.routes[path] = config;
		return config;
	},
	redirect: (options: unknown) => ({ redirectTo: options }),
}));

vi.mock("@/utils/i18n", () => ({
	default: { t: (key: string) => key, language: "en" },
}));

await Promise.all([
	import("./index"),
	import("./compare/index"),
	import("./game-nights/index"),
	import("./games/$gameId"),
	import("./games/$gameId_.sessions"),
	import("./games/$gameId_.update"),
	import("./games/bgg"),
	import("./games/index"),
	import("./games/new"),
	import("./games/import/list_.$username"),
	import("./games/import/start"),
	import("./leaderboard/index"),
	import("./players/$playerId"),
	import("./players/$playerId_.sessions"),
	import("./sessions/new"),
	import("./sessions/new_.$gameId"),
	import("./sessions/update_.$sessionId"),
	import("./settings/index"),
	import("./shames/index"),
]);

const createQueryClient = (resolve: (options: QueryOptions) => unknown = () => undefined): FakeQueryClient => ({
	query: vi.fn((options: QueryOptions) => Promise.resolve(resolve(options))),
});

const route = (path: string) => {
	const config = mocks.routes[path];
	if (!config) throw new Error(`route ${path} was not registered`);
	return config;
};

const queriedKeys = (queryClient: FakeQueryClient) => queryClient.query.mock.calls.map(([options]) => options.queryKey);

describe("route loaders", () => {
	let queryClient: FakeQueryClient;

	beforeEach(() => {
		queryClient = createQueryClient();
	});

	it.each<[string, Record<string, number | string>, QueryOptions[]]>([
		["/", {}, [getDashboardStatistics()]],
		["/compare/", {}, [getPlayers()]],
		["/game-nights/", {}, [getGameNights(), getPlayers(), getGames(), getLocations()]],
		[
			"/games/$gameId",
			{ gameId: 4 },
			[getGame(4), getGameStatistics(4), getSettings(), getGameSessionsShortList(4, 5)],
		],
		["/games/$gameId_/sessions", { gameId: 4 }, [getGame(4), getSettings(), getPlayers(), getGameSessions(4)]],
		["/games/$gameId_/update", { gameId: 4 }, [getSettings()]],
		["/games/bgg", {}, [getSettings()]],
		["/games/", {}, [getGames(), getSettings()]],
		["/games/new", {}, [getSettings()]],
		["/games/import/list_/$username", { username: "tester" }, [getBggCollection("tester"), getGames(), getSettings()]],
		["/leaderboard/", {}, [getLeaderboard({ minSessions: 5, rankBy: "wins" }), getGames()]],
		["/players/$playerId", { playerId: 2 }, [getPlayer(2), getPlayerStatistics(2), getBadges()]],
		[
			"/players/$playerId_/sessions",
			{ playerId: 2 },
			[getSettings(), getGames(), getPlayers(), getPlayer(2), getPlayerSessions(2)],
		],
		["/sessions/new", {}, [getGames(), getPlayers(), getLocations()]],
		["/sessions/new_/$gameId", { gameId: 4 }, [getGame(4), getGames(), getLocations(), getPlayers()]],
		["/settings/", {}, [getSettings(), getLanguages()]],
		["/shames/", {}, [getShames(), getShameStatistics()]],
	])("prefetches the queries of %s", async (path, params, expected) => {
		await route(path).loader?.({ params, deps: {}, context: { queryClient } });

		expect(queriedKeys(queryClient)).toEqual(expected.map((options) => options.queryKey));
		for (const [options] of queryClient.query.mock.calls) {
			expect(options.staleTime).not.toBe("static");
		}
	});

	it("swallows prefetch errors in the loaders", () => {
		const swallow = vi.fn();
		queryClient.query.mockReturnValue({ catch: swallow } as unknown as Promise<unknown>);

		route("/games/").loader?.({ params: {}, deps: {}, context: { queryClient } });

		expect(swallow).toHaveBeenCalledTimes(2);
		expect(swallow).toHaveBeenCalledWith(noop);
	});

	it("loads the session first and then prefetches its game", async () => {
		const sessionKey = getSession(3).queryKey;
		queryClient = createQueryClient((options) => (options.queryKey[0] === sessionKey[0] ? { gameId: 9 } : undefined));

		await route("/sessions/update_/$sessionId").loader?.({
			params: { sessionId: 3 },
			deps: {},
			context: { queryClient },
		});

		expect(queriedKeys(queryClient)).toEqual([sessionKey, getGame(9).queryKey]);
	});

	it.each([
		["/game-nights/", { gameNightsEnabled: true }, { gameNightsEnabled: false }, "/"],
		["/shames/", { shelfOfShameEnabled: true }, { shelfOfShameEnabled: false }, "/"],
		["/games/import/start", { bggStatus: { isConfigured: true } }, { bggStatus: { isConfigured: false } }, "/games"],
		[
			"/games/import/list_/$username",
			{ bggStatus: { isConfigured: true } },
			{ bggStatus: { isConfigured: false } },
			"/games",
		],
	])("guards %s on the cached settings", async (path, enabled, disabled, target) => {
		const allowed = createQueryClient(() => enabled);
		await expect(route(path).beforeLoad?.({ context: { queryClient: allowed } })).resolves.toBeUndefined();
		expect(allowed.query).toHaveBeenCalledWith(
			expect.objectContaining({ queryKey: getSettings().queryKey, staleTime: "static" }),
		);

		const blocked = createQueryClient(() => disabled);
		await expect(route(path).beforeLoad?.({ context: { queryClient: blocked } })).rejects.toEqual({
			redirectTo: { to: target },
		});
	});
});
