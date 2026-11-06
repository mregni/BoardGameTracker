import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen } from "@/test/test-utils";
import { PlayerLoansCard } from "./PlayerLoansCard";

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, params }: { children: React.ReactNode; params: { gameId: number } }) => (
		<a href={`/games/${params.gameId}`}>{children}</a>
	),
}));

describe("PlayerLoansCard", () => {
	it("renders nothing without loans", () => {
		const { container } = renderWithTheme(<PlayerLoansCard games={[]} />);

		expect(container).toBeEmptyDOMElement();
	});

	it("links each game on loan to its game page", () => {
		renderWithTheme(
			<PlayerLoansCard
				games={[
					{ id: 7, title: "Azul", image: "" },
					{ id: 8, title: "Brass", image: "" },
				]}
			/>,
		);

		expect(screen.getByRole("heading", { name: "cards.games-on-loan" })).toBeInTheDocument();
		expect(screen.getByRole("link", { name: "Azul" })).toHaveAttribute("href", "/games/7");
		expect(screen.getByRole("link", { name: "Brass" })).toHaveAttribute("href", "/games/8");
	});
});
