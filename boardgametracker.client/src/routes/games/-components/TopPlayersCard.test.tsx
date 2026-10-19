import { describe, expect, it, vi } from "vitest";
import { type TopPlayer, Trend } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
import { TopPlayersCard } from "./TopPlayersCard";

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, className }: { children: React.ReactNode; className?: string }) => (
		<a href="/players" className={className}>
			{children}
		</a>
	),
}));

vi.mock("@/routes/-hooks/usePlayerById", () => ({
	usePlayerById: () => ({
		playerById: (id: number) =>
			({ 1: { id: 1, name: "Kathleen", image: null }, 2: { id: 2, name: "Mikhaël", image: null } })[id],
	}),
}));

const topPlayer = (overrides: Partial<TopPlayer> = {}): TopPlayer => ({
	playerId: 1,
	playCount: 2,
	wins: 2,
	winPercentage: 1,
	trend: Trend.Up,
	averageScore: 20,
	...overrides,
});

describe("TopPlayersCard", () => {
	it("shows one line per player who won, with the win rate", () => {
		renderWithTheme(
			<TopPlayersCard
				topPlayers={[
					topPlayer(),
					topPlayer({ playerId: 2, wins: 0, playCount: 1, winPercentage: 0, trend: Trend.Equal }),
				]}
			/>,
		);

		expect(screen.getByText("Kathleen")).toBeInTheDocument();
		expect(screen.getByText("100%")).toBeInTheDocument();
		expect(screen.queryByText("Mikhaël")).not.toBeInTheDocument();
		expect(screen.queryByText("0%")).not.toBeInTheDocument();
	});

	it("colours a downward trend with the bad token", () => {
		renderWithTheme(<TopPlayersCard topPlayers={[topPlayer({ trend: Trend.Down })]} />);

		const rate = screen.getByText(/100%/);
		expect(rate).toHaveClass("text-bad");
		expect(rate).not.toHaveClass("text-red-500");
	});

	it("shows the empty state when nobody has won yet", () => {
		renderWithTheme(<TopPlayersCard topPlayers={[topPlayer({ wins: 0, winPercentage: 0 })]} />);

		expect(screen.queryByText("Kathleen")).not.toBeInTheDocument();
	});

	it("shows the wins next to the win rate so the ranking by wins reads in order", () => {
		renderWithTheme(<TopPlayersCard topPlayers={[topPlayer({ wins: 7, winPercentage: 0.5 })]} />);

		const row = screen.getByText("Kathleen").closest("a");
		expect(row).toHaveTextContent("common:win");
		expect(row).toHaveTextContent("50%");
	});

	it("keeps the sessions and the average score in the tooltip", () => {
		renderWithTheme(<TopPlayersCard topPlayers={[topPlayer()]} />);

		expect(screen.getByText("Kathleen").closest("li")).toHaveAttribute(
			"title",
			"common:sessions · 20 statistics:average-abreviation",
		);
	});

	it("explains the trend arrow", () => {
		renderWithTheme(
			<TopPlayersCard
				topPlayers={[
					topPlayer(),
					topPlayer({ playerId: 2, trend: Trend.Down }),
					topPlayer({ playerId: 3, trend: Trend.Equal }),
				]}
			/>,
		);

		expect(screen.getByRole("img", { name: "top-players.trend-up" })).toBeInTheDocument();
		expect(screen.getByRole("img", { name: "top-players.trend-down" })).toBeInTheDocument();
		expect(screen.getAllByRole("img")).toHaveLength(2);
	});

	it("shows the empty state without players", () => {
		renderWithTheme(<TopPlayersCard topPlayers={[]} />);

		expect(screen.queryByRole("listitem")).not.toBeInTheDocument();
	});
});
