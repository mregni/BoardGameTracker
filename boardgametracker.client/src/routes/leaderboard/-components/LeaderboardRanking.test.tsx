import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { LeaderboardEntry, LeaderboardRankBy } from "@/models";
import { renderWithTheme, screen, within } from "@/test/test-utils";
import { LeaderboardRanking } from "./LeaderboardRanking";

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

const players = [
	entry({
		rank: 1,
		playerId: 7,
		name: "Mikhaël",
		winCount: 54,
		playCount: 142,
		winPercentage: 38,
		minutesPlayed: 7860,
	}),
	entry({ rank: 2, playerId: 3, name: "Kathleen", winCount: 48, playCount: 118, winPercentage: 40.7 }),
	entry({ rank: 2, playerId: 4, name: "reggi", winCount: 48, playCount: 118, winPercentage: 40.7 }),
	entry({ rank: 4, playerId: 5, name: "Anna", winCount: 15, playCount: 41 }),
	entry({
		rank: null,
		playerId: 9,
		name: "Bram",
		winCount: 1,
		playCount: 4,
		winPercentage: 25,
		minutesPlayed: 220,
		isRanked: false,
		sessionsToRank: 1,
	}),
];

const renderRanking = (list: LeaderboardEntry[] = players, coopWinCount = 0, rankBy: LeaderboardRankBy = "wins") =>
	renderWithTheme(
		<LeaderboardRanking
			players={list}
			rankBy={rankBy}
			minimumSessions={5}
			minimumPlayersForPodium={4}
			label="Ranking"
			winCount={149}
			coopWinCount={coopWinCount}
		/>,
	);

describe("LeaderboardRanking", () => {
	it("should keep the order and shared ranks of the ranked players", () => {
		renderRanking();

		const rows = within(screen.getAllByRole("list")[0]).getAllByRole("link");
		expect(rows.map((row) => row.querySelector("strong")?.textContent)).toEqual([
			"Mikhaël",
			"Kathleen",
			"reggi",
			"Anna",
		]);
		expect(rows.map((row) => row.firstElementChild?.textContent)).toEqual(["1", "2", "2", "4"]);
	});

	it("should link every row to the player", () => {
		renderRanking();

		const links = screen.getAllByTestId("leaderboard-row");
		expect(links).toHaveLength(5);
		expect(links.map((link) => link.getAttribute("href"))).toEqual([
			"/players/7",
			"/players/3",
			"/players/4",
			"/players/5",
			"/players/9",
		]);
	});

	it("should put players below the minimum under a not ranked divider", () => {
		renderRanking();

		const divider = screen.getByRole("heading", { name: "ranking.not-ranked" });
		const lists = screen.getAllByRole("list");
		expect(lists).toHaveLength(2);
		expect(divider.nextElementSibling).toBe(lists[1]);
		const below = within(lists[1]).getByRole("link");
		expect(below).toHaveTextContent("Bram");
		expect(below).toHaveTextContent("ranking.not-ranked-rank");
		expect(below).toHaveTextContent("values.to-rank");
		expect(below.className).toContain("border-dashed");
	});

	it("should show the details line with podiums for ranked players", () => {
		renderRanking([players[0]]);

		expect(screen.getByRole("link")).toHaveTextContent("38% · values.sessions · values.podiums");
	});

	it("should show culture-aware numbers, the win rate and one duration format", () => {
		renderRanking([players[0], players[4]]);

		const [first, second] = screen.getAllByTestId("leaderboard-row");
		expect(first).toHaveTextContent("142");
		expect(first).toHaveTextContent("38%");
		expect(first).toHaveTextContent("short-duration.hours");
		expect(second).toHaveTextContent("short-duration.hours-minutes");
	});

	it("should hide the podiums column and podium counts when nobody has a podium", () => {
		renderRanking(players.map((player) => ({ ...player, podiumCount: 0 })));

		expect(screen.queryByText("columns.podiums")).not.toBeInTheDocument();
		expect(screen.queryByText("columns.podiums-hint")).not.toBeInTheDocument();
		const [first] = screen.getAllByTestId("leaderboard-row");
		expect(first).not.toHaveTextContent("values.podiums");
		expect(first.className).toContain(
			"md:grid-cols-[1.625rem_1.875rem_minmax(0,1.3fr)_3.5rem_4.25rem_minmax(8rem,1.3fr)_4.25rem_1rem]",
		);
	});

	it("should explain the podium column and that ties share the rank", () => {
		renderRanking();

		expect(screen.getAllByText("columns.podiums-hint").length).toBeGreaterThan(0);
		expect(screen.getByText("ranking.ties-wins")).toBeInTheDocument();
	});

	it.each([
		["winRate", "ranking.ties-win-rate"],
		["sessions", "ranking.ties-sessions"],
	] as const)("should name the tiebreak of ranking by %s", (rankBy, key) => {
		renderRanking(players, 0, rankBy);

		expect(screen.getByText(key)).toBeInTheDocument();
	});

	it("should leave out the divider when everybody is ranked", () => {
		renderRanking(players.slice(0, 1));

		expect(screen.queryByRole("heading", { name: "ranking.not-ranked" })).not.toBeInTheDocument();
		expect(screen.queryByText("ranking.ties-wins")).not.toBeInTheDocument();
	});

	it("should only show the divider when nobody is ranked yet", () => {
		renderRanking([players[4]]);

		expect(screen.getAllByRole("list")).toHaveLength(1);
		expect(screen.getByRole("heading", { name: "ranking.not-ranked" })).toBeInTheDocument();
	});

	it("should explain co-op wins when some wins come from co-op sessions", () => {
		renderRanking(players, 24);

		expect(screen.getByText("ranking.coop ranking.ties-wins")).toBeInTheDocument();
	});

	it("should leave out the co-op note without co-op wins", () => {
		renderRanking(players, 0);

		expect(screen.queryByText(/ranking\.coop/)).not.toBeInTheDocument();
	});

	it("should show the co-op note alone when there are no ties to explain", () => {
		renderRanking(players.slice(0, 1), 3);

		expect(screen.getByText("ranking.coop")).toBeInTheDocument();
	});

	it.each([
		["wins", "values.wins", "38% · values.sessions · values.podiums"],
		["winRate", "38%", "values.wins · values.sessions · values.podiums"],
		["sessions", "values.sessions", "values.wins · 38% · values.podiums"],
	] as const)(
		"should show the rank-by metric as the prominent mobile value when ranking by %s",
		(rankBy, value, details) => {
			renderRanking([players[0]], 0, rankBy);

			expect(screen.getByTestId("leaderboard-mobile-value")).toHaveTextContent(value);
			expect(screen.getByTestId("leaderboard-mobile-value")).toHaveClass("md:hidden");
			expect(screen.getByRole("link")).toHaveTextContent(details);
		},
	);
});
