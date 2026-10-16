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

const entry = (playerId: number, name: string, rank: number): LeaderboardEntry => ({
	rank,
	playerId,
	name,
	image: null,
	playCount: 20,
	winCount: 8,
	podiumCount: 1,
	winPercentage: 40,
	minutesPlayed: 600,
	isRanked: true,
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

	it.each([
		["wins", "values.wins · 40%"],
		["winRate", "40% · values.wins"],
		["sessions", "values.sessions · 40%"],
	] as const)("should lead the summary with the %s metric", (rankBy, summary) => {
		renderWithTheme(<LeaderboardPodium players={players} rankBy={rankBy} />);

		expect(screen.getAllByText(summary)).toHaveLength(3);
	});
});
