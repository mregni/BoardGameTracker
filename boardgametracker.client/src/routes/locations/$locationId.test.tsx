import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { type Game, type Session, StorageLayout, type StorageLocation } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor, within } from "@/test/test-utils";

interface CapturedRoute {
	component: FC;
	loader: (args: { params: { locationId: number }; context: { queryClient: unknown } }) => void;
}

const mocks = vi.hoisted(() => ({
	captured: {} as { config: CapturedRoute },
	sessions: [] as Session[],
	locationError: null as unknown,
	usage: { playCount: 12, gameNightCount: 0, storedCount: 4 },
	nodes: [] as StorageLocation[],
	games: [] as Game[],
	navigate: vi.fn(),
	deleteLocationCall: vi.fn(),
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return { ...config, useParams: () => ({ locationId: 3 }) };
	},
	useNavigate: () => mocks.navigate,
	useRouter: () => ({ history: { back: vi.fn() } }),
	Link: ({
		children,
		to,
		params,
		search,
	}: {
		children: React.ReactNode;
		to: string;
		params?: { sessionId: number };
		search?: Record<string, string>;
	}) => (
		<a
			href={`${params ? to.replace("$sessionId", String(params.sessionId)) : to}${search ? `?${new URLSearchParams(search)}` : ""}`}
		>
			{children}
		</a>
	),
}));

vi.mock("@/hooks/useStorage", () => ({
	useStorageData: () => ({ nodes: mocks.nodes, locations: [], isLoading: false }),
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ isAdmin: false, canWrite: true, canManageSettings: false }),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: vi.fn(), errorToast: vi.fn() }),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

vi.mock("@/services/locationService", () => ({
	deleteLocationCall: (id: number) => mocks.deleteLocationCall(id),
	updateLocationCall: vi.fn(),
	addLocationCall: vi.fn(),
}));

vi.mock("@/services/queries/locations", () => ({
	getLocation: (id: number) => ({
		queryKey: ["locations", id],
		queryFn: () =>
			mocks.locationError
				? Promise.reject(mocks.locationError)
				: Promise.resolve({
						id,
						name: "Café De Spelerij Gent-Sint-Pieters",
						...mocks.usage,
						lastPlayed: "2026-10-20T20:00:00",
					}),
	}),
	getLocationStatistics: (id: number) => ({
		queryKey: ["locations", id, "statistics"],
		queryFn: () => Promise.resolve({ distinctGameCount: 6, distinctPlayerCount: 5, totalPlayedTime: 460 }),
	}),
	getLocationSessions: (id: number) => ({
		queryKey: ["locations", id, "sessions"],
		queryFn: () => Promise.resolve(mocks.sessions),
	}),
}));

vi.mock("@/services/queries/games", () => ({
	getGames: () => ({
		queryKey: ["games"],
		queryFn: () => Promise.resolve(mocks.games),
	}),
}));

vi.mock("@/services/queries/players", () => ({
	getPlayers: () => ({
		queryKey: ["players"],
		queryFn: () => Promise.resolve([{ id: 7, name: "Mikhael", image: null, email: null, badges: [] }]),
	}),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ dateFormat: "dd-MM-yyyy", uiLanguage: "en-US" }),
	}),
}));

const session = (id: number, day: number): Session => ({
	id,
	comment: "",
	gameId: 1,
	start: new Date(2026, 9, day, 20, 0),
	end: new Date(2026, 9, day, 20, 40),
	minutes: 40,
	playerSessions: [
		{ sessionId: id, playerId: 7, won: true, firstPlay: false, score: null },
		{ sessionId: id, playerId: 8, won: false, firstPlay: false, score: null },
	],
	expansions: [],
	locationId: 3,
	gameNightId: null,
	isCoop: false,
	teamWon: null,
	teamScore: null,
});

const allSessions = Array.from({ length: 12 }, (_, index) => session(index + 1, 20 - index));

const game = (id: number, storedAtLocationId: number | null, storageLocationId: number | null) =>
	({
		id,
		title: id === 1 ? "Carcassonne" : `Game ${id}`,
		image: null,
		expansions: [],
		storedAtLocationId,
		storageLocationId,
		storagePosition: 0,
		isLoaned: false,
	}) as unknown as Game;

