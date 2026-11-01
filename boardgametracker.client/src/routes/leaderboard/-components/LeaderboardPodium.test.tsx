import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { LeaderboardEntry } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
import { LeaderboardPodium } from "./LeaderboardPodium";

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

const entry = (playerId: number, name: string, rank: number | null, isRanked = true): LeaderboardEntry => ({
	rank,
	playerId,
	name,
	image: null,
	playCount: 20,
	winCount: 8,
	podiumCount: 1,
	winPercentage: 40,
	minutesPlayed: 600,
	isRanked,
	sessionsToRank: 0,
});

const players = [entry(7, "Mikhaël", 1), entry(3, "Kathleen", 2), entry(4, "reggi", 3), entry(5, "Anna", 4)];

describe("LeaderboardPodium", () => {
	it("should show the top three in rank order, each linking to the player", () => {
		renderWithTheme(<LeaderboardPodium players={players} rankBy="wins" />);

		const links = screen.getAllByRole("link");
		expect(links.map((link) => link.getAttribute("href"))).toEqual(["/players/7", "/players/3", "/players/4"]);
		expect(screen.queryByText("Anna")).not.toBeInTheDocument();
		expect(screen.getByRole("region", { name: "ranking.podium" })).toBeInTheDocument();
	});

	it("should put the winner in the middle", () => {
		renderWithTheme(<LeaderboardPodium players={players} rankBy="wins" />);

		const items = screen.getAllByRole("listitem");
		expect(items[0].className).toContain("order-2");
		expect(items[1].className).toContain("order-1");
		expect(items[2].className).toContain("order-3");
	});

	it("should show two places when only two players are ranked", () => {
		renderWithTheme(
			<LeaderboardPodium
				players={[entry(7, "Mikhaël", 1), entry(3, "Kathleen", 2), entry(4, "reggi", null, false)]}
				rankBy="wins"
			/>,
		);

		expect(screen.getAllByRole("listitem")).toHaveLength(2);
		expect(screen.getAllByRole("link")[0]).toHaveAccessibleName(
			"ranking.place 1, Mikhaël, 8 columns.wins, 40% · values.sessions",
		);
		expect(screen.getAllByRole("listitem")[0].className).not.toContain("order-");
		expect(screen.queryByText("reggi")).not.toBeInTheDocument();
	});

	it("should render nothing with fewer than two ranked players", () => {
		const { container } = renderWithTheme(
			<LeaderboardPodium players={[entry(7, "Mikhaël", 1), entry(3, "Kathleen", null, false)]} rankBy="wins" />,
		);

		expect(container).toBeEmptyDOMElement();
	});

	it.each([
		["wins", "40% · values.sessions"],
		["winRate", "values.wins · values.sessions"],
		["sessions", "values.wins · 40%"],
	] as const)("should show the other metrics for %s", (rankBy, details) => {
		renderWithTheme(<LeaderboardPodium players={players} rankBy={rankBy} />);

		expect(screen.getAllByText(details)).toHaveLength(3);
	});

	it.each([
		["wins", "8", "columns.wins"],
		["winRate", "40%", "columns.win-rate"],
		["sessions", "20", "columns.sessions"],
	] as const)("should label the score it ranks by %s", (rankBy, score, unit) => {
		renderWithTheme(<LeaderboardPodium players={players} rankBy={rankBy} />);

		expect(screen.getAllByText(score)[0]).toHaveClass("text-card-value");
		expect(screen.getAllByText(unit)).toHaveLength(3);
	});
});
