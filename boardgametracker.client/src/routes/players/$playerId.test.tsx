import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	captured: {} as { config: { component: FC } },
	playerData: {} as Record<string, unknown>,
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.config = config;
		return { ...config, useParams: () => ({ playerId: 1 }) };
	},
	useNavigate: () => vi.fn(),
	Link: ({ children }: { children: React.ReactNode }) => <a href="/">{children}</a>,
}));

vi.mock("@/hooks/usePermissions", () => ({ usePermissions: () => ({ canWrite: true }) }));
vi.mock("./-hooks/usePlayerData", () => ({ silent: { silent: true }, usePlayerData: () => mocks.playerData }));
vi.mock("./-hooks/usePlayerActions", () => ({ usePlayerActions: () => ({ handleBackToPlayers: vi.fn() }) }));
vi.mock("../-hooks/useGameById", () => ({ useGameById: () => ({ gameById: () => undefined }) }));
vi.mock("../-hooks/usePlayerById", () => ({
	usePlayerById: () => ({ playerById: (id: number) => (id === 2 ? { id: 2, name: "Ben" } : undefined) }),
}));

const stub = (name: string) => (props: Record<string, unknown>) => (
	<div data-testid={name}>{JSON.stringify(props.nemesis ?? props.rivals ?? null)}</div>
);

vi.mock("./-components/PlayerHeader", () => ({ PlayerHeader: stub("header") }));
vi.mock("./-components/PlayerStatisticsGrid", () => ({ PlayerStatisticsGrid: stub("stats") }));
vi.mock("./-components/RecentPlayerSessionsCard", () => ({ RecentPlayerSessionsCard: stub("recent") }));
vi.mock("./-components/PlayerRivalsCard", () => ({ PlayerRivalsCard: stub("rivals") }));
vi.mock("./-components/MostPlayedGamesCard", () => ({ MostPlayedGamesCard: stub("most-played") }));
vi.mock("./-components/PlayerAchievementsCard", () => ({ PlayerAchievementsCard: stub("achievements") }));
vi.mock("../-components/BgtPoster", () => ({ BgtPoster: stub("poster") }));
vi.mock("../-modals/BgtDeleteModal", () => ({ BgtDeleteModal: () => null }));
vi.mock("./-modals/EditPlayerModal", () => ({ EditPlayerModal: () => null }));

await import("./$playerId");

const player = { id: 1, name: "Kathleen", image: null, badges: [] };
const settings = { dateFormat: "dd-MM-yy", uiLanguage: "en-US" };
const statistics = (playCount: number) => ({
	playCount,
	mostPlayedGames: [],
	rivals: [{ playerId: 2, sessions: 4, wins: 1, losses: 3 }],
});

const setData = (overrides: Record<string, unknown> = {}) => {
	mocks.playerData = {
		player,
		statistics: statistics(4),
		badges: [],
		settings,
		sessions: [],
		isLoading: false,
		error: undefined,
		retry: vi.fn(),
		deletePlayer: vi.fn(),
		...overrides,
	};
};

const renderPage = () => {
	const Component = mocks.captured.config.component;
	return renderWithProviders(<Component />);
};

describe("Player detail page", () => {
	beforeEach(() => {
		document.title = "BoardGameTracker";
		setData();
	});

	it("shows the record, rivals and nemesis and names the tab after the player", () => {
		renderPage();

		for (const id of ["header", "stats", "recent", "rivals", "most-played", "achievements"]) {
			expect(screen.getByTestId(id)).toBeInTheDocument();
		}
		expect(screen.getByTestId("stats")).toHaveTextContent('"name":"Ben"');
		expect(screen.queryByTestId("poster")).not.toBeInTheDocument();
		expect(document.title).toBe("Kathleen · BoardGameTracker");
	});

	it("shows the photo beside the page for a player with a picture", () => {
		setData({ player: { ...player, image: "kathleen.png" } });
		renderPage();

		expect(screen.getByTestId("poster")).toBeInTheDocument();
	});

	it("invites the first session for a player without sessions", () => {
		setData({ statistics: statistics(0) });
		renderPage();

		expect(screen.getByRole("heading", { level: 2, name: "no-sessions.title" })).toBeInTheDocument();
		expect(screen.queryByTestId("stats")).not.toBeInTheDocument();
	});

	it("gives a missing player a page heading and tab title", () => {
		setData({ player: undefined, statistics: undefined, error: { kind: "client", status: 404, message: "Not found" } });
		renderPage();

		expect(screen.getByRole("heading", { level: 1, name: "missing.title" })).toBeInTheDocument();
		expect(document.title).toBe("missing.title · BoardGameTracker");
	});
});
