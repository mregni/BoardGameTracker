import type { FC } from "react";
import { beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { BoxOrientation, GameState, StorageLayout } from "@/models";
import { fireEvent, renderWithProviders, screen, userEvent, within } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	captured: {} as { config: { component: FC } },
	games: [] as unknown[],
	hideCards: false,
	isError: false,
	retry: vi.fn(),
	search: {} as Record<string, unknown>,
	navigate: vi.fn(),
	updateExpansion: vi.fn(),
	updateGame: vi.fn(),
	assignStorage: vi.fn(() => Promise.resolve()),
}));

vi.mock("./-components/GameTableCards", async (importOriginal) => {
	const actual = await importOriginal<typeof import("./-components/GameTableCards")>();
	return {
		...actual,
		GameTableCards: (props: Parameters<typeof actual.GameTableCards>[0]) =>
			mocks.hideCards ? null : <actual.GameTableCards {...props} />,
	};
});

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.config = config;
		return { ...config, useSearch: () => mocks.search };
	},
	useNavigate: () => mocks.navigate,
	Link: ({ children }: { children: React.ReactNode }) => <a href="/">{children}</a>,
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canWrite: true }),
}));

vi.mock("./-hooks/useGamesData", () => ({
	useGamesData: () => ({ games: mocks.games, isLoading: false, isError: mocks.isError, retry: mocks.retry }),
}));

vi.mock("./-hooks/useInlineGameUpdate", () => ({
	useInlineGameUpdate: () => ({ updateGame: mocks.updateGame }),
}));

vi.mock("./-hooks/useInlineExpansionUpdate", () => ({
	useInlineExpansionUpdate: () => ({ updateExpansion: mocks.updateExpansion }),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ currency: "€", uiLanguage: "en-US", dateFormat: "dd-MM-yyyy" }),
	}),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

vi.mock("@/hooks/useStorage", () => ({
	useStorageData: () => ({
		nodes: [
			{
				id: 10,
				name: "Office",
				locationId: 1,
				parentId: null,
				layout: StorageLayout.Free,
				rows: null,
				columns: null,
				row: null,
				column: null,
			},
		],
		locations: [{ id: 1, name: "Home", playCount: 0, gameNightCount: 0, storedCount: 0, lastPlayed: null }],
		isLoading: false,
	}),
	useStorageActions: () => ({ assignStorage: mocks.assignStorage, isPending: false }),
}));

await import("./index");

const game = (id: number, buyingPrice: number | null, playCount = 0) => ({
	id,
	title: `Game ${id}`,
	state: GameState.Owned,
	buyingPrice,
	image: null,
	minPlayers: 2,
	maxPlayers: 4,
	minPlayTime: 45,
	maxPlayTime: 45,
	rating: null,
	weight: null,
	language: null,
	additionDate: null,
	changeDetectionWatchId: null,
	expansions: [],
	categories: [],
	isLoaned: false,
	playCount,
	lastPlayed: null,
	storedAtLocationId: null,
	storageLocationId: null,
	storagePosition: 0,
	boxOrientation: BoxOrientation.Front,
});

const expansion = (id: number, storageLocationId: number | null) => ({
	id,
	title: `Expansion ${id}`,
	bggId: null,
	gameId: 1,
	image: null,
	state: GameState.Owned,
	buyingPrice: null,
	soldPrice: null,
	additionDate: null,
	storedAtLocationId: storageLocationId === null ? null : 1,
	storageLocationId,
	storagePosition: 0,
	boxOrientation: BoxOrientation.Front,
});

const renderTable = () => {
	const Component = mocks.captured.config.component;
	renderWithProviders(<Component />);
};

beforeEach(() => {
	mocks.games = [game(1, null)];
	mocks.hideCards = false;
	mocks.isError = false;
	mocks.retry.mockReset();
	mocks.navigate.mockReset();
	mocks.search = { view: "table" };
});

