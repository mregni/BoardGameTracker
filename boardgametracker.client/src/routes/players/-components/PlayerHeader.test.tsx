import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen } from "@/test/test-utils";
import { PlayerHeader } from "./PlayerHeader";

const mocks = vi.hoisted(() => ({ standing: null as unknown }));

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, to }: { children: React.ReactNode; to: string }) => <a href={to}>{children}</a>,
}));

vi.mock("../-hooks/usePlayerStanding", () => ({
	usePlayerStanding: () => mocks.standing,
}));

const renderHeader = (playCount = 3, canWrite = true) =>
	renderWithTheme(
		<PlayerHeader
			playerId={1}
			playerName="Kathleen"
			image={null}
			playCount={playCount}
			canWrite={canWrite}
			onEdit={vi.fn()}
			onDelete={vi.fn()}
		/>,
	);

describe("PlayerHeader", () => {
	beforeEach(() => {
		mocks.standing = null;
	});

	it("shows the name as the page heading with the session actions", () => {
		renderHeader();

		expect(screen.getByRole("heading", { level: 1, name: "Kathleen" })).toBeInTheDocument();
		expect(screen.getByRole("link", { name: /actions.log-session/ })).toHaveAttribute("href", "/sessions/new");
		expect(screen.getByRole("link", { name: /actions.compare/ })).toHaveAttribute("href", "/compare");
	});

	it("hides the actions for a player without sessions", () => {
		renderHeader(0);

		expect(screen.queryByRole("link", { name: /actions.compare/ })).not.toBeInTheDocument();
	});

	it("hides logging a session for read-only users", () => {
		renderHeader(3, false);

		expect(screen.queryByRole("link", { name: /actions.log-session/ })).not.toBeInTheDocument();
	});

	it("links the leaderboard rank", () => {
		mocks.standing = { rank: 1, isRanked: true, rankedCount: 4, sessionsToRank: 0 };
		renderHeader();

		expect(screen.getByRole("link", { name: "standing.ranked" })).toHaveAttribute("href", "/leaderboard");
	});

	it("says how many sessions are left to rank", () => {
		mocks.standing = { rank: 3, isRanked: false, rankedCount: 4, sessionsToRank: 2 };
		renderHeader();

		expect(screen.getByRole("link", { name: "standing.to-rank" })).toBeInTheDocument();
	});
});
