import { describe, expect, it, vi } from "vitest";
import type { PlayerRival } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
import { PlayerRivalsCard } from "./PlayerRivalsCard";

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, search }: { children: React.ReactNode; search: { left: number; right: number } }) => (
		<a href={`/compare?left=${search.left}&right=${search.right}`}>{children}</a>
	),
}));

vi.mock("@/routes/-hooks/usePlayerById", () => ({
	usePlayerById: () => ({ playerById: (id: number) => ({ id, name: `Player ${id}`, image: null }) }),
}));

const rival = (playerId: number, wins: number, losses: number): PlayerRival => ({
	playerId,
	sessions: wins + losses,
	wins,
	losses,
});

describe("PlayerRivalsCard", () => {
	it("renders nothing without rivals", () => {
		const { container } = renderWithTheme(<PlayerRivalsCard playerId={1} rivals={[]} />);

		expect(container).toBeEmptyDOMElement();
	});

	it("links each rival to the comparison and colours the record", () => {
		renderWithTheme(<PlayerRivalsCard playerId={1} rivals={[rival(2, 5, 1), rival(3, 1, 4), rival(4, 2, 2)]} />);

		expect(screen.getByText("rivals.title")).toBeInTheDocument();
		expect(screen.getByRole("link", { name: /Player 2/ })).toHaveAttribute("href", "/compare?left=1&right=2");
		expect(screen.getByText("5–1").parentElement).toHaveClass("text-ok");
		expect(screen.getByText("1–4").parentElement).toHaveClass("text-error");
		expect(screen.getByText("2–2").parentElement).toHaveClass("text-white/70");
	});

	it("shows at most five rivals", () => {
		const rivals = [2, 3, 4, 5, 6, 7].map((id) => rival(id, 1, 1));
		renderWithTheme(<PlayerRivalsCard playerId={1} rivals={rivals} />);

		expect(screen.getAllByRole("link")).toHaveLength(5);
		expect(screen.queryByText("Player 7")).not.toBeInTheDocument();
	});
});
