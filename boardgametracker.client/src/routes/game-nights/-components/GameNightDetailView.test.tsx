import { addHours, addMinutes } from "date-fns";
import { describe, expect, it, vi } from "vitest";
import {
	type Game,
	type GameNightDetail,
	GameNightRsvpState,
	type Player,
	type Session,
	type Settings,
} from "@/models";
import { act, renderWithProviders, screen, userEvent, within } from "@/test/test-utils";

const navigate = vi.hoisted(() => vi.fn());

vi.mock("@tanstack/react-router", () => ({
	useNavigate: () => navigate,
	Link: ({ children, to, params }: { children: React.ReactNode; to: string; params: { sessionId: number } }) => (
		<a href={to.replace("$sessionId", String(params.sessionId))}>{children}</a>
	),
}));

import { GameNightDetailView } from "./GameNightDetailView";

const settings = { dateFormat: "dd-MM-yyyy", timeFormat: "HH:mm", uiLanguage: "en-us" } as Settings;
const players = [
	{ id: 1, name: "Mikhael", image: null },
	{ id: 2, name: "Kathleen", image: null },
	{ id: 3, name: "Reggi", image: null },
] as unknown as Player[];
const games = [
	{ id: 7, title: "Azul", image: null },
	{ id: 8, title: "Carcassonne", image: null },
	{ id: 9, title: "Age of Inventors", image: null },
] as unknown as Game[];

const session = (id: number, gameId: number, start: Date, scores: [number, number | null, boolean][]) =>
	({
		id,
		gameId,
		start,
		end: addMinutes(start, 30),
		minutes: 30,
		playerSessions: scores.map(([playerId, score, won]) => ({ sessionId: id, playerId, score, won, firstPlay: false })),
	}) as unknown as Session;

const buildDetail = (startDate: Date, sessions: Session[], running = true, endedAt: Date | null = null) =>
	({
		gameNight: {
			id: 4,
			title: "Strategy night",
			notes: "Bring snacks",
			startDate,
			endedAt,
			hostId: 1,
			host: players[0],
			locationId: 5,
			location: { id: 5, name: "Thuis" },
			linkId: "abc",
			suggestedGames: [games[1], games[2]],
			invitedPlayers: [
				{ id: 11, playerId: 1, gameNightId: 4, state: GameNightRsvpState.Accepted, player: players[0] },
				{ id: 12, playerId: 2, gameNightId: 4, state: GameNightRsvpState.Accepted, player: players[1] },
				{ id: 13, playerId: 3, gameNightId: 4, state: GameNightRsvpState.Declined, player: players[2] },
			],
			summary: { gamesPlayed: sessions.length, totalMinutes: sessions.length * 30, mostWins: 1, winnerIds: [2] },
		},
		sessions,
		runningGames: running
			? [{ id: 12, gameNightId: 4, gameId: 7, start: addMinutes(new Date(), -12), playerIds: [1, 2] }]
			: [],
	}) as unknown as GameNightDetail;

const handlers = () => ({
	onEdit: vi.fn(),
	onDelete: vi.fn(),
	onEditAttendees: vi.fn(),
	onStart: vi.fn(),
	onLog: vi.fn(),
	onFinish: vi.fn(),
	onDiscard: vi.fn(),
	onEnd: vi.fn(),
	onReopen: vi.fn(),
});

const renderView = (detail: GameNightDetail, canWrite = true) => {
	const props = handlers();
	renderWithProviders(
		<GameNightDetailView
			detail={detail}
			settings={settings}
			games={games}
			players={players}
			canWrite={canWrite}
			{...props}
		/>,
	);
	return props;
};

const tonight = () => addHours(new Date(), -2);
const played = () => [
	session(2, 7, addMinutes(new Date(), -40), [
		[2, 82, true],
		[1, 71, false],
	]),
	session(1, 8, addMinutes(new Date(), -100), [
		[1, null, false],
		[2, 90, true],
	]),
];

