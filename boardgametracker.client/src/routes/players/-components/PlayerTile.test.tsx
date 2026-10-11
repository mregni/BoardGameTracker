import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen } from "@/test/test-utils";
import { PlayerTile } from "./PlayerTile";

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, to }: { children: React.ReactNode; to: string }) => <a href={to}>{children}</a>,
}));

describe("PlayerTile", () => {
	it("links to the player page and shows the win rate with the session count", () => {
		renderWithTheme(<PlayerTile player={{ id: 4, name: "Mikhael", image: null, playCount: 8, winCount: 5 }} />);

		expect(screen.getByRole("link")).toHaveAttribute("href", "/players/4");
		expect(screen.getByText("62.5%")).toBeInTheDocument();
		expect(screen.getByText("8")).toBeInTheDocument();
	});

	it("says there are no sessions yet for a player without sessions", () => {
		renderWithTheme(<PlayerTile player={{ id: 5, name: "dfgh", image: null, playCount: 0, winCount: 0 }} />);

		expect(screen.getByText("tile.no-sessions")).toBeInTheDocument();
		expect(screen.queryByText("0%")).not.toBeInTheDocument();
	});
});
