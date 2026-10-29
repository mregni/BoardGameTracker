import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Player, Session } from "@/models";
import { renderWithProviders, screen, within } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	captured: {} as { config: { component: FC } },
	sessions: undefined as unknown[] | undefined,
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.config = config;
		return { ...config, useParams: () => ({ playerId: 1 }) };
	},
	useNavigate: () => vi.fn(),
	Link: ({ children }: { children: React.ReactNode }) => <a href="/">{children}</a>,
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canWrite: false }),
}));

vi.mock("../-hooks/useBack", () => ({
	useBack: () => vi.fn(),
}));

const players = [
	{ id: 1, name: "Kathleen", image: null },
	{ id: 2, name: "Mikhaël", image: null },
] as unknown as Player[];

const sessions = [
	{
		id: 42,
		gameId: 1,
		start: new Date(2026, 9, 5, 20, 15),
		minutes: 70,
		comment: null,
		isCoop: true,
		teamWon: true,
		teamScore: 112,
		playerSessions: [
			{ playerId: 2, sessionId: 42, won: false, score: 61, firstPlay: false },
			{ playerId: 1, sessionId: 42, won: true, score: 78, firstPlay: false },
		],
	},
] as unknown as Session[];

vi.mock("./-hooks/usePlayerSessionData", () => ({
	usePlayerSessionData: () => ({
		settings: { dateFormat: "dd-MM-yyyy", timeFormat: "HH:mm", uiLanguage: "en-US" },
		player: { id: 1, name: "Kathleen", image: null },
		games: [{ id: 1, title: "Spirit Island", image: null }],
		sessions: mocks.sessions ?? sessions,
		players,
		deleteSession: vi.fn(),
		isLoading: false,
	}),
}));

await import("./$playerId_.sessions");

const renderPage = () => {
	const Component = mocks.captured.config.component;
	renderWithProviders(<Component />);
};

describe("Player sessions page", () => {
	beforeEach(() => {
		mocks.sessions = undefined;
	});

	it("shows the game of a session with a link and leaves a missing game empty", () => {
		mocks.sessions = [sessions[0], { ...sessions[0], id: 43, gameId: 99 }];
		renderPage();

		const rows = within(screen.getByRole("table")).getAllByRole("row");
		expect(within(rows[1]).getByText("Spirit Island")).toBeInTheDocument();
		expect(within(rows[1]).getByRole("button", { name: "Spirit Island" })).toBeInTheDocument();
		expect(rows[2].children[2]).toBeEmptyDOMElement();
	});

	it("titles the page after the player without a hyphen", () => {
		renderPage();

		expect(screen.getByText("sessions:title-of")).toBeInTheDocument();
		expect(screen.queryByText(/Kathleen - /)).not.toBeInTheDocument();
	});

	it("shows the co-op result and team score next to the players", () => {
		renderPage();

		const table = screen.getByRole("table");
		expect(within(table).getByText("coop.won")).toBeInTheDocument();
		expect(within(table).getByText("112")).toBeInTheDocument();
	});

	it("shows every player's name and score in the desktop table, winners first with a crown", () => {
		renderPage();

		const table = screen.getByRole("table");
		const pills = within(within(table).getByRole("list", { name: "players" })).getAllByRole("listitem");
		expect(pills).toHaveLength(2);
		expect(pills[0]).toHaveTextContent("Kathleen");
		expect(pills[0]).toHaveTextContent("78");
		expect(within(pills[0]).getByRole("img", { name: "won" })).toBeInTheDocument();
		expect(pills[1]).toHaveTextContent("Mikhaël");
		expect(pills[1]).toHaveTextContent("61");
		expect(within(pills[1]).queryByRole("img", { name: "won" })).not.toBeInTheDocument();
	});

	it("shows the duration in the short format", () => {
		renderPage();

		expect(within(screen.getByRole("table")).getByText("short-duration.hours-minutes")).toBeInTheDocument();
	});
});
