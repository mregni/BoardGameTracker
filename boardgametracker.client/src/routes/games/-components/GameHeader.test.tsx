import { describe, expect, it, vi } from "vitest";
import { GameState } from "@/models";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { GameDetailEmptyState } from "./GameDetailEmptyState";
import { GameHeader } from "./GameHeader";

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, className }: { children: React.ReactNode; className?: string }) => (
		<a href="/loans" className={className}>
			{children}
		</a>
	),
}));

const renderHeader = (state: GameState, onMarkOwned?: () => void) => {
	const onAddSession = vi.fn();
	renderWithTheme(
		<GameHeader
			gameTitle="Ark Nova"
			gameState={state}
			isLoaned={false}
			canWrite
			onAddSession={onAddSession}
			onMarkOwned={onMarkOwned}
			onEdit={vi.fn()}
			onDelete={vi.fn()}
		/>,
	);
	return { onAddSession };
};

describe("GameHeader", () => {
	it("names the borrower and links to the loans of a loaned game", () => {
		renderWithTheme(
			<GameHeader
				gameTitle="Ark Nova"
				gameState={GameState.Owned}
				isLoaned
				loan={{ playerName: "Ed", since: "12-10-26" }}
				canWrite={false}
				onAddSession={vi.fn()}
				onEdit={vi.fn()}
				onDelete={vi.fn()}
			/>,
		);

		expect(screen.getByRole("link", { name: "state.on-loan-to" })).toHaveAttribute("href", "/loans");
	});

	it("offers to mark a wanted game as owned next to logging a session", async () => {
		const onMarkOwned = vi.fn();
		const { onAddSession } = renderHeader(GameState.Wanted, onMarkOwned);

		await userEvent.click(screen.getByRole("button", { name: "mark-owned" }));
		expect(onMarkOwned).toHaveBeenCalledTimes(1);
		expect(onAddSession).not.toHaveBeenCalled();

		await userEvent.click(screen.getByRole("button", { name: "add" }));
		expect(onAddSession).toHaveBeenCalledTimes(1);
	});

	it("leaves logging to the empty state when asked", () => {
		renderWithTheme(
			<GameHeader
				gameTitle="Ark Nova"
				gameState={GameState.Owned}
				isLoaned={false}
				canWrite
				showAddSession={false}
				onAddSession={vi.fn()}
				onEdit={vi.fn()}
				onDelete={vi.fn()}
			/>,
		);

		expect(screen.queryByRole("button", { name: "add" })).not.toBeInTheDocument();
	});

	it("renders the game title as the page heading", () => {
		renderHeader(GameState.Owned);

		expect(screen.getByRole("heading", { level: 1, name: "Ark Nova" })).toBeInTheDocument();
	});

	it("logs a session for an owned game", async () => {
		const { onAddSession } = renderHeader(GameState.Owned, vi.fn());

		expect(screen.queryByRole("button", { name: "mark-owned" })).not.toBeInTheDocument();
		await userEvent.click(screen.getByRole("button", { name: "add" }));

		expect(onAddSession).toHaveBeenCalledTimes(1);
	});
});

describe("GameDetailEmptyState", () => {
	it("invites a session for a game without plays", async () => {
		const onAddSession = vi.fn();
		renderWithTheme(<GameDetailEmptyState onAddSession={onAddSession} />);

		expect(screen.getByText("no-sessions.title")).toBeInTheDocument();
		expect(screen.getByText("no-sessions.description")).toBeInTheDocument();
		await userEvent.click(screen.getByRole("button", { name: "add" }));
		expect(onAddSession).toHaveBeenCalledTimes(1);
	});

	it("says not played yet for a wanted game", () => {
		renderWithTheme(<GameDetailEmptyState wanted />);

		expect(screen.getByText("no-sessions.not-played")).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "add" })).not.toBeInTheDocument();
	});
});
