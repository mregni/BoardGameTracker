import { describe, expect, it, vi } from "vitest";
import { GameState } from "@/models";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { GameDetailEmptyState } from "./GameDetailEmptyState";
import { GameHeader } from "./GameHeader";

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
	it("offers to mark a wanted game as owned instead of logging a session", async () => {
		const onMarkOwned = vi.fn();
		const { onAddSession } = renderHeader(GameState.Wanted, onMarkOwned);

		expect(screen.queryByRole("button", { name: "add" })).not.toBeInTheDocument();
		await userEvent.click(screen.getByRole("button", { name: "mark-owned" }));

		expect(onMarkOwned).toHaveBeenCalledTimes(1);
		expect(onAddSession).not.toHaveBeenCalled();
	});

	it("logs a session for an owned game", async () => {
		const { onAddSession } = renderHeader(GameState.Owned, vi.fn());

		expect(screen.queryByRole("button", { name: "mark-owned" })).not.toBeInTheDocument();
		await userEvent.click(screen.getByRole("button", { name: "add" }));

		expect(onAddSession).toHaveBeenCalledTimes(1);
	});
});

describe("GameDetailEmptyState", () => {
	it("invites a session for a game without plays", () => {
		renderWithTheme(<GameDetailEmptyState />);

		expect(screen.getByText("no-sessions.title")).toBeInTheDocument();
		expect(screen.getByText("no-sessions.description")).toBeInTheDocument();
	});

	it("only says not played yet for a wanted game", () => {
		renderWithTheme(<GameDetailEmptyState wanted />);

		expect(screen.getByText("no-sessions.not-played")).toBeInTheDocument();
		expect(screen.queryByText("no-sessions.description")).not.toBeInTheDocument();
	});
});
