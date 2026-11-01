import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { GameState } from "@/models";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	captured: {} as { config: { component: FC } },
	gameData: {} as Record<string, unknown>,
	retry: vi.fn(),
	handleBackToGames: vi.fn(),
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.config = config;
		return { ...config, useParams: () => ({ gameId: 7 }) };
	},
	useNavigate: () => vi.fn(),
	Link: ({ children }: { children: React.ReactNode }) => <a href="/">{children}</a>,
}));

vi.mock("@/hooks/usePermissions", () => ({ usePermissions: () => ({ canWrite: true }) }));
vi.mock("./-hooks/useGameData", () => ({ useGameData: () => mocks.gameData }));
vi.mock("./-hooks/useGameManuals", () => ({ useGameManuals: () => ({ manuals: [] }) }));
vi.mock("./-hooks/useInlineGameUpdate", () => ({ useInlineGameUpdate: () => ({ updateGame: vi.fn() }) }));
vi.mock("./-hooks/useGameActions", () => ({
	useGameActions: () => ({ handleBackToGames: mocks.handleBackToGames, handleAddSession: vi.fn() }),
}));
vi.mock("../-hooks/usePlayerById", () => ({
	usePlayerById: () => ({ playerById: (id?: number) => (id === 1 ? { id: 1, name: "Kathleen" } : undefined) }),
}));
vi.mock("@/services/loanService", () => ({
	getLoansCall: () =>
		Promise.resolve([{ id: 3, gameId: 7, playerId: 1, isActive: true, loanDate: new Date(2026, 9, 1) }]),
}));

const stub = (name: string) => (props: Record<string, unknown>) => (
	<div data-testid={name}>{JSON.stringify(props.loan ?? null)}</div>
);

vi.mock("./-components/GameHeader", () => ({ GameHeader: stub("header") }));
vi.mock("./-components/GameFactsCard", () => ({ GameFactsCard: stub("facts") }));
vi.mock("./-components/GameStaticSection", () => ({ GameStaticSection: stub("static") }));
vi.mock("./-components/GameStatisticsGrid", () => ({ GameStatisticsGrid: stub("stats") }));
vi.mock("./-components/RecentSessionsCard", () => ({ RecentSessionsCard: stub("recent") }));
vi.mock("./-components/ScoringResultsCard", () => ({ ScoringResultsCard: stub("scoring") }));
vi.mock("./-components/SessionCountChartCard", () => ({ SessionCountChartCard: stub("by-day") }));
vi.mock("./-components/PlayerCountChartCard", () => ({ PlayerCountChartCard: stub("by-players") }));
vi.mock("./-components/TopPlayersCard", () => ({ TopPlayersCard: stub("top-players") }));
vi.mock("./-components/TrackPriceDialog", () => ({ TrackPriceDialog: () => null }));
vi.mock("./-components/ManualsDialog", () => ({ ManualsDialog: () => null }));
vi.mock("./-components/ExpansionsDialog", () => ({ ExpansionsDialog: () => null }));
vi.mock("../-modals/BgtDeleteModal", () => ({ BgtDeleteModal: () => null }));
vi.mock("./-modals/ExpansionSelectorModal", () => ({ ExpansionSelectorModal: () => null }));

await import("./$gameId");

const game = {
	id: 7,
	title: "Agricola",
	image: null,
	state: GameState.Owned,
	isLoaned: false,
	expansions: [],
	changeDetectionWatchId: null,
	shopUrl: null,
};
const settings = { currency: "EUR", dateFormat: "dd-MM-yy", uiLanguage: "en-US", ragEnabled: false };
const statistics = (playCount: number) => ({
	gameStats: { playCount },
	topPlayers: [],
	scoreRankChart: [],
	playByDayChart: [],
	playerCountChart: [],
});

const setData = (overrides: Record<string, unknown> = {}) => {
	mocks.gameData = {
		game,
		settings,
		statistics: statistics(3),
		sessions: [],
		isLoading: false,
		loadFailed: false,
		isNotFound: false,
		detailsFailed: false,
		retry: mocks.retry,
		...overrides,
	};
};

const renderPage = () => {
	const Component = mocks.captured.config.component;
	return renderWithProviders(<Component />);
};

describe("Game detail page", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		document.title = "BoardGameTracker";
		setData();
	});

	it("shows the play history of a played game and names the tab after it", () => {
		renderPage();

		for (const id of ["header", "static", "stats", "recent", "scoring", "top-players", "facts"]) {
			expect(screen.getByTestId(id)).toBeInTheDocument();
		}
		expect(document.title).toBe("Agricola · BoardGameTracker");
	});

	it("invites the first play for a game without sessions", () => {
		setData({ statistics: statistics(0) });
		renderPage();

		expect(screen.getByText("no-sessions.title")).toBeInTheDocument();
		expect(screen.queryByTestId("stats")).not.toBeInTheDocument();
		expect(screen.queryByTestId("top-players")).not.toBeInTheDocument();
	});

	it("shows a spinner while the statistics are loading", () => {
		setData({ statistics: undefined, sessions: undefined });
		renderPage();

		expect(screen.getByRole("status")).toBeInTheDocument();
	});

	it("offers a retry when the statistics failed to load", async () => {
		setData({ statistics: undefined, detailsFailed: true });
		renderPage();

		await userEvent.click(screen.getByRole("button", { name: "load-error.retry" }));
		expect(mocks.retry).toHaveBeenCalledTimes(1);
	});

	it("names the borrower of a loaned game", async () => {
		setData({ game: { ...game, isLoaned: true } });
		renderPage();

		expect(await screen.findByText(/Kathleen/)).toBeInTheDocument();
		expect(screen.getByTestId("header")).toHaveTextContent("01-10-26");
	});

	it("gives a missing game a page heading, tab title and a way back", async () => {
		setData({ game: undefined, loadFailed: true, isNotFound: true });
		renderPage();

		expect(screen.getByRole("heading", { level: 1, name: "game:notifications.not-found" })).toBeInTheDocument();
		expect(document.title).toBe("game:notifications.not-found · BoardGameTracker");
		await userEvent.click(screen.getByRole("button", { name: "game:load-error.back" }));
		expect(mocks.handleBackToGames).toHaveBeenCalledTimes(1);
	});

	it("offers a retry when the game failed to load", async () => {
		setData({ game: undefined, loadFailed: true });
		renderPage();

		await userEvent.click(screen.getByRole("button", { name: "game:load-error.retry" }));
		expect(mocks.retry).toHaveBeenCalledTimes(1);
	});
});
