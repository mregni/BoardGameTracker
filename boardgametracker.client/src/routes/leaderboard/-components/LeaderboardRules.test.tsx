import { describe, expect, it } from "vitest";
import type { Leaderboard, LeaderboardEntry } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
import { LeaderboardRules } from "./LeaderboardRules";

const entry = (podiumCount: number): LeaderboardEntry => ({
	rank: 1,
	playerId: 1,
	name: "Player",
	image: null,
	playCount: 10,
	winCount: 4,
	podiumCount,
	winPercentage: 40,
	minutesPlayed: 600,
	isRanked: true,
	sessionsToRank: 0,
});

const leaderboard = (overrides: Partial<Leaderboard> = {}): Leaderboard => ({
	mostWins: null,
	mostPlays: null,
	bestWinRate: null,
	mostTimePlayed: null,
	minimumSessions: 5,
	minimumPlayersForPodium: 4,
	rankBy: "wins",
	showPodium: true,
	rankedCount: 3,
	winCount: 149,
	coopWinCount: 0,
	players: [entry(2), entry(0)],
	...overrides,
});

describe("LeaderboardRules", () => {
	it("should explain the podium count and that ties share the rank", () => {
		renderWithTheme(<LeaderboardRules leaderboard={leaderboard()} />);

		expect(screen.getByText("ranking.how")).toBeInTheDocument();
		expect(screen.getByText("columns.podiums-hint")).toBeInTheDocument();
		expect(screen.getByText("ranking.ties-wins")).toBeInTheDocument();
	});

	it.each([
		["winRate", "ranking.ties-win-rate"],
		["sessions", "ranking.ties-sessions"],
	] as const)("should name the tiebreak of ranking by %s", (rankBy, key) => {
		renderWithTheme(<LeaderboardRules leaderboard={leaderboard({ rankBy })} />);

		expect(screen.getByText(key)).toBeInTheDocument();
	});

	it("should explain co-op wins next to the tie rule", () => {
		renderWithTheme(<LeaderboardRules leaderboard={leaderboard({ coopWinCount: 24 })} />);

		expect(screen.getByText("ranking.coop ranking.ties-wins")).toBeInTheDocument();
	});

	it("should show the co-op note alone when there are no ties to explain", () => {
		renderWithTheme(<LeaderboardRules leaderboard={leaderboard({ coopWinCount: 3, rankedCount: 1 })} />);

		expect(screen.getByText("ranking.coop")).toBeInTheDocument();
	});

	it("should render nothing when there is nothing to explain", () => {
		const { container } = renderWithTheme(
			<LeaderboardRules leaderboard={leaderboard({ rankedCount: 1, players: [entry(0)] })} />,
		);

		expect(container).toBeEmptyDOMElement();
	});
});
