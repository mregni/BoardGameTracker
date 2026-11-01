import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen } from "@/test/test-utils";
import { PlayerRow } from "./PlayerRow";

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, to, params }: { children: React.ReactNode; to: string; params?: { playerId: number } }) => (
		<a href={params ? to.replace("$playerId", String(params.playerId)) : to}>{children}</a>
	),
}));

const mikhael = { id: 4, name: "Mikhael", image: null, playCount: 8, winCount: 5, lastPlayed: "2026-10-30T18:00:00Z" };

const renderRow = (sort: "recent" | "name" | "sessions") =>
	renderWithTheme(
		<ul>
			<PlayerRow player={mikhael} sort={sort} />
		</ul>,
	);

describe("PlayerRow", () => {
	it("links to the player with the record and without announcing the avatar", () => {
		renderRow("recent");

		const link = screen.getByRole("link", { name: /^Mikhaellist\.record/ });
		expect(link).toHaveAttribute("href", "/players/4");
		expect(screen.getByText("list.last-played")).toBeInTheDocument();
	});

	it("shows the win rate when sorting by name", () => {
		renderRow("name");

		expect(screen.getByText("63%")).toBeInTheDocument();
		expect(screen.getByText("list.win-rate")).toBeInTheDocument();
	});

	it("shows the session count when sorting by sessions", () => {
		renderRow("sessions");

		expect(screen.getByText("8")).toBeInTheDocument();
		expect(screen.getByText("list.sessions")).toBeInTheDocument();
	});
});
