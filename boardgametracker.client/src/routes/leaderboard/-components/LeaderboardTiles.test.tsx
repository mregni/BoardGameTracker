import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { Leaderboard, LeaderboardEntry } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
import { LeaderboardTiles } from "./LeaderboardTiles";

vi.mock("@tanstack/react-router", () => ({
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
	it("should leave out the record that repeats the ranking and link the rest to their player", () => {
		renderWithTheme(<LeaderboardTiles leaderboard={leaderboard()} podium={[]} />);

		const links = screen.getAllByRole("link");
		expect(links.map((link) => link.getAttribute("href"))).toEqual(["/players/3", "/players/4", "/players/5"]);
		expect(screen.queryByText("highlights.most-wins")).not.toBeInTheDocument();
		expect(screen.getByText("142")).toBeInTheDocument();
		expect(screen.getByText("41%")).toBeInTheDocument();
		expect(screen.getByText("short-duration.hours")).toBeInTheDocument();
		expect(screen.getByRole("heading", { name: "highlights.title" })).toBeInTheDocument();
	});

	it.each([
		["winRate", "highlights.best-win-rate", "1,054"],
		["sessions", "highlights.most-sessions", "41%"],
	] as const)("should leave out the repeated record when ranking by %s", (rankBy, hidden, shown) => {
		renderWithTheme(<LeaderboardTiles leaderboard={leaderboard({ rankBy })} podium={[]} />);

		expect(screen.getAllByRole("listitem")).toHaveLength(3);
		expect(screen.queryByText(hidden, { exact: false })).not.toBeInTheDocument();
		expect(screen.getByText(shown)).toBeInTheDocument();
	});

	it("should name the holder and the minimum on the win rate record", () => {
		renderWithTheme(<LeaderboardTiles leaderboard={leaderboard()} podium={[]} />);

		expect(screen.getByText("reggi")).toBeInTheDocument();
		expect(screen.getByText("highlights.best-win-rate · highlights.min-sessions")).toBeInTheDocument();
	});

	it("should show unlinked records when nobody holds one yet", () => {
		renderWithTheme(
			<LeaderboardTiles
				leaderboard={leaderboard({ mostWins: null, mostPlays: null, bestWinRate: null, mostTimePlayed: null })}
				podium={[]}
			/>,
		);

		expect(screen.queryAllByRole("link")).toHaveLength(0);
		expect(screen.getAllByText("highlights.nobody-yet")).toHaveLength(3);
	});

	it("should leave out records the podium already shows but keep the time played", () => {
		const board = leaderboard();
		renderWithTheme(
			<LeaderboardTiles
				leaderboard={board}
				podium={[board.mostPlays, board.mostTimePlayed].filter((x) => x !== null)}
			/>,
		);

		expect(screen.getAllByRole("link").map((link) => link.getAttribute("href"))).toEqual(["/players/4", "/players/5"]);
		expect(screen.queryByText("highlights.most-sessions")).not.toBeInTheDocument();
	});

	it("should give each record link one clear name", () => {
		renderWithTheme(<LeaderboardTiles leaderboard={leaderboard()} podium={[]} />);

		expect(
			screen.getByRole("link", { name: "highlights.best-win-rate, highlights.min-sessions: reggi, 41%" }),
		).toBeInTheDocument();
	});
});
