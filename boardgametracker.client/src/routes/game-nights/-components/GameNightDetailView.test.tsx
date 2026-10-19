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
			summary: {
				gamesPlayed: sessions.length,
				totalMinutes: sessions.length * 30,
				mostWins: 1,
				winnerIds: [2],
				runningGames: running ? 1 : 0,
				attendeeIds: [
					...new Set([1, ...sessions.flatMap((x) => x.playerSessions.map((p) => p.playerId)), ...(running ? [1] : [])]),
				],
				attendanceRecorded: false,
			},
		},
		sessions,
		runningGames: running
			? [{ id: 12, gameNightId: 4, gameId: 7, start: addMinutes(new Date(), -12), playerIds: [1] }]
			: [],
	}) as unknown as GameNightDetail;

const handlers = () => ({
	onEdit: vi.fn(),
	onDelete: vi.fn(),
	onEditAttendees: vi.fn(),
	onManageRsvps: vi.fn(),
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

	it("starts a game with the attendees who are free, logs one and plays a game of tonight again", async () => {
		const user = userEvent.setup();
		const props = renderView(buildDetail(tonight(), played()));

		await user.click(screen.getByRole("button", { name: "play.start.button" }));
		await user.click(screen.getByRole("button", { name: "play.log.button" }));
		await user.click(screen.getAllByRole("button", { name: "play.again-game" })[0]);

		expect(props.onStart).toHaveBeenNthCalledWith(1, null, [2]);
		expect(props.onLog).toHaveBeenCalled();
		expect(props.onStart).toHaveBeenNthCalledWith(2, 7, [2]);
	});

	it("starts with everyone who is coming when nothing runs yet", async () => {
		const user = userEvent.setup();
		const props = renderView(buildDetail(tonight(), [], false));

		await user.click(screen.getByRole("button", { name: "play.start.button" }));

		expect(props.onStart).toHaveBeenCalledWith(null, [1, 2]);
	});

	it("puts whoever played at the table, whatever their answer", () => {
		const declinedButPlayed = [
			session(3, 7, addMinutes(new Date(), -40), [
				[3, 60, true],
				[1, 40, false],
			]),
		];
		renderView(buildDetail(tonight(), declinedButPlayed, false));

		const table = screen.getByRole("region", { name: "table.title" });
		expect(within(table).getByText("Reggi")).toBeInTheDocument();
		expect(within(table).queryByText("Kathleen")).not.toBeInTheDocument();
	});

	it("shows the players who said yes as expected before any game", () => {
		renderView(buildDetail(tonight(), [], false));

		expect(screen.getByText("table.expected")).toBeInTheDocument();
		expect(screen.getByText("Kathleen")).toBeInTheDocument();
		expect(screen.queryByText("Reggi")).not.toBeInTheDocument();
	});

	it("says it is playing now in the header of a night in progress", () => {
		renderView(buildDetail(tonight(), [], false));

		expect(screen.getByText("card.live")).toBeInTheDocument();
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

	it("shows the night winner and only keeps logging once the night is over by time", () => {
		renderView(buildDetail(addHours(new Date(), -30), played(), false));

		expect(screen.getByText("summary.night-winner")).toBeInTheDocument();
		expect(screen.getByText("played.after")).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "play.again-game" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "play.start.button" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "end.button" })).not.toBeInTheDocument();
		expect(screen.getByRole("button", { name: "play.log.button" })).toBeInTheDocument();
	});

	it("explains how to add games afterwards when nothing was logged", () => {
		renderView(buildDetail(addHours(new Date(), -30), [], false));

		expect(screen.getByText("played.empty-after")).toBeInTheDocument();
		expect(screen.queryByText("played.empty")).not.toBeInTheDocument();
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
		renderView(buildDetail(tonight(), [], false));

		await user.click(screen.getByText("Kathleen"));
		await user.click(screen.getByText("Age of Inventors"));

		expect(navigate).toHaveBeenCalledWith({ to: "/players/2" });
		expect(navigate).toHaveBeenCalledWith({ to: "/games/9" });
	});

	it("only lets the host end the night once no game is running", () => {
		renderView(buildDetail(tonight(), played()));

		const endButton = screen.getByRole("button", { name: "end.button" });
		expect(endButton).toBeDisabled();
		expect(endButton).toHaveAccessibleDescription("end.running-hint");
		expect(endButton.parentElement).toHaveAttribute("title", "end.running-hint");
	});

	it("ends the night when no game is running", async () => {
		const user = userEvent.setup();
		const props = renderView(buildDetail(tonight(), played(), false));

		expect(screen.queryByText("end.running-hint")).not.toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: "end.button" }));

		expect(props.onEnd).toHaveBeenCalled();
	});

	it("does not offer to reopen a night once its play window has passed", () => {
		renderView(buildDetail(addHours(new Date(), -30), played(), false, addHours(new Date(), -26)));

		expect(screen.queryByRole("button", { name: "end.reopen" })).not.toBeInTheDocument();
		expect(screen.queryByTestId("night-actions")).not.toBeInTheDocument();
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

	it("renders one set of night actions, with Start pinned first on phones", () => {
		renderView(buildDetail(tonight(), played(), false));

		const actions = screen.getByTestId("night-actions");
		const buttons = within(actions).getAllByRole("button");
		expect(buttons.map((x) => x.textContent)).toEqual(["end.button", "play.log.button", "play.start.button"]);
		expect(buttons[2]).toHaveClass("order-first", "md:order-none");
		expect(buttons[1].parentElement).toHaveClass("fixed", "md:static");
	});

	it("only offers logging in the header when the night is over by time", () => {
		renderView(buildDetail(addHours(new Date(), -30), [], false));

		const buttons = within(screen.getByTestId("night-actions")).getAllByRole("button");
		expect(buttons.map((x) => x.textContent)).toEqual(["play.log.button"]);
		expect(buttons[0].parentElement).toHaveClass("grid-cols-1");
	});

	it("shows no night actions to readers", () => {
		renderView(buildDetail(tonight(), [], false), false);

		expect(screen.queryByTestId("night-actions")).not.toBeInTheDocument();
	});

	it("shows the plan instead of the live night for a night a week away", async () => {
		const user = userEvent.setup();
		const props = renderView(buildDetail(addHours(new Date(), 24 * 7), [], false));

		expect(screen.getByTestId("night-plan")).toBeInTheDocument();
		expect(screen.getByText("plan.starts")).toBeInTheDocument();
		expect(screen.getByTestId("rsvp-summary")).toHaveTextContent("rsvp.summary.coming · rsvp.summary.declined");
		expect(screen.getByText("Carcassonne")).toBeInTheDocument();
		expect(screen.queryByTestId("night-actions")).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "end.button" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "play.start.button" })).not.toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "play.log.button" })).not.toBeInTheDocument();
		expect(screen.queryByText("played.tonight")).not.toBeInTheDocument();
		expect(screen.queryByText("table.title")).not.toBeInTheDocument();

		await user.click(screen.getByRole("button", { name: "card.manage-rsvps" }));
		expect(props.onManageRsvps).toHaveBeenCalledTimes(1);
		expect(props.onEditAttendees).not.toHaveBeenCalled();
	});

	it("says when no games are planned and hides managing RSVPs from readers", () => {
		const detail = buildDetail(addHours(new Date(), 13), [], false);
		detail.gameNight.suggestedGames = [];
		renderView(detail, false);

		expect(screen.getByText("plan.no-games")).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "card.manage-rsvps" })).not.toBeInTheDocument();
	});

	it("opens the live controls from twelve hours before the start", () => {
		renderView(buildDetail(addHours(new Date(), 11), [], false));

		expect(screen.queryByTestId("night-plan")).not.toBeInTheDocument();
		expect(screen.getByRole("button", { name: "play.start.button" })).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "end.button" })).toBeInTheDocument();
		expect(screen.getByText("card.when")).toBeInTheDocument();
	});
});
