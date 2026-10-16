import { describe, expect, it, vi } from "vitest";
import type { Game, Player, Session } from "@/models";
import { renderWithTheme, screen, userEvent, within } from "@/test/test-utils";
import { SessionCardList } from "./SessionCardList";

vi.mock("@tanstack/react-router", () => ({
	Link: ({ children, to, params }: { children: React.ReactNode; to: string; params: { gameId: number } }) => (
		<a href={to.replace("$gameId", String(params.gameId))}>{children}</a>
	),
}));

const players = [
	{ id: 1, name: "Kathleen", image: null },
	{ id: 2, name: "Mikhaël", image: null },
	{ id: 3, name: "Jules", image: null },
] as unknown as Player[];

const games = [
	{ id: 9, title: "Twilight Imperium: Fourth Edition – Prophecy of Kings", image: null },
] as unknown as Game[];

const session = {
	id: 42,
	gameId: 9,
	start: new Date(2026, 9, 5, 20, 15),
	minutes: 95,
	comment: "  Close finish on the last round  ",
	playerSessions: [
		{ playerId: 2, sessionId: 42, won: false, score: 12, firstPlay: false },
		{ playerId: 1, sessionId: 42, won: true, score: 20, firstPlay: false },
		{ playerId: 3, sessionId: 42, won: false, score: null, firstPlay: false },
	],
} as unknown as Session;

const renderList = (overrides: Partial<React.ComponentProps<typeof SessionCardList>> = {}) => {
	const onEdit = vi.fn();
	const onDelete = vi.fn();
	renderWithTheme(
		<SessionCardList
			sessions={[session]}
			players={players}
			games={games}
			dateFormat="dd-MM-yyyy"
			timeFormat="HH:mm"
			uiLanguage="en-US"
			canWrite
			onEdit={onEdit}
			onDelete={onDelete}
			{...overrides}
		/>,
	);
	return { onEdit, onDelete };
};

describe("SessionCardList", () => {
	it("shows the date, time and duration of every session", () => {
		renderList();

		expect(screen.getByText("05-10-2026 · 20:15 · 95 minutes")).toBeInTheDocument();
	});

	it("lists every player with their score, winners first with a crown", () => {
		renderList();

		const pills = within(screen.getByRole("list", { name: "players" })).getAllByRole("listitem");
		expect(pills.map((pill) => pill.textContent)).toEqual(["KKathleen20", "MMikhaël12", "JJules"]);
		expect(within(pills[0]).getByRole("img", { name: "won" })).toBeInTheDocument();
		expect(within(pills[1]).queryByRole("img", { name: "won" })).not.toBeInTheDocument();
	});

	it("uses round avatars inside the player pills", () => {
		renderList();

		const pills = within(screen.getByRole("list", { name: "players" })).getAllByRole("listitem");
		for (const pill of pills) {
			const initial = within(pill).getByText(pill.textContent?.charAt(0) ?? "", { selector: "span" });
			expect(initial.parentElement).toHaveClass("rounded-full");
		}
	});

	it("shows the full trimmed comment", () => {
		renderList();

		expect(screen.getByText("Close finish on the last round")).toBeInTheDocument();
	});

	it("shows the full game title as a link when asked to", () => {
		renderList({ showGame: true });

		const link = screen.getByRole("link", { name: /Prophecy of Kings/ });
		expect(link).toHaveAttribute("href", "/games/9");
		expect(link).toHaveTextContent("Twilight Imperium: Fourth Edition – Prophecy of Kings");
	});

	it("leaves out the game when not asked to", () => {
		renderList();

		expect(screen.queryByRole("link")).not.toBeInTheDocument();
	});

	it("edits and deletes the session through icon buttons", async () => {
		const user = userEvent.setup();
		const { onEdit, onDelete } = renderList();

		await user.click(screen.getByRole("button", { name: "edit" }));
		await user.click(screen.getByRole("button", { name: "delete.button" }));

		expect(onEdit).toHaveBeenCalledWith(42);
		expect(onDelete).toHaveBeenCalledWith(42);
	});

	it("hides the actions for read-only users", () => {
		renderList({ canWrite: false });

		expect(screen.queryByRole("button", { name: "edit" })).not.toBeInTheDocument();
	});

	it("skips the comment and player list when there is nothing to show", () => {
		renderList({ sessions: [{ ...session, comment: "   ", playerSessions: [] } as Session] });

		expect(screen.queryByRole("list", { name: "players" })).not.toBeInTheDocument();
		expect(screen.getAllByRole("listitem")).toHaveLength(1);
	});

	it("shows the empty message without sessions", () => {
		renderList({ sessions: [] });

		expect(screen.getByText("no-data-yet")).toBeInTheDocument();
	});
});
