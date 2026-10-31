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
		return { ...config, useSearch: () => ({}) };
	},
	useNavigate: () => vi.fn(),
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

await import("./table");

const game = (id: number, buyingPrice: number | null) => ({
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
});

describe("Games table filters", () => {
	it("says what each filter filters while it is set to any", () => {
		renderTable();

		const filters = screen.getAllByRole("combobox");
		expect(filters.map((filter) => filter.textContent)).toEqual([
			"games:filters.state-any",
			"games:filters.language-any",
		]);
	});
});

describe("Games table empty collection", () => {
	it("shows the collection empty state with Add your first game instead of blaming the filters", () => {
		mocks.games = [];
		renderTable();

		expect(screen.getByText("dashboard:empty.title")).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "dashboard:empty.button" })).toBeInTheDocument();
		expect(screen.queryByText("games:table.empty")).not.toBeInTheDocument();
		expect(screen.queryAllByRole("combobox")).toHaveLength(0);
	});
});

describe("Games table load error", () => {
	it("shows a retry instead of the empty collection when the games fail to load", async () => {
		mocks.games = [];
		mocks.isError = true;
		renderTable();

		expect(screen.getByText("games:load-error")).toBeInTheDocument();
		expect(screen.queryByText("dashboard:empty.title")).not.toBeInTheDocument();
		await userEvent.click(screen.getByRole("button", { name: "common:retry" }));
		expect(mocks.retry).toHaveBeenCalledOnce();
	});
});

describe("Games table price tiles", () => {
	it("shows a dash instead of a zero price when no owned game has a price", () => {
		renderTable();

		expect(screen.getAllByText("–").length).toBeGreaterThanOrEqual(2);
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

describe("Games table inline editing", () => {
	it("opens the price editor of only the clicked row and closes it again", async () => {
		const user = userEvent.setup();
		mocks.games = [game(1, null), game(2, null)];
		mocks.hideCards = true;
		renderTable();

		const priceCell = (rowIndex: number) =>
			within(within(screen.getByRole("table")).getAllByRole("row")[rowIndex].children[8] as HTMLElement);
		fireEvent.click(priceCell(1).getByRole("button"));

		expect(priceCell(1).getByRole("textbox", { name: "games:columns.price: Game 1" })).toHaveFocus();
		expect(priceCell(2).queryByRole("textbox")).not.toBeInTheDocument();

		await user.keyboard("{Escape}");

		expect(priceCell(1).queryByRole("textbox")).not.toBeInTheDocument();
	});
});
