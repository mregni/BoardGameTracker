import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { Leaderboard, LeaderboardEntry } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
import { LeaderboardTiles } from "./LeaderboardTiles";

vi.mock("@tanstack/react-router", () => ({
	Link: ({ to, children, ...rest }: { to: string; children: ReactNode; className?: string }) => (
		<a href={to} {...rest}>
			{children}
		</a>
	),
}));

const entry = (overrides: Partial<LeaderboardEntry>): LeaderboardEntry => ({
	rank: 1,
	playerId: 1,
	name: "Player",
	image: null,
	playCount: 10,
	winCount: 4,
	podiumCount: 2,
	winPercentage: 40,
	minutesPlayed: 600,
	isRanked: true,
	sessionsToRank: 0,
	...overrides,
});

const leaderboard = (overrides: Partial<Leaderboard> = {}): Leaderboard => ({
	mostWins: entry({ playerId: 7, name: "Mikhaël", winCount: 1054 }),
	mostPlays: entry({ playerId: 3, name: "Kathleen", playCount: 142 }),
	bestWinRate: entry({ playerId: 4, name: "reggi", winPercentage: 41.2 }),
	mostTimePlayed: entry({ playerId: 5, name: "Anna", minutesPlayed: 7860 }),
	minimumSessions: 5,
	minimumPlayersForPodium: 4,
	rankBy: "wins",
	showPodium: true,
	rankedCount: 4,
	winCount: 20,
	coopWinCount: 0,
	players: [],
	...overrides,
});

describe("LeaderboardTiles", () => {
	it("should show four tiles that link to their player", () => {
		renderWithTheme(<LeaderboardTiles leaderboard={leaderboard()} />);

		const links = screen.getAllByRole("link");
		expect(links.map((link) => link.getAttribute("href"))).toEqual([
			"/players/7",
			"/players/3",
			"/players/4",
			"/players/5",
		]);
		expect(screen.getByText("1,054")).toBeInTheDocument();
		expect(screen.getByText("142")).toBeInTheDocument();
		expect(screen.getByText("41%")).toBeInTheDocument();
		expect(screen.getByText("short-duration.hours")).toBeInTheDocument();
	});

	it("should name the holder and the minimum on the win rate tile", () => {
		renderWithTheme(<LeaderboardTiles leaderboard={leaderboard()} />);

		expect(screen.getByText("Mikhaël")).toBeInTheDocument();
		expect(screen.getByText("reggi · highlights.min-sessions")).toBeInTheDocument();
	});

	it("should show unlinked tiles when nobody holds a record yet", () => {
		renderWithTheme(
			<LeaderboardTiles
				leaderboard={leaderboard({ mostWins: null, mostPlays: null, bestWinRate: null, mostTimePlayed: null })}
			/>,
		);

		expect(screen.queryAllByRole("link")).toHaveLength(0);
		expect(screen.getAllByText("highlights.nobody-yet")).toHaveLength(3);
		expect(screen.getByText("highlights.nobody-yet · highlights.min-sessions")).toBeInTheDocument();
	});
});