describe("GameNightDetailView", () => {
	it("shows the running game with its players and lets the host finish or discard it", async () => {
		const user = userEvent.setup();
		const props = renderView(buildDetail(tonight(), played()));

		const card = screen.getByTestId("running-game");
		expect(within(card).getByText("play.now-playing")).toBeInTheDocument();
		await user.click(within(card).getByRole("button", { name: "play.finish.button" }));
		await user.click(within(card).getByRole("button", { name: "play.discard.button" }));

		expect(props.onFinish).toHaveBeenCalledWith(expect.objectContaining({ id: 12 }));
		expect(props.onDiscard).toHaveBeenCalledWith(expect.objectContaining({ id: 12 }));
	});

	it("starts a game with the attendees, logs one and plays a game of tonight again", async () => {
		const user = userEvent.setup();
		const props = renderView(buildDetail(tonight(), played()));

		await user.click(screen.getByRole("button", { name: "play.start.button" }));
		await user.click(screen.getByRole("button", { name: "play.log.button" }));
		await user.click(screen.getAllByRole("button", { name: "play.again-game" })[0]);

		expect(props.onStart).toHaveBeenNthCalledWith(1, null, [1, 2]);
		expect(props.onLog).toHaveBeenCalled();
		expect(props.onStart).toHaveBeenNthCalledWith(2, 7, [2, 1]);
	});

	it("lists what was played newest first with winner, scores and a link to the session", () => {
		renderView(buildDetail(tonight(), played()));

		const rows = screen.getAllByTestId("played-session");
		expect(rows).toHaveLength(2);
		expect(rows[0]).toHaveTextContent("#2");
		expect(rows[0]).toHaveTextContent("Kathleen 82 · Mikhael 71");
		expect(rows[1]).toHaveTextContent("Kathleen 90 · Mikhael");
		expect(within(rows[0]).getByRole("link")).toHaveAttribute("href", "/sessions/update/2");
		expect(screen.getByText("played.tonight")).toBeInTheDocument();
		expect(screen.getByText("summary.leading")).toBeInTheDocument();
	});

	it("shows who is at the table, opens the attendee editor and ticks the planned games that were played", async () => {
		const user = userEvent.setup();
		const props = renderView(buildDetail(tonight(), played()));

		expect(screen.getByText("table.host")).toBeInTheDocument();
		expect(screen.queryByText("Reggi")).not.toBeInTheDocument();
		expect(screen.getAllByLabelText("planned.played")).toHaveLength(1);
		await user.click(screen.getByRole("button", { name: "table.edit" }));

		expect(props.onEditAttendees).toHaveBeenCalled();
	});

	it("shows the night winner and hides the buttons once the night is over", () => {
		renderView(buildDetail(addHours(new Date(), -30), played(), false));

		expect(screen.getByText("summary.night-winner")).toBeInTheDocument();
		expect(screen.getByText("played.after")).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "play.again-game" })).not.toBeInTheDocument();
	});

	it("explains how to start when nothing was played yet", () => {
		renderView(buildDetail(tonight(), [], false));

		expect(screen.getByText("played.empty")).toBeInTheDocument();
		expect(screen.queryByText("summary.games-played")).not.toBeInTheDocument();
	});

	it("is read-only for readers", () => {
		renderView(buildDetail(tonight(), played()), false);

		expect(screen.queryByRole("button", { name: "play.start.button" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "play.finish.button" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "table.edit" })).not.toBeInTheDocument();
		expect(screen.queryByRole("link")).not.toBeInTheDocument();
	});

	it("keeps the running time up to date", () => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		try {
			renderView(buildDetail(tonight(), []));
			act(() => {
				vi.advanceTimersByTime(30_000);
			});
			expect(screen.getByText("play.running")).toBeInTheDocument();
		} finally {
			vi.useRealTimers();
		}
	});

	it("opens the players and planned games", async () => {
		const user = userEvent.setup();
		renderView(buildDetail(tonight(), []));

		await user.click(screen.getByText("Kathleen"));
		await user.click(screen.getByText("Age of Inventors"));

		expect(navigate).toHaveBeenCalledWith({ to: "/players/2" });
		expect(navigate).toHaveBeenCalledWith({ to: "/games/9" });
	});

	it("only lets the host end the night once no game is running", () => {
		renderView(buildDetail(tonight(), played()));

		expect(screen.getByRole("button", { name: "end.button" })).toBeDisabled();
		expect(screen.getByText("end.running-hint")).toBeInTheDocument();
	});

	it("ends the night when no game is running", async () => {
		const user = userEvent.setup();
		const props = renderView(buildDetail(tonight(), played(), false));

		expect(screen.queryByText("end.running-hint")).not.toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: "end.button" }));

		expect(props.onEnd).toHaveBeenCalled();
	});

	it("shows an ended night with its winner and offers to reopen it", async () => {
		const user = userEvent.setup();
		const props = renderView(buildDetail(tonight(), played(), false, new Date()));

		expect(screen.getByText("summary.night-winner")).toBeInTheDocument();
		expect(screen.getByText("played.after")).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "play.start.button" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "play.log.button" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "play.again-game" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "end.button" })).not.toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: "end.reopen" }));

		expect(props.onReopen).toHaveBeenCalled();
	});

	it("keeps logging possible when the night is only over because it is late", () => {
		renderView(buildDetail(addHours(new Date(), -30), played(), false));

		expect(screen.getByRole("button", { name: "play.log.button" })).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "end.button" })).toBeEnabled();
	});
});
