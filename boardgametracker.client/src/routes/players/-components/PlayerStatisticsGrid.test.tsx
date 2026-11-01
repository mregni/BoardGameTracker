import { describe, expect, it, vi } from "vitest";
import type { Settings } from "@/models";
import { renderWithProviders, screen } from "@/test/test-utils";
import { PlayerStatisticsGrid } from "./PlayerStatisticsGrid";

vi.mock("@tanstack/react-router", () => ({
	Link: ({
		children,
		to,
		params,
		search,
	}: {
		children: React.ReactNode;
		to: string;
		params?: { gameId: number };
		search?: { left: number; right: number };
	}) => (
		<a
			href={params ? to.replace("$gameId", String(params.gameId)) : `${to}?left=${search?.left}&right=${search?.right}`}
		>
			{children}
		</a>
	),
}));

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
	it("leads with the sessions, wins and win rate, followed by the favourite game and when the player last played", () => {
		renderWithProviders(
			<PlayerStatisticsGrid
				playerId={1}
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
		expect(screen.getByText("play-count")).toBeInTheDocument();
		expect(screen.getByText("25")).toBeInTheDocument();
		expect(screen.getByText("common:wins")).toBeInTheDocument();
		expect(screen.getByText("player:record.win-rate")).toBeInTheDocument();
		expect(screen.getByRole("link", { name: "Wingspan" })).toHaveAttribute("href", "/games/1");
	});

	it("shows the total play time in the short duration format", () => {
		renderWithProviders(<PlayerStatisticsGrid playerId={1} statistics={statistics} settings={settings} />);

		expect(screen.getByText("short-duration.hours-minutes")).toBeInTheDocument();
	});

	it("leaves out the favourite game and last played without data", () => {
		renderWithProviders(
			<PlayerStatisticsGrid
				playerId={1}
				statistics={{ ...statistics, mostPlayedGames: [] }}
				settings={settings}
				lastPlayed={null}
			/>,
		);

		expect(screen.queryByText("favorite-game")).not.toBeInTheDocument();
		expect(screen.queryByText("last-played")).not.toBeInTheDocument();
	});

	it("shows the recent form as letters with a spoken summary", () => {
		renderWithProviders(
			<PlayerStatisticsGrid
				playerId={1}
				statistics={statistics}
				settings={settings}
				form={[
					{ id: 1, won: true },
					{ id: 2, won: false },
					{ id: 3, won: true },
				]}
			/>,
		);

		expect(screen.getByText("player:record.form")).toBeInTheDocument();
		expect(screen.getByText("player:record.form-summary")).toHaveClass("sr-only");
		expect(screen.getAllByText("player:record.form-win")).toHaveLength(2);
		expect(screen.getAllByText("player:record.form-loss")).toHaveLength(1);
	});

	it("links the nemesis to the comparison", () => {
		renderWithProviders(
			<PlayerStatisticsGrid
				playerId={1}
				statistics={statistics}
				settings={settings}
				nemesis={{ playerId: 7, name: "Kathleen", wins: 1, losses: 5 }}
			/>,
		);

		expect(screen.getByText("player:record.nemesis")).toBeInTheDocument();
		expect(screen.getByRole("link", { name: "Kathleen" })).toHaveAttribute("href", "/compare?left=1&right=7");
		expect(screen.getByText("player:rivals.record")).toBeInTheDocument();
	});
});
