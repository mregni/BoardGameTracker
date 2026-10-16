import type { FC, ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Leaderboard, LeaderboardEntry, LeaderboardParams } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor, within } from "@/test/test-utils";
import type { LeaderboardSearch } from "./-utils/leaderboardSearch";

const mocks = vi.hoisted(() => ({
	captured: {} as { config: { component: FC } },
	search: {} as LeaderboardSearch,
	navigate: vi.fn(),
	getLeaderboardCall: vi.fn(),
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.config = config;
		return { ...config, fullPath: "/leaderboard/", useSearch: () => mocks.search };
	},
	useNavigate: () => mocks.navigate,
	Link: ({
		to,
		params,
		children,
		...rest
	}: {
		to: string;
		params?: { playerId: number };
		children: ReactNode;
		className?: string;
	}) => (
		<a href={to.replace("$playerId", String(params?.playerId))} {...rest}>
			{children}
		</a>
	),
}));

vi.mock("@/services/leaderboardService", () => ({
	getLeaderboardCall: mocks.getLeaderboardCall,
}));

vi.mock("@/services/queries/games", () => ({
	getGames: () => ({
		queryKey: ["games"],
		queryFn: () => Promise.resolve([{ id: 12, title: "Azul", image: null }]),
	}),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ dateFormat: "yyyy-MM-dd", timeFormat: "HH:mm", uiLanguage: "en-us" }),
	}),
}));

await import("./index");

const entry = (playerId: number, name: string, overrides: Partial<LeaderboardEntry> = {}): LeaderboardEntry => ({
	rank: playerId,
	playerId,
	name,
	image: null,
	playCount: 10,
	winCount: 4,
	podiumCount: 1,
	winPercentage: 40,
	minutesPlayed: 120,
	isRanked: true,
	sessionsToRank: 0,
	...overrides,
});

const byWins = [
	entry(1, "Mikhaël", { winCount: 54, playCount: 142 }),
	entry(2, "Kathleen", { winCount: 48, playCount: 118 }),
	entry(3, "reggi", { winCount: 31, playCount: 96 }),
	entry(4, "Anna", { winCount: 15, playCount: 41 }),
];

const leaderboard = (players: LeaderboardEntry[], overrides: Partial<Leaderboard> = {}): Leaderboard => {
	const ranked = players.filter((x) => x.isRanked);
	return {
		mostWins: players[0] ?? null,
		mostPlays: players[0] ?? null,
		bestWinRate: ranked[0] ?? null,
		mostTimePlayed: players[0] ?? null,
		minimumSessions: 5,
		minimumPlayersForPodium: 4,
		rankBy: "wins",
		showPodium: ranked.length >= 4,
		rankedCount: ranked.length,
		winCount: 0,
		coopWinCount: 0,
		players,
		...overrides,
	};
};

const renderPage = () => {
	const Component = mocks.captured.config.component;
	return renderWithProviders(<Component />);
};

const rankingNames = async () => {
	const ranking = await screen.findByRole("region", { name: /^ranking\.label/ });
	return within(ranking)
		.getAllByTestId("leaderboard-row")
		.map((row) => row.querySelector("strong")?.textContent);
};

