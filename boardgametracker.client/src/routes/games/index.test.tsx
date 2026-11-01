import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { GameState } from "@/models";
import { fireEvent, renderWithProviders, screen, userEvent, within } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	captured: {} as { config: { component: FC } },
	games: [] as unknown[],
	hideCards: false,
	isError: false,
	retry: vi.fn(),
	search: {} as Record<string, unknown>,
	navigate: vi.fn(),
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
	useInlineGameUpdate: () => ({ updateGame: vi.fn() }),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ currency: "€", uiLanguage: "en-US", dateFormat: "dd-MM-yyyy" }),
	}),
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
			within(within(screen.getByRole("table")).getAllByRole("row")[rowIndex].children[8] as HTMLElement);
		fireEvent.click(priceCell(1).getByRole("button"));

		expect(priceCell(1).getByRole("textbox", { name: "games:columns.price: Game 1" })).toHaveFocus();
		expect(priceCell(2).queryByRole("textbox")).not.toBeInTheDocument();

		fireEvent.keyDown(priceCell(1).getByRole("textbox"), { key: "Escape" });

		expect(priceCell(1).queryByRole("textbox")).not.toBeInTheDocument();
	});
});