describe("Games filters", () => {
	it("names every filter while it is set to any, the table filters included", () => {
		renderTable();

		const labels = screen.getAllByRole("combobox").map((filter) => filter.textContent);
		expect(labels).toEqual(
			expect.arrayContaining(["games:filters.category", "games:columns.state", "games:columns.language"]),
		);
	});
});

describe("Games filters on a phone", () => {
	it("keeps the collapsed filters out of the tab order until the toggle opens them", async () => {
		renderTable();

		const toggle = screen.getByRole("button", { name: "filters.show-more" });
		expect(toggle).toHaveAttribute("aria-expanded", "false");
		expect(document.getElementById("games-filters")).toHaveAttribute("inert");

		await userEvent.click(toggle);

		expect(toggle).toHaveAttribute("aria-expanded", "true");
		expect(document.getElementById("games-filters")).not.toHaveAttribute("inert");
	});

	it("labels the search field", () => {
		renderTable();

		expect(screen.getByRole("textbox", { name: "filter-name" })).toBeInTheDocument();
	});
});

describe("Games grid", () => {
	it("opens the pick page with the player, time and category filters", async () => {
		mocks.search = { players: 4, playTime: 60, weight: "light" };
		mocks.games = [{ ...game(1, null), weight: 1.5 }];
		renderTable();

		await userEvent.click(screen.getByRole("button", { name: "pick.button" }));

		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/games/pick", search: { players: 4, playTime: 60 } });
	});

	it("shows the play count on the cards and sorts by most played", () => {
		mocks.search = { sort: "plays" };
		mocks.games = [game(1, null, 0), game(2, null, 7)];
		renderTable();

		const titles = screen.getAllByText(/^Game \d$/).map((title) => title.textContent);
		expect(titles).toEqual(["Game 2", "Game 1"]);
		expect(screen.getByText("plays")).toBeInTheDocument();
		expect(screen.getByText("never-played")).toBeInTheDocument();
	});

	it("switches to the table view through the URL", async () => {
		mocks.search = {};
		renderTable();

		await userEvent.click(screen.getByRole("button", { name: "view.table" }));
		expect(mocks.navigate).toHaveBeenCalledWith(expect.objectContaining({ to: "/games", replace: true }));
		const { search } = mocks.navigate.mock.calls[0][0] as { search: (prev: object) => object };
		expect(search({})).toEqual({ view: "table" });
	});
});

describe("Games table empty collection", () => {
	it("shows the collection empty state with Add your first game instead of blaming the filters", () => {
		mocks.games = [];
		renderTable();

		expect(screen.getByText("empty.title")).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "empty.button" })).toBeInTheDocument();
		expect(screen.queryByText("games:table.empty")).not.toBeInTheDocument();
		expect(screen.queryAllByRole("combobox")).toHaveLength(0);
	});
});

describe("Games table load error", () => {
	it("shows a retry instead of the empty collection when the games fail to load", async () => {
		mocks.games = [];
		mocks.isError = true;
		renderTable();

		expect(screen.getByText("load-error")).toBeInTheDocument();
		expect(screen.queryByText("empty.title")).not.toBeInTheDocument();
		await userEvent.click(screen.getByRole("button", { name: "common:retry" }));
		expect(mocks.retry).toHaveBeenCalledOnce();
	});
});

describe("Games table price summary", () => {
	it("hides the owned price summary when no owned game has a price", () => {
		mocks.games = [game(1, null), game(2, 0)];
		renderTable();

		expect(screen.queryByText(/games:table.total-price-owned/)).not.toBeInTheDocument();
		expect(screen.queryByText("€0")).not.toBeInTheDocument();
	});

	it("shows the total and mean once owned games have prices", async () => {
		mocks.games = [game(1, 40), game(2, 20)];
		renderTable();

		expect(await screen.findByText("€60")).toBeInTheDocument();
		expect(screen.getByText("€30")).toBeInTheDocument();
		expect(screen.queryByText("–")).not.toBeInTheDocument();
	});

	it("collapses an equal play time range to one value", () => {
		renderTable();

		expect(screen.getAllByText("short-duration.minutes").length).toBeGreaterThan(0);
		expect(screen.getAllByText("2–4").length).toBeGreaterThan(0);
	});
});

