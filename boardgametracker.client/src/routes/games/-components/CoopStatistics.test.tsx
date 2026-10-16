import { describe, expect, it, vi } from "vitest";
import { PlayerStatisticsGrid } from "@/routes/players/-components/PlayerStatisticsGrid";
import { renderWithProviders, screen } from "@/test/test-utils";
import { GameStatisticsGrid } from "./GameStatisticsGrid";

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
});

describe("co-op statistics", () => {
	it("shows the team win rate for a game with co-op sessions", () => {
		renderWithProviders(<GameStatisticsGrid gameStats={gameStats(3, 1)} currency="EUR" />);

		expect(screen.getByText("team-win-rate")).toBeInTheDocument();
		expect(screen.getByText(/^75/)).toBeInTheDocument();
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
