import { describe, expect, it, vi } from "vitest";
import { RouterLinkMock } from "@/test/routerLinkMock";
import { renderWithTheme, screen, within } from "@/test/test-utils";
import { TopPlayersCard } from "./TopPlayersCard";

vi.mock("@tanstack/react-router", () => ({ Link: RouterLinkMock }));

const players = [
	{ id: 1, rank: 1, name: "Mikhaël", image: null, playCount: 142, winCount: 54 },
	{ id: 2, rank: 2, name: "Kathleen", image: null, playCount: 118, winCount: 48 },
	{ id: 3, rank: 3, name: "reggi", image: null, playCount: 0, winCount: 0 },
];

describe("TopPlayersCard", () => {
	it("links to the leaderboard", () => {
		renderWithTheme(<TopPlayersCard players={players} includesCoopWins={false} />);

		expect(screen.getByRole("link", { name: "top-players.leaderboard" })).toHaveAttribute("href", "/leaderboard");
	});

	it("ranks each player with wins and a link to the player", () => {
		renderWithTheme(<TopPlayersCard players={players} includesCoopWins={false} />);

		const rows = screen.getAllByRole("listitem");
		expect(rows).toHaveLength(3);
		const first = within(rows[0]).getByRole("link");
		expect(first).toHaveAttribute("href", "/players/1");
		expect(first).toHaveAttribute("title", "top-players.row");
		expect(within(first).getByText("1")).toBeInTheDocument();
		expect(within(first).getByText("Mikhaël")).toBeInTheDocument();
		expect(within(first).getByText("54")).toBeInTheDocument();
		expect(within(rows[2]).getByRole("link")).toHaveAttribute("href", "/players/3");
		expect(screen.getByText("top-players.hint")).toBeInTheDocument();
	});

	it("says co-op wins are included when there are co-op wins", () => {
		renderWithTheme(<TopPlayersCard players={players} includesCoopWins />);

		expect(screen.getByText("top-players.hint-coop")).toBeInTheDocument();
		expect(screen.queryByText("top-players.hint")).not.toBeInTheDocument();
	});

	it("shows an empty state without players", () => {
		renderWithTheme(<TopPlayersCard players={[]} includesCoopWins={false} />);

		expect(screen.getByText("top-players.empty")).toBeInTheDocument();
		expect(screen.queryByRole("listitem")).not.toBeInTheDocument();
	});
});
