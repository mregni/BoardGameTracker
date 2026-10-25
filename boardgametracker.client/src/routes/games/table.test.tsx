import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { GameState } from "@/models";
import { renderWithProviders, screen } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	captured: {} as { config: { component: FC } },
	games: [] as unknown[],
}));

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
	useGamesData: () => ({ games: mocks.games, isLoading: false }),
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
