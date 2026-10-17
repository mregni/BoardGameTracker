import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { type DashboardStatistics, GameState } from "@/models";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	captured: {} as { Page: FC },
	navigate: vi.fn(),
	data: {} as { statistics?: unknown; settings?: unknown },
	canWrite: true,
}));

vi.mock("@tanstack/react-router", async () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.Page = config.component;
		return config;
	},
	useNavigate: () => mocks.navigate,
	Link: (await import("@/test/routerLinkMock")).RouterLinkMock,
}));

vi.mock("./-hooks/useDashboardData", () => ({
	useDashboardData: () => mocks.data,
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canWrite: mocks.canWrite }),
}));

await import("./index");

const statistics: DashboardStatistics = {
	totalGames: 52,
	ownedGames: 41,
	activePlayers: 6,
	sessionsPlayed: 168,
	totalPlayedTime: 9500,
	totalCollectionValue: 1240,
	pricedGameCount: 31,
	expansionsOwned: 4,
	avgSessionTime: 57,
	sessionsThisMonth: 14,
	activePlayersThisMonth: 5,
	linkedPlayer: { id: 1, name: "Mikhaël", sessionsThisMonth: 9, winsThisMonth: 4 },
	nextGameNight: {
		id: 3,
		title: "Strategy night",
		startDate: "2026-10-23T19:30:00",
		locationName: "Thuis",
		hostId: 1,
		hostName: "Mikhaël",
		acceptedCount: 4,
		maybeCount: 0,
		pendingCount: 0,
		declinedCount: 0,
		suggestedGames: [],
	},
	plannedGameNights: 1,
	shelfOfShame: { count: 9, neverPlayedCount: 5, totalValue: 365 },
	recentActivities: [],
	collection: [{ type: GameState.Owned, gameCount: 41 }],
	mostPlayedGames: [],
	mostPlayedGamesThisMonth: [],
	topPlayers: [],
	topPlayersIncludeCoopWins: false,
	recentAddedGames: [],
};

const settings = {
	uiLanguage: "en-US",
	timeFormat: "HH:mm",
	dateFormat: "dd-MM-yyyy",
	currency: "EUR",
	gameNightsEnabled: true,
	shelfOfShameEnabled: true,
};

describe("Dashboard page", () => {
	beforeEach(() => {
		mocks.data = { statistics, settings };
		mocks.canWrite = true;
		mocks.navigate.mockReset();
	});

	it("renders nothing while the data loads", () => {
		mocks.data = {};
		const { container } = renderWithProviders(<mocks.captured.Page />);

		expect(container).toBeEmptyDOMElement();
	});

	it("shows the empty page without games and sends the user to add a game", async () => {
		mocks.data = { statistics: { ...statistics, totalGames: 0 }, settings };
		renderWithProviders(<mocks.captured.Page />);

		expect(screen.getByText("empty.title")).toBeInTheDocument();
		await userEvent.click(screen.getByRole("button", { name: "empty.button" }));
		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/games/add" });
	});

	it("shows the welcome, the next night, the tiles and the cards", () => {
		renderWithProviders(<mocks.captured.Page />);

		expect(screen.getByRole("heading", { level: 1 })).toHaveTextContent("welcome.title");
		expect(screen.getByTestId("next-game-night")).toBeInTheDocument();
		expect(screen.getByRole("link", { name: /tiles.games/ })).toHaveAttribute("href", "/games");
		expect(screen.getByRole("link", { name: /tiles.players/ })).toHaveAttribute("href", "/players");
		expect(screen.queryByRole("link", { name: /tiles.sessions/ })).not.toBeInTheDocument();
		expect(screen.getByText("tiles.play-time-detail")).toBeInTheDocument();
		expect(screen.getByRole("link", { name: /shame.title/ })).toHaveAttribute("href", "/shames");
		expect(screen.getByText("top-players.title")).toBeInTheDocument();
		expect(screen.getByText("most-played.title")).toBeInTheDocument();
		expect(screen.getByText("collection.title")).toBeInTheDocument();
		expect(screen.queryByText("charts.collection")).not.toBeInTheDocument();
	});

	it("hides the night card and the shelf nudge when those features are off", () => {
		mocks.data = { statistics, settings: { ...settings, gameNightsEnabled: false, shelfOfShameEnabled: false } };
		renderWithProviders(<mocks.captured.Page />);

		expect(screen.queryByTestId("next-game-night")).not.toBeInTheDocument();
		expect(screen.queryByText("night.none")).not.toBeInTheDocument();
		expect(screen.queryByText(/shame.title/)).not.toBeInTheDocument();
	});

	it("leaves out the average session time before the first session", () => {
		mocks.data = { statistics: { ...statistics, sessionsPlayed: 0, totalPlayedTime: 0 }, settings };
		renderWithProviders(<mocks.captured.Page />);

		expect(screen.queryByText("tiles.play-time-detail")).not.toBeInTheDocument();
	});
});