const node = (id: number, parentId: number | null, name: string | null, layout: StorageLayout) => ({
	id,
	name,
	locationId: 3,
	parentId,
	layout,
	rows: null,
	columns: null,
	row: name ? null : 1,
	column: name ? null : 1,
});

await import("./$locationId");

const renderPage = () => {
	const Component = mocks.captured.config.component;
	return renderWithProviders(<Component />);
};

describe("Location detail page", () => {
	beforeEach(() => {
		mocks.navigate.mockReset().mockResolvedValue(undefined);
		mocks.deleteLocationCall.mockReset().mockResolvedValue(undefined);
		mocks.locationError = null;
		mocks.usage = { playCount: 12, gameNightCount: 0, storedCount: 4 };
		mocks.sessions = allSessions;
		mocks.nodes = [];
		mocks.games = [game(1, null, null)];
	});

	it("prefetches the location, its statistics, sessions, games and players in the loader", () => {
		const queryClient = { query: vi.fn().mockResolvedValue(undefined) };

		mocks.captured.config.loader({ params: { locationId: 3 }, context: { queryClient } });

		const keys = queryClient.query.mock.calls.map(([options]) => options.queryKey);
		expect(keys).toEqual(
			expect.arrayContaining([
				["locations", 3],
				["locations", 3, "statistics"],
				["locations", 3, "sessions"],
				["games"],
				["players"],
			]),
		);
	});

	it("goes back to the locations overview", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(await screen.findByRole("button", { name: /detail.back/ }));

		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/locations", replace: true });
	});

	it("opens the edit dialog with the location name", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(await screen.findByRole("button", { name: "edit-named" }));

		expect(await screen.findByDisplayValue("Café De Spelerij Gent-Sint-Pieters")).toBeInTheDocument();
	});

	it("explains that a location in use cannot be deleted and offers no delete", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.click(await screen.findByRole("button", { name: "delete.named" }));

		const dialog = await screen.findByRole("dialog");
		expect(within(dialog).getByText("delete.in-use.title")).toBeInTheDocument();
		expect(within(dialog).getByText("delete.in-use.sessions delete.in-use.hint")).toBeInTheDocument();
		expect(within(dialog).queryByRole("button", { name: "delete.button" })).not.toBeInTheDocument();
		await user.click(within(dialog).getAllByRole("button", { name: "close" }).at(-1) as HTMLElement);

		expect(mocks.deleteLocationCall).not.toHaveBeenCalled();
	});

	it("also counts the game nights of a location", async () => {
		const user = userEvent.setup();
		mocks.usage = { playCount: 0, gameNightCount: 2, storedCount: 0 };
		renderPage();

		await user.click(await screen.findByRole("button", { name: "delete.named" }));

		expect(await screen.findByText("delete.in-use.game-nights delete.in-use.hint")).toBeInTheDocument();
	});

	it("deletes an unused location after confirming", async () => {
		const user = userEvent.setup();
		mocks.usage = { playCount: 0, gameNightCount: 0, storedCount: 0 };
		renderPage();

		await user.click(await screen.findByRole("button", { name: "delete.named" }));
		expect(await screen.findByText("delete.description")).toBeInTheDocument();

		const confirm = screen.getAllByRole("button", { name: "delete.button" }).at(-1);
		await user.click(confirm as HTMLElement);

		await waitFor(() => expect(mocks.deleteLocationCall).toHaveBeenCalledWith(3));
		await waitFor(() => expect(mocks.navigate).toHaveBeenCalledWith({ to: "/locations" }));
	});
	it("shows the location name on one line with a tooltip", async () => {
		renderPage();

		const heading = await screen.findByRole("heading", { level: 1, name: "Café De Spelerij Gent-Sint-Pieters" });
		expect(heading).toHaveClass("truncate");
		expect(heading).toHaveAttribute("title", "Café De Spelerij Gent-Sint-Pieters");
	});

	it("shows the statistics cards", async () => {
		renderPage();

		expect(await screen.findByText("detail.games")).toBeInTheDocument();
		expect(screen.getAllByText("detail.sessions").length).toBeGreaterThan(0);
		expect(screen.getByText("detail.players")).toBeInTheDocument();
		expect(screen.getByText("detail.play-time")).toBeInTheDocument();
		expect(screen.getByText("detail.stored")).toBeInTheDocument();
		expect(screen.getByText("12")).toBeInTheDocument();
		expect(screen.getByText("4")).toBeInTheDocument();
		expect(screen.getByText("6")).toBeInTheDocument();
		expect(screen.getByText("5")).toBeInTheDocument();
	});

	it("shows the five newest sessions and expands to all of them", async () => {
		const user = userEvent.setup();
		renderPage();
		const sessionLinks = () =>
			screen.getAllByRole("link").filter((link) => link.getAttribute("href")?.startsWith("/sessions/update/"));

		await screen.findByText("detail.recent-sessions");
		await waitFor(() => expect(sessionLinks()).toHaveLength(5));
		expect(sessionLinks()[0]).toHaveAttribute("href", "/sessions/update/1");
		expect(sessionLinks()[0]).toHaveTextContent("20-10-2026");

		await user.click(screen.getByRole("button", { name: "detail.show-all" }));

		expect(sessionLinks()).toHaveLength(12);
		expect(sessionLinks()[11]).toHaveTextContent("09-10-2026");
	});

	it("leads with the most played game and the top winner at this location", async () => {
		renderPage();

		expect(await screen.findByText("detail.most-played")).toBeInTheDocument();
		expect(screen.getByText("detail.top-winners")).toBeInTheDocument();
		expect(screen.getByRole("link", { name: /Carcassonne\s*detail.session-count/ })).toHaveAttribute(
			"href",
			"/games/1",
		);
		expect(screen.getByRole("link", { name: /Mikhael\s*detail.win-count/ })).toHaveAttribute("href", "/players/7");
	});

	it("shows a not-found state with a way back instead of spinning forever", async () => {
		mocks.locationError = { kind: "client", status: 404, message: "Not found" };
		const user = userEvent.setup();
		renderPage();

		expect(await screen.findByText("detail.not-found.title")).toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: "detail.back" }));
		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/locations", replace: true });
	});

	it("shows the edit and delete buttons for writers", async () => {
		renderPage();

		expect(await screen.findByRole("button", { name: "edit-named" })).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "delete.named" })).toBeInTheDocument();
	});

	it("warns that deleting removes its storage locations and leaves its games without a place", async () => {
		const user = userEvent.setup();
		mocks.usage = { playCount: 0, gameNightCount: 0, storedCount: 2 };
		mocks.nodes = [node(20, null, "Living room", StorageLayout.Free), node(21, 20, "Kallax", StorageLayout.Grid)];
		renderPage();

		await user.click(await screen.findByRole("button", { name: "delete.named" }));

		expect(
			await screen.findByText("delete.description delete.storage.inside delete.storage.unplaced"),
		).toBeInTheDocument();
	});

	it("lists the storage locations with what is inside and how much is stored below each", async () => {
		mocks.nodes = [
			node(20, null, "Living room", StorageLayout.Free),
			node(21, 20, "Kallax", StorageLayout.Grid),
			node(22, 21, null, StorageLayout.Slot),
			node(23, 20, null, StorageLayout.Slot),
			node(30, null, "Attic", StorageLayout.Free),
		];
		mocks.games = [game(1, 3, 22), game(2, 3, 20), game(3, 3, null), game(4, 4, null)];
		renderPage();

		const card = (await screen.findByText("detail.storage.title")).closest("div.rounded-lg") as HTMLElement;
		const rows = await within(card).findAllByRole("listitem");
		expect(rows.map((row) => row.textContent)).toEqual([
			"Attic0 detail.storage.count",
			"Living roomKallax2 detail.storage.count",
			"storage:place.somewhere-in1 detail.storage.count",
		]);
		expect(within(card).getByRole("link", { name: "detail.storage.open" })).toHaveAttribute("href", "/storage?at=3%3A");
	});

	it("says nothing is stored when the location has no storage at all", async () => {
		renderPage();

		expect(await screen.findByText("detail.storage.empty")).toBeInTheDocument();
	});

	it("leads with storage and keeps sessions to one line when nothing was played here", async () => {
		mocks.sessions = [];
		mocks.usage = { playCount: 0, gameNightCount: 0, storedCount: 0 };
		renderPage();

		expect(await screen.findByText("detail.no-sessions")).toBeInTheDocument();
		expect(screen.getByText("detail.storage.title")).toBeInTheDocument();
		expect(screen.queryByText("detail.recent-sessions")).not.toBeInTheDocument();
		expect(screen.queryByText("detail.sessions")).not.toBeInTheDocument();
	});
});
