import { describe, expect, it, vi } from "vitest";
import type { Session } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
import { PlayerSessionCardItem } from "./PlayerSessionCardItem";

vi.mock("@tanstack/react-router", () => ({
	useNavigate: () => vi.fn(),
}));

const session = (overrides: Partial<Session>) =>
	({
		id: 1,
		start: new Date("2026-10-16T19:00:00Z"),
		isCoop: false,
		teamWon: null,
		playerSessions: [{ playerId: 7, won: false, score: null }],
		...overrides,
	}) as unknown as Session;

const renderItem = (value: Session) =>
	renderWithTheme(<PlayerSessionCardItem session={value} playerId={7} dateFormat="dd-MM-yy" uiLanguage="en-us" />);

describe("PlayerSessionCardItem", () => {
	it("shows a co-op result as won or lost followed by co-op, without a separate badge", () => {
		renderItem(session({ isCoop: true, teamWon: true }));

		expect(screen.getByText("won")).toBeInTheDocument();
		expect(screen.getByText(/coop\.label/)).toBeInTheDocument();
		expect(screen.queryByText("coop.won")).not.toBeInTheDocument();
	});

	it("shows a lost co-op game as lost", () => {
		renderItem(session({ isCoop: true, teamWon: false }));

		expect(screen.getByText("lost")).toBeInTheDocument();
		expect(screen.getByText(/coop\.label/)).toBeInTheDocument();
	});

	it("shows the player's own result and score for a competitive game", () => {
		renderItem(session({ playerSessions: [{ playerId: 7, won: true, score: 100 }] } as Partial<Session>));

		expect(screen.getByText("won")).toBeInTheDocument();
		expect(screen.queryByText(/coop\.label/)).not.toBeInTheDocument();
		expect(screen.getByText("points")).toBeInTheDocument();
	});
});
