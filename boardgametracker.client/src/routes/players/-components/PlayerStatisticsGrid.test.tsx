import { describe, expect, it } from "vitest";
import type { Settings } from "@/models";
import { renderWithProviders, screen } from "@/test/test-utils";
import { PlayerStatisticsGrid } from "./PlayerStatisticsGrid";

const settings = { dateFormat: "dd-MM-yyyy", uiLanguage: "en-US" } as Settings;

const statistics = {
	playCount: 25,
	totalPlayedTime: 1454,
	winCount: 12,
	distinctGameCount: 6,
	coopWinCount: 0,
	coopLossCount: 0,
	mostPlayedGames: [
		{ id: 1, image: "", title: "Wingspan", totalWins: 4, totalSessions: 9, winningPercentage: 44 },
		{ id: 2, image: "", title: "Azul", totalWins: 2, totalSessions: 5, winningPercentage: 40 },
	],
};

describe("PlayerStatisticsGrid", () => {
	it("shows the favourite game and when the player last played instead of session and win counts", () => {
		renderWithProviders(
			<PlayerStatisticsGrid
				statistics={statistics}
				settings={settings}
				lastPlayed={{ start: "2026-10-11T19:30:00", gameTitle: "Azul" }}
			/>,
		);

		expect(screen.getByText("favorite-game")).toBeInTheDocument();
		expect(screen.getByText("Wingspan")).toBeInTheDocument();
		expect(screen.getByText("last-played")).toBeInTheDocument();
		expect(screen.getByText("11-10-2026")).toBeInTheDocument();
		expect(screen.getByText("Azul")).toBeInTheDocument();
		expect(screen.queryByText("play-count")).not.toBeInTheDocument();
		expect(screen.queryByText("win-count")).not.toBeInTheDocument();
	});

	it("shows the total play time in the short duration format", () => {
		renderWithProviders(<PlayerStatisticsGrid statistics={statistics} settings={settings} />);

		expect(screen.getByText("short-duration.hours-minutes")).toBeInTheDocument();
	});

	it("leaves out the favourite game and last played without data", () => {
		renderWithProviders(
			<PlayerStatisticsGrid
				statistics={{ ...statistics, mostPlayedGames: [] }}
				settings={settings}
				lastPlayed={null}
			/>,
		);

		expect(screen.queryByText("favorite-game")).not.toBeInTheDocument();
		expect(screen.queryByText("last-played")).not.toBeInTheDocument();
	});
});