describe("Leaderboard page", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.search = {};
	});

	it("should ask for the default filters and show the podium with four ranked players", async () => {
		mocks.getLeaderboardCall.mockResolvedValue(leaderboard(byWins));

		renderPage();

		expect(await screen.findByRole("region", { name: "ranking.podium" })).toBeInTheDocument();
		expect(screen.queryByRole("note")).not.toBeInTheDocument();
		expect(mocks.getLeaderboardCall).toHaveBeenCalledWith({ minSessions: 5, rankBy: "wins" });
		expect(await rankingNames()).toEqual(["Mikhaël", "Kathleen", "reggi", "Anna"]);
	});

	it.each([
		["winRate", ["Kathleen", "Anna", "Mikhaël", "reggi"]],
		["sessions", ["Mikhaël", "Kathleen", "reggi", "Anna"]],
	] as const)("should ask the server to rank by %s and keep its order", async (rank, order) => {
		mocks.search = { rank };
		mocks.getLeaderboardCall.mockImplementation((params: LeaderboardParams) =>
			Promise.resolve(
				leaderboard(
					order.map((name, index) => entry(index + 1, name)),
					{ rankBy: params.rankBy },
				),
			),
		);

		renderPage();

		expect(await rankingNames()).toEqual(order);
		expect(mocks.getLeaderboardCall).toHaveBeenCalledWith({ minSessions: 5, rankBy: rank });
	});

	it("should explain why there is no podium with fewer than four ranked players for a game", async () => {
		mocks.search = { game: 12, range: "custom", from: "2026-01-01", to: "2026-06-30", min: 3 };
		mocks.getLeaderboardCall.mockResolvedValue(
			leaderboard([
				...byWins.slice(0, 3),
				entry(9, "Bram", { rank: null, isRanked: false, playCount: 2, sessionsToRank: 1 }),
			]),
		);

		renderPage();

		await waitFor(() => expect(screen.getByRole("note")).toHaveTextContent("no-podium.game"));
		expect(screen.getByRole("note")).toHaveTextContent("no-podium.include-one");
		expect(screen.queryByRole("region", { name: "ranking.podium" })).not.toBeInTheDocument();
		expect(screen.getByRole("heading", { name: "ranking.not-ranked" })).toBeInTheDocument();
		expect(mocks.getLeaderboardCall).toHaveBeenCalledWith({
			from: "2026-01-01",
			to: "2026-06-30",
			gameId: 12,
			minSessions: 3,
			rankBy: "wins",
		});
	});

	it("should suggest a lower minimum for several unranked players on all games", async () => {
		mocks.getLeaderboardCall.mockResolvedValue(
			leaderboard([
				byWins[0],
				entry(8, "Lou", { rank: null, isRanked: false }),
				entry(9, "Bram", { rank: null, isRanked: false }),
			]),
		);

		renderPage();

		const note = await screen.findByRole("note");
		expect(note).toHaveTextContent("no-podium.all");
		expect(note).toHaveTextContent("no-podium.include-more");
	});

	it("should say nobody is ranked yet when everyone is below the minimum", async () => {
		mocks.getLeaderboardCall.mockResolvedValue(leaderboard([entry(9, "Bram", { rank: null, isRanked: false })]));

		renderPage();

		const note = await screen.findByRole("note");
		expect(note).toHaveTextContent("no-podium.none");
		expect(note).toHaveTextContent("no-podium.include-one");
	});

	it("should link the podium, the tiles and every row to the player", async () => {
		mocks.getLeaderboardCall.mockResolvedValue(leaderboard(byWins));

		renderPage();

		await screen.findByRole("region", { name: "ranking.podium" });
		const hrefs = screen.getAllByRole("link").map((link) => link.getAttribute("href"));
		expect(hrefs.every((href) => href?.startsWith("/players/"))).toBe(true);
		expect(hrefs).toHaveLength(4 + 3 + 4);
	});

	it("should update the search when a filter changes", async () => {
		mocks.getLeaderboardCall.mockResolvedValue(leaderboard(byWins));

		renderPage();

		await userEvent.click(await screen.findByRole("button", { name: "filters.last-12-months" }));

		expect(mocks.navigate).toHaveBeenCalledWith({ search: expect.any(Function), replace: true });
		const [{ search }] = mocks.navigate.mock.calls[0];
		expect(search({ game: 12 })).toEqual({ game: 12, range: "12m", from: undefined, to: undefined });
	});

	it("should show the empty page when nothing was played yet", async () => {
		mocks.getLeaderboardCall.mockResolvedValue(leaderboard([]));

		renderPage();

		expect(await screen.findByText("empty.title")).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "filters.all-time" })).not.toBeInTheDocument();
	});

	it("should keep the filters when nothing matches them", async () => {
		mocks.search = { range: "year" };
		mocks.getLeaderboardCall.mockResolvedValue(leaderboard([]));

		renderPage();

		expect(await screen.findByText("empty.filtered-title")).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "filters.all-time" })).toBeInTheDocument();
	});
});
