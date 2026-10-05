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
	it("shows one line per player with the win rate", () => {
		renderWithTheme(
			<TopPlayersCard
				topPlayers={[
					topPlayer(),
					topPlayer({ playerId: 2, wins: 0, playCount: 1, winPercentage: 0, trend: Trend.Equal }),
				]}
			/>,
		);

		expect(screen.getByText("Kathleen")).toBeInTheDocument();
		expect(screen.getByText("Mikhaël")).toBeInTheDocument();
		expect(screen.getByText("100%")).toBeInTheDocument();
		expect(screen.getByText("0%")).toBeInTheDocument();
	});

	it("keeps wins, games and the average score out of the row and in its tooltip", () => {
		renderWithTheme(<TopPlayersCard topPlayers={[topPlayer()]} />);

		expect(screen.queryByText(/common:win/)).not.toBeInTheDocument();
		expect(screen.getByText("Kathleen").closest("li")).toHaveAttribute(
			"title",
			"common:win • common:game • 20 statistics:average-abreviation",
		);
	});

	it("shows the empty state without players", () => {
		renderWithTheme(<TopPlayersCard topPlayers={[]} />);

		expect(screen.queryByRole("listitem")).not.toBeInTheDocument();
	});
});
