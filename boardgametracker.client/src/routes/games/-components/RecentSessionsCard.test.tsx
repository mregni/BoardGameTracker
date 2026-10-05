import { describe, expect, it, vi } from "vitest";
import type { Session } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
import { RecentSessionsCard } from "./RecentSessionsCard";

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, className }: { children: React.ReactNode; className?: string }) => (
		<a href="/games/3/sessions" className={className}>
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

const session = (id: number, playerSessions: { playerId: number; won: boolean; score: number | null }[]) =>
	({
		id,
		start: new Date(2026, 9, 5, 20, 0),
		minutes: 80,
		playerSessions,
	}) as unknown as Session;

describe("RecentSessionsCard", () => {
	it("shows the date, the winner and the winning score on one line", () => {
		renderWithTheme(
			<RecentSessionsCard
				gameId={3}
				dateFormat="yy-MM-dd"
				sessions={[
					session(1, [
						{ playerId: 1, won: true, score: 20 },
						{ playerId: 2, won: false, score: 12 },
					]),
				]}
			/>,
		);

		const row = screen.getByRole("listitem");
		expect(row).toHaveTextContent("26-10-05");
		expect(row).toHaveTextContent("Kathleen");
		expect(row).toHaveTextContent("20");
		expect(row).not.toHaveTextContent("Mikhaël");
		expect(row).toHaveAttribute("title", "common:player • 80common:minutes-abbreviation");
	});

	it("leaves out the score for a game without scoring", () => {
		renderWithTheme(
			<RecentSessionsCard
				gameId={3}
				dateFormat="yy-MM-dd"
				sessions={[session(1, [{ playerId: 2, won: true, score: null }])]}
			/>,
		);

		expect(screen.getByRole("listitem")).toHaveTextContent(/^26-10-05Mikhaël$/);
	});

	it("links to every session of the game", () => {
		renderWithTheme(<RecentSessionsCard gameId={3} dateFormat="yy-MM-dd" sessions={[]} />);

		expect(screen.getByRole("link", { name: "sessions" })).toHaveAttribute("href", "/games/3/sessions");
	});
});