describe("Games table sorting", () => {
	it("leaves sorting to the page sort control instead of the column headers", () => {
		mocks.hideCards = true;
		renderTable();

		const headers = within(screen.getByRole("table")).getAllByRole("columnheader");
		expect(headers.some((header) => within(header).queryByRole("button"))).toBe(false);
	});
});

describe("Games table inline editing", () => {
	it("opens the price editor of only the clicked row and closes it again", () => {
		mocks.games = [game(1, null), game(2, null)];
		mocks.hideCards = true;
		renderTable();

		const priceCell = (rowIndex: number) =>
			within(within(screen.getByRole("table")).getAllByRole("row")[rowIndex].children[10] as HTMLElement);
		fireEvent.click(priceCell(1).getByRole("button"));

		expect(priceCell(1).getByRole("textbox", { name: "games:columns.price: Game 1" })).toHaveFocus();
		expect(priceCell(2).queryByRole("textbox")).not.toBeInTheDocument();

		fireEvent.keyDown(priceCell(1).getByRole("textbox"), { key: "Escape" });

		expect(priceCell(1).queryByRole("textbox")).not.toBeInTheDocument();
	});
});

describe("Games table with expansions", () => {
	const expansion = (id: number, title: string, state: GameState, buyingPrice: number | null) => ({
		id,
		title,
		state,
		buyingPrice,
		bggId: 300 + id,
		gameId: 1,
		image: null,
		soldPrice: null,
		additionDate: null,
	});
	const catan = () => ({
		...game(1, 35),
		title: "Catan",
		expansions: [expansion(7, "Seafarers", GameState.Owned, 30), expansion(8, "Traders", GameState.Wanted, 25)],
	});
	const table = () => within(screen.getByRole("table"));

	it("shows the collection as one row with the base game and owned expansions together", async () => {
		mocks.games = [catan()];
		mocks.hideCards = true;
		renderTable();

		expect(await within(await screen.findByRole("table")).findByText("€65")).toBeInTheDocument();
		expect(table().getByText("games:table.collection-price")).toBeInTheDocument();
		expect(table().queryByRole("button", { name: "games:columns.price: Catan" })).not.toBeInTheDocument();
		expect(table().queryByText("Seafarers")).not.toBeInTheDocument();
		expect(screen.getAllByText("€65", { selector: "span.font-bold" })).toHaveLength(2);
	});

	it("opens the collection into a base game row and a row per expansion", async () => {
		mocks.games = [catan()];
		mocks.hideCards = true;
		renderTable();
		await screen.findAllByText("€65");

		await userEvent.click(table().getByRole("button", { name: "games:table.toggle-expansions" }));

		expect(table().getByRole("button", { name: "games:table.toggle-expansions" })).toHaveAttribute(
			"aria-expanded",
			"true",
		);
		expect(table().getByText("games:table.base-tag")).toBeInTheDocument();
		expect(table().getAllByText("games:table.expansion-tag")).toHaveLength(2);
		expect(table().getByText("Seafarers")).toBeInTheDocument();
		expect(table().getByText("€30")).toBeInTheDocument();
	});

	it("saves a new price of an expansion from its row", async () => {
		mocks.games = [catan()];
		mocks.hideCards = true;
		renderTable();
		await screen.findAllByText("€65");
		await userEvent.click(table().getByRole("button", { name: "games:table.toggle-expansions" }));

		const row = table().getByText("Seafarers").closest("tr") as HTMLElement;
		fireEvent.click(within(row.children[10] as HTMLElement).getByRole("button"));
		const input = table().getByRole("textbox", { name: "games:columns.price: Seafarers" });
		fireEvent.change(input, { target: { value: "32" } });
		fireEvent.keyDown(input, { key: "Enter" });

		expect(mocks.updateExpansion).toHaveBeenCalledWith(1, expect.objectContaining({ id: 7, buyingPrice: 32 }));
	});
});

