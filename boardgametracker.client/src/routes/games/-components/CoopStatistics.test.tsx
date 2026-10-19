import { describe, expect, it, vi } from "vitest";
import { PlayerStatisticsGrid } from "@/routes/players/-components/PlayerStatisticsGrid";
import { renderWithProviders, screen } from "@/test/test-utils";
import { GameStatisticsGrid, statisticsGridColumns } from "./GameStatisticsGrid";

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({ queryKey: ["settings"], queryFn: async () => ({ uiLanguage: "en-US" }) }),
}));

const gameStats = (coopWinCount: number, coopLossCount: number) => ({
	playCount: 4,
	totalPlayedTime: 240,
	pricePerPlay: null,
	highScore: null,
	averageScore: null,
	averagePlayTime: 60,
	lastPlayed: null,
	coopWinCount,
	coopLossCount,
});

const playerStats = (coopWinCount: number, coopLossCount: number) => ({
	playCount: 6,
	totalPlayedTime: 300,
	winCount: 3,
	distinctGameCount: 2,
	coopWinCount,
	coopLossCount,
	mostPlayedGames: [],
});

describe("game statistics grid", () => {
	it("picks columns that leave no tile alone on a row", () => {
		expect(statisticsGridColumns(2)).toBe("md:grid-cols-2");
		expect(statisticsGridColumns(3)).toBe("md:grid-cols-3");
		expect(statisticsGridColumns(4)).toBe("md:grid-cols-2 xl:grid-cols-4");
		expect(statisticsGridColumns(5)).toBe("md:grid-cols-3 xl:grid-cols-5");
		expect(statisticsGridColumns(6)).toBe("md:grid-cols-3 2xl:grid-cols-6");
		expect(statisticsGridColumns(7)).toBe("md:grid-cols-4");
	});

	it("lays four tiles out as two by two, then four in a row", async () => {
		const stats = { ...gameStats(0, 0), pricePerPlay: 12.5, lastPlayed: "2026-10-01T10:00:00Z" };
		renderWithProviders(<GameStatisticsGrid gameStats={stats} currency="EUR" hasScoringResults />);

		await screen.findByText("last-played");
		expect(screen.getByTestId("game-statistics-grid")).toHaveClass("md:grid-cols-2", "xl:grid-cols-4");
	});
});

describe("co-op statistics", () => {
	it("shows the team win rate for a game with co-op sessions", () => {
		renderWithProviders(<GameStatisticsGrid gameStats={gameStats(3, 1)} currency="EUR" />);

		expect(screen.getByText("team-win-rate")).toBeInTheDocument();
		expect(screen.getByText(/^75/)).toBeInTheDocument();
		expect(screen.getByText("team-win-rate-count")).toBeInTheDocument();
	});

	it("hides the team win rate below three co-op sessions", () => {
		renderWithProviders(<GameStatisticsGrid gameStats={gameStats(1, 0)} currency="EUR" />);

		expect(screen.queryByText("team-win-rate")).not.toBeInTheDocument();
	});

	it("leaves the high and average score to the scoring results card when it is shown", () => {
		const scored = { ...gameStats(0, 0), highScore: 104, averageScore: 71.2 };
		const { unmount } = renderWithProviders(<GameStatisticsGrid gameStats={scored} currency="EUR" hasScoringResults />);

		expect(screen.queryByText("high-score")).not.toBeInTheDocument();
		expect(screen.queryByText("average-score")).not.toBeInTheDocument();
		unmount();

		renderWithProviders(<GameStatisticsGrid gameStats={scored} currency="EUR" />);
		expect(screen.getByText("high-score")).toBeInTheDocument();
		expect(screen.getByText("average-score")).toBeInTheDocument();
	});

	it("leaves the team win rate out for a game without co-op sessions", () => {
		renderWithProviders(<GameStatisticsGrid gameStats={gameStats(0, 0)} currency="EUR" />);

		expect(screen.queryByText("team-win-rate")).not.toBeInTheDocument();
	});

	it("shows the co-op wins and losses of a player who played co-op", () => {
		renderWithProviders(<PlayerStatisticsGrid statistics={playerStats(2, 1)} />);

		expect(screen.getByText("coop-won-lost")).toBeInTheDocument();
		expect(screen.getByText("coop-results")).toBeInTheDocument();
	});

	it("leaves the co-op stat out for a player who never played co-op", () => {
		renderWithProviders(<PlayerStatisticsGrid statistics={playerStats(0, 0)} />);

		expect(screen.queryByText("coop-won-lost")).not.toBeInTheDocument();
	});
});
