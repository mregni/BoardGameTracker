import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import TrophyIcon from "@/assets/icons/trophy.svg?react";
import type { LeaderboardEntry } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
import { LeaderboardHighlight } from "./LeaderboardHighlight";

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, to, params }: { children: ReactNode; to: string; params: { playerId: number } }) => (
		<a href={to.replace("$playerId", String(params.playerId))}>{children}</a>
	),
}));

const entry: LeaderboardEntry = {
	rank: 1,
	playerId: 7,
	name: "Alice",
	image: null,
	playCount: 12,
	winCount: 9,
	podiumCount: 11,
	winPercentage: 75,
	minutesPlayed: 600,
};

describe("LeaderboardHighlight", () => {
	it("links the leading player with the highlighted value", () => {
		renderWithTheme(
			<LeaderboardHighlight title="Most wins" value="9 wins" entry={entry} emptyText="Nobody yet" icon={TrophyIcon} />,
		);

		expect(screen.getByText("Most wins")).toBeInTheDocument();
		expect(screen.getByText("9 wins")).toBeInTheDocument();
		expect(screen.getByRole("link")).toHaveAttribute("href", "/players/7");
		expect(screen.getByText("A")).toBeInTheDocument();
	});

	it("shows the empty text when there is no leader", () => {
		renderWithTheme(
			<LeaderboardHighlight
				title="Best win rate"
				value="0%"
				entry={null}
				emptyText="Needs at least 5 plays"
				icon={TrophyIcon}
			/>,
		);

		expect(screen.getByText("Needs at least 5 plays")).toBeInTheDocument();
		expect(screen.queryByRole("link")).not.toBeInTheDocument();
	});
});