describe("Games storage", () => {
	beforeAll(() => {
		Element.prototype.hasPointerCapture = vi.fn(() => false);
		Element.prototype.releasePointerCapture = vi.fn();
		Element.prototype.scrollIntoView = vi.fn();
	});

	const lastSearch = () => {
		const call = mocks.navigate.mock.calls.at(-1)?.[0] as { search: (prev: object) => object };
		return call.search(mocks.search);
	};

	it("shows an expansion stored in the filtered place under its greyed-out base game in the table", () => {
		mocks.search = { view: "table", storage: "1:10" };
		mocks.games = [
			{ ...game(1, null), storedAtLocationId: 1, storageLocationId: null, expansions: [expansion(5, 10)] },
			{ ...game(2, null), storedAtLocationId: 1, storageLocationId: 10 },
		];
		mocks.hideCards = true;
		renderTable();

		expect(screen.getByText("count")).toBeInTheDocument();
		expect(screen.getAllByText("Game 1").length).toBeGreaterThan(0);
		expect(screen.getByText(/stored-elsewhere/)).toBeInTheDocument();
		expect(screen.getByText("Expansion 5")).toBeInTheDocument();
		expect(screen.getAllByRole("checkbox", { name: /table.select$/ })[0]).toBeDisabled();
	});

	it("lists only owned games without a place for the no-storage filter", () => {
		mocks.search = { storage: "none" };
		mocks.games = [
			game(1, null),
			{ ...game(2, null), storedAtLocationId: 1 },
			{ ...game(3, null), state: GameState.Wanted },
		];
		renderTable();

		expect(screen.getByText("Game 1")).toBeInTheDocument();
		expect(screen.queryByText("Game 2")).not.toBeInTheDocument();
		expect(screen.queryByText("Game 3")).not.toBeInTheDocument();
	});

	it("switches the cards to base games and expansions when a storage location is picked", async () => {
		mocks.search = {};
		renderTable();

		await userEvent.click(screen.getByRole("combobox", { name: "games:filters.storage.label" }));
		await userEvent.click(await screen.findByRole("option", { name: /Home › Office/ }));

		expect(lastSearch()).toEqual({ storage: "1:10", type: "any" });
	});

	it("opens a quick view for an expansion card", async () => {
		mocks.search = { storage: "1:10", type: "any" };
		mocks.games = [{ ...game(1, null), storedAtLocationId: 1, expansions: [expansion(5, 10)] }];
		renderTable();

		const card = screen.getByRole("button", { name: /Expansion 5/ });
		expect(screen.queryByText("Game 1")).not.toBeInTheDocument();

		await userEvent.click(card);

		expect(await screen.findByRole("dialog", { name: /Expansion 5/ })).toBeInTheDocument();
	});

	it("moves the selected games to a storage location", async () => {
		mocks.search = { view: "table" };
		mocks.hideCards = true;
		mocks.games = [game(1, null), game(2, null)];
		renderTable();

		fireEvent.click(screen.getAllByRole("checkbox", { name: /table.select$/ })[1]);
		const selection = screen.getByRole("region", { name: /table.selection/ });
		await userEvent.click(within(selection).getByRole("combobox", { name: /move-to-storage/ }));
		await userEvent.click(await screen.findByRole("option", { name: /Home › Office/ }));

		expect(mocks.assignStorage).toHaveBeenCalledWith({
			items: [{ type: "game", id: 2 }],
			locationId: 1,
			storageLocationId: 10,
		});
	});
});
