import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	BoxOrientation,
	type Game,
	GameState,
	type Location,
	StorageItemType,
	StorageLayout,
	type StorageLocation,
} from "@/models";
import { renderWithProviders, screen, userEvent, within } from "@/test/test-utils";

interface CapturedRoute {
	component: FC;
	loader: (args: { context: { queryClient: unknown } }) => void;
}

const mocks = vi.hoisted(() => ({
	captured: {} as { config: CapturedRoute },
	search: {} as { at?: string },
	navigate: vi.fn(),
	assign: vi.fn(() => Promise.resolve()),
	arrange: vi.fn(() => Promise.resolve()),
	orientation: vi.fn(() => Promise.resolve()),
	games: [] as Game[],
	nodes: [] as StorageLocation[],
	locations: [] as Location[],
	canWrite: true,
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return { ...config, useSearch: () => mocks.search, fullPath: "/storage/" };
	},
	useNavigate: () => mocks.navigate,
	Link: ({ children }: { children: React.ReactNode }) => <a href="/games">{children}</a>,
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ isAdmin: false, canWrite: mocks.canWrite, canManageSettings: false }),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: vi.fn(), errorToast: vi.fn() }),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

vi.mock("@/hooks/useStorage", () => ({
	useStorageData: () => ({ nodes: mocks.nodes, locations: mocks.locations, isLoading: false }),
	useStorageActions: () => ({
		assignStorage: mocks.assign,
		arrangeStorage: mocks.arrange,
		updateBoxOrientation: mocks.orientation,
		createStorageLocation: vi.fn(),
		updateStorageLocation: vi.fn(),
		moveStorageLocation: vi.fn(),
		deleteStorageLocation: vi.fn(),
		isPending: false,
	}),
}));

vi.mock("@/services/queries/games", () => ({
	getGames: () => ({ queryKey: ["games"], queryFn: () => Promise.resolve(mocks.games) }),
}));

await import("./index");

const node = (id: number, parentId: number | null, overrides: Partial<StorageLocation> = {}): StorageLocation => ({
	id,
	name: `Node ${id}`,
	locationId: 1,
	parentId,
	layout: StorageLayout.Free,
	rows: null,
	columns: null,
	row: null,
	column: null,
	...overrides,
});

const game = (id: number, title: string, overrides: Partial<Game> = {}) =>
	({
		id,
		title,
		image: null,
		state: GameState.Owned,
		isLoaned: false,
		expansions: [],
		storedAtLocationId: null,
		storageLocationId: null,
		storagePosition: 0,
		boxOrientation: BoxOrientation.Side,
		...overrides,
	}) as unknown as Game;

const renderPage = () => {
	const Component = mocks.captured.config.component;
	return renderWithProviders(<Component />);
};

describe("Storage page", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.canWrite = true;
		mocks.search = {};
		mocks.locations = [
			{ id: 1, name: "Home", playCount: 4, gameNightCount: 0, storedCount: 3, lastPlayed: null },
			{ id: 2, name: "Trailer", playCount: 0, gameNightCount: 0, storedCount: 0, lastPlayed: null },
		];
		mocks.nodes = [
			node(10, null, { name: "Office" }),
			node(11, 10, { name: "Billy", layout: StorageLayout.Shelves, rows: 2, columns: 1 }),
			node(12, 11, { name: null, layout: StorageLayout.Slot, row: 1, column: 1 }),
			node(13, 11, { name: null, layout: StorageLayout.Slot, row: 2, column: 1 }),
		];
		mocks.games = [
			game(1, "Wingspan", { storedAtLocationId: 1, storageLocationId: 12, storagePosition: 0 }),
			game(2, "Pandemic", { storedAtLocationId: 1, storageLocationId: 12, storagePosition: 1 }),
			game(3, "Skull", { storedAtLocationId: 1 }),
			game(4, "Root"),
			game(5, "Ark Nova", { state: GameState.Wanted }),
		];
	});

	it("prefetches storage, locations and games", () => {
		const queryClient = { query: vi.fn().mockResolvedValue(undefined) };

		mocks.captured.config.loader({ context: { queryClient } });

		expect(queryClient.query).toHaveBeenCalledTimes(3);
	});

	it("opens the first location and lists what is stored directly in it", async () => {
		renderPage();

		expect(await screen.findByRole("heading", { name: "Home", level: 2 })).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Skull" })).toBeInTheDocument();
		const content = screen.getByRole("region", { name: "Home" });
		expect(within(content).getByRole("button", { name: /Office/ })).toBeInTheDocument();
		const tree = screen.getByRole("list", { name: "page.tree" });
		expect(within(tree).getByRole("button", { name: /Home/ })).toHaveAttribute("aria-current", "true");
	});

	it("goes to a storage location from the tree", async () => {
		const user = userEvent.setup();
		renderPage();

		const tree = await screen.findByRole("list", { name: "page.tree" });
		await user.click(within(tree).getByRole("button", { name: /Office/ }));

		expect(mocks.navigate).toHaveBeenCalledWith({ search: { at: "1:10" } });
	});

	it("draws a cabinet with its shelves and turns a selected box", async () => {
		const user = userEvent.setup();
		mocks.search = { at: "1:11" };
		renderPage();

		const shelves = await screen.findAllByRole("group", { name: "storage:slot.shelf" });
		expect(shelves).toHaveLength(2);
		expect(
			within(shelves[0])
				.getAllByRole("button")
				.map((box) => box.getAttribute("aria-label")),
		).toEqual(["Wingspan", "Pandemic"]);

		await user.click(within(shelves[0]).getByRole("button", { name: "Pandemic" }));
		await user.click(screen.getByRole("button", { name: "orientation.flat" }));
		expect(mocks.orientation).toHaveBeenCalledWith({
			item: { type: StorageItemType.Game, id: 2 },
			orientation: BoxOrientation.Flat,
		});

		await user.click(screen.getByRole("button", { name: "box.earlier" }));
		expect(mocks.arrange).toHaveBeenCalledWith({
			storageLocationId: 12,
			items: [
				{ type: StorageItemType.Game, id: 2 },
				{ type: StorageItemType.Game, id: 1 },
			],
		});
	});

	it("opens the cabinet when the address points at one of its shelves", async () => {
		mocks.search = { at: "1:12" };
		renderPage();

		expect(await screen.findByRole("heading", { name: "Billy", level: 2 })).toBeInTheDocument();
	});

	it("lists owned games without a place under Needs a place", async () => {
		renderPage();

		const card = (await screen.findByRole("heading", { name: /inbox.title/ })).closest("div") as HTMLElement;
		expect(within(card).getByRole("checkbox", { name: /Root/ })).toBeInTheDocument();
		expect(within(card).queryByRole("checkbox", { name: /Ark Nova/ })).not.toBeInTheDocument();
	});

	it("finds where a game is and jumps to its cabinet", async () => {
		const user = userEvent.setup();
		renderPage();

		await user.type(await screen.findByRole("combobox", { name: "page.search" }), "pand");
		await user.keyboard("{Enter}");

		expect(mocks.navigate).toHaveBeenCalledWith({ search: { at: "1:11" } });
	});

	it("only shows reading controls to readers", async () => {
		mocks.canWrite = false;
		mocks.search = { at: "1:11" };
		renderPage();

		await screen.findAllByRole("group", { name: "storage:slot.shelf" });
		expect(screen.queryByRole("button", { name: "page.add-here" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: /page.delete/ })).not.toBeInTheDocument();
	});
});
